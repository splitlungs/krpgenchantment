using System.Collections.Generic;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace KRPGLib.Enchantment
{
    public class EnchantingBE : BlockEntityOpenableContainer
    {
        ICoreServerAPI sApi;
        GuiDialogEnchantingBE clientDialog;
        EnchantingInventory inventory;
        public int msEnchantTick = 3000;
        public double inputEnchantTime;
        public double prevInputEnchantTime;
        private double latentResetTime;
        // Shortened for debugging
        // public double maxEnchantTime = 0.1;
        public bool nowEnchanting = false;
        public List<EnchantingRecipe> ValidRecipes = new List<EnchantingRecipe>();
        public EnchantingRecipe CurrentRecipe;
        int nowOutputFace;
        #region Candles
        int QuantityCandles = 5;
        internal Vec3f[] candleWickPositions = new Vec3f[5]
        {
            new Vec3f(7.8f, 2f, 8.8f),
            new Vec3f(12.8f, 6f, 12.8f),
            new Vec3f(11.8f, 4f, 6.8f),
            new Vec3f(1.8f, 1f, 12.8f),
            new Vec3f(6.8f, 4f, 13.8f)
        };
        private Vec3f[][] candleWickPositionsByRot = new Vec3f[4][];
        internal void initRotations()
        {
            for (int i = 0; i < 4; i++)
            {
                Matrixf matrixf = new Matrixf();
                matrixf.Translate(0.5f, 0.5f, 0.5f);
                matrixf.RotateYDeg(i * 90);
                matrixf.Translate(-0.5f, -0.5f, -0.5f);
                Vec3f[] array = (candleWickPositionsByRot[i] = new Vec3f[candleWickPositions.Length]);
                for (int j = 0; j < array.Length; j++)
                {
                    Vec4f vec4f = matrixf.TransformVector(new Vec4f(candleWickPositions[j].X / 16f, candleWickPositions[j].Y / 16f, candleWickPositions[j].Z / 16f, 1f));
                    array[j] = new Vec3f(vec4f.X, vec4f.Y, vec4f.Z);
                }
            }
        }
        public static AdvancedParticleProperties[] ParticleProperties = new AdvancedParticleProperties[0];
        
        public static SimpleParticleProperties particle = new SimpleParticleProperties(
                    1, 1,
                    ColorUtil.ColorFromRgba(220, 220, 220, 50),
                    new Vec3d(),
                    new Vec3d(),
                    new Vec3f(-0.25f, 0.1f, -0.25f),
                    new Vec3f(0.25f, 0.1f, 0.25f),
                    1.5f,
                    -0.075f,
                    0.25f,
                    0.25f,
                    EnumParticleModel.Quad
                );
        public void OnAsyncClientParticleTick(IAsyncParticleManager manager, BlockPos pos, float windAffectednessAtPos, float secondsTicking)
        {
            if (ParticleProperties == null || ParticleProperties.Length == 0)
            {
                return;
            }

            int num = GameMath.MurmurHash3Mod(pos.X, pos.Y, pos.Z, 4);
            Vec3f[] array = candleWickPositionsByRot[num];
            for (int i = 0; i < ParticleProperties.Length; i++)
            {
                AdvancedParticleProperties advancedParticleProperties = ParticleProperties[i];
                advancedParticleProperties.WindAffectednesAtPos = windAffectednessAtPos;
                for (int j = 0; j < QuantityCandles; j++)
                {
                    Vec3f vec3f = array[j];
                    advancedParticleProperties.basePos.X = (float)pos.X + vec3f.X;
                    advancedParticleProperties.basePos.Y = (float)pos.Y + vec3f.Y;
                    advancedParticleProperties.basePos.Z = (float)pos.Z + vec3f.Z;
                    manager.Spawn(advancedParticleProperties);
                }
            }
        }
        #endregion
        #region Getters
        public string Material
        {
            get { return Block.LastCodePart(); }
        }
        // TODO: Conform to KRPGClasses
        /*
        public float EnchantSpeed
        {
            get
            {
                if (quantityPlayersEnchanting > 0) return 1f;

                return 0;
            }
        }*/
        public virtual string DialogTitle
        {
            get { return Lang.Get("krpgenchantment:block-enchanting-table"); }
        }
        public override string InventoryClassName
        {
            get { return Block.FirstCodePart(); }
        }
        public override InventoryBase Inventory
        {
            get { return inventory; }
        }
        public ItemSlot InputSlot
        {
            get { return inventory[0]; }
        }
        public ItemSlot OutputSlot
        {
            get { return inventory[1]; }
        }
        public ItemStack OutputStack
        {
            get;
            private set;
        }
        public ItemSlot ReagentSlot
        {
            get { return inventory[2]; }
        }
        public EnchantingBE()
        {
            inventory = new EnchantingInventory(null, null, this);
            inventory.SlotModified += OnSlotModified;
        }
        /// <summary>
        /// Returns True if an ItemStack in Slot has any Enchantments
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        public bool isEnchanted(ItemSlot slot)
        {
            if (slot.Itemstack == null) return false;

            Dictionary<string, int> enchants = Api.GetEnchantments(slot);

            if (enchants.Count > 0) return true;

            return false;
        }
        private bool CanEnchantByType
        {
            get
            {
                if (CurrentRecipe == null) return false;
                if (!CurrentRecipe.Matches(InputSlot, ReagentSlot)) return false;

                return true;
            }
        }
        private bool CanEnchant
        {
            get
            {
                if (CurrentRecipe == null) return false;
                if (!CurrentRecipe.Matches(InputSlot, ReagentSlot)) return false;

                return true;
            }
        }
        /// <summary>
        /// Returns Matching EnchantingRecipe or null if not found.
        /// </summary>
        /// <returns></returns>
        public EnchantingRecipe GetMatchingEnchantingRecipe()
        {
            var enchantingRecipes = Api.GetEnchantingRecipes();
            if (enchantingRecipes != null)
            {
                for (int i = 0; i < enchantingRecipes.Count; i++)
                {
                    if (enchantingRecipes[i].Matches(InputSlot, ReagentSlot))
                    {
                        return enchantingRecipes[i].Clone();
                    }
                }
            }
            else
                Api.Logger.Event("No Matching Enchanting Recipe Registry found!");

            return null;
        }
        /// <summary>
        /// Returns Matching EnchantingRecipe or null if not found.
        /// </summary>
        /// <returns></returns>
        public void GetMatchingEnchantingRecipes()
        {
            var enchantingRecipes = Api.GetEnchantingRecipes();
            if (enchantingRecipes != null)
            {
                ValidRecipes.Clear();
                for (int i = 0; i < enchantingRecipes.Count; i++)
                {
                    if (enchantingRecipes[i].Matches(InputSlot, ReagentSlot))
                         ValidRecipes.Add(enchantingRecipes[i].Clone());
                }
            }
            else
                Api.Logger.Event("No Matching Enchanting Recipe Registry found!");
        }

        public double LatentResetTime() 
        {
            double ero = -0.1d;
            // Return override first
            if (EnchantingRecipeLoader.Config?.EnchantResetOverride != null)
                    ero = EnchantingRecipeLoader.Config.EnchantResetOverride;
                if (ero >= 0d)
                    return ero;
                // Then current recipe
                if (CurrentRecipe != null)
                    ero = CurrentRecipe.processingHours;
                if (ero >= 0d)
                    return ero;
                // Fall back to 7 days
                return 7d;
        }
        /// <summary>
        /// Find a valid Enchantment for the item and write a Latent Enchantment to Attributes
        /// </summary>
        public void AssessItem()
        {
            if (ValidRecipes.Count < 1 || InputSlot.Empty) return;

            double timeStamp = Api.World.Calendar.ElapsedDays;
            double latentStamp = InputSlot.Itemstack.Attributes.GetDouble("latentEnchantTime", 0);
            if (Api.World.Calendar.ElapsedDays < latentStamp + 1)
                return;

            foreach (var rec in ValidRecipes)
            {
                double rNum2 = Api.World.Rand.NextDouble();
                InputSlot.Itemstack.Attributes.SetString("latentEnchant", rec.Name.ToShortString());
                InputSlot.Itemstack.Attributes.SetDouble("latentEnchantTime", timeStamp);
            }
        }
        /// <summary>
        /// Gets the current recipe's processing time in in-game hours.
        /// </summary>
        /// <returns></returns>
        public double MaxEnchantTime
        {
            get { return GetMaxEnchantTime(); }
        }
        private double GetMaxEnchantTime()
        {
            double eto = -0.1d;
            // Return override first
            if (EnchantingRecipeLoader.Config?.EnchantTimeOverride != null)
                eto = EnchantingRecipeLoader.Config.EnchantTimeOverride;
            if (eto >= 0d)
                return eto;
            // Then current recipe
            if (CurrentRecipe != null)
                eto = CurrentRecipe.processingHours;
            if (eto >= 0d)
                return eto;
            // Fall back to 1 hour
            return 1d;
        }
        /// <summary>
        /// Gets Name of Matching Enchanting Recipe with Enchanter Prefix
        /// </summary>
        /// <returns></returns>
        public string OutputText
        {
            get { return GetOutputText(); }
        }
        private string GetOutputText()
        {
            string outText = Lang.Get("krpgenchantment:krpg-enchanter-enchant-prefix");

            if (CurrentRecipe != null)
            {
                string eName = CurrentRecipe.Name.ToShortString();
                eName = eName.Replace(CurrentRecipe.Name.Domain, "krpgenchantment");
                return outText + Lang.Get(eName);
            }
            else
                return outText;
        }
        #endregion
        #region Main
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            Api = api;
            sApi = api as ICoreServerAPI;

            inventory.LateInitialize(Block.FirstCodePart() + "-" + Pos.X + "/" + Pos.Y + "/" + Pos.Z, api);
            
            if (api.Side == EnumAppSide.Server)
                RegisterGameTickListener(TickEnchanting, 1000);
        }
        public override void CreateBehaviors(Block block, IWorldAccessor worldForResolve)
        {
            base.CreateBehaviors(block, worldForResolve);
        }
        private void enchantInput()
        {
            ItemStack outStack = CurrentRecipe.OutStack(InputSlot.Itemstack).Clone();
            if (OutputSlot.Itemstack == null)
            {
                OutputSlot.Itemstack = outStack;
            }
            else
            {
                int mergableQuantity = OutputSlot.Itemstack.Collectible.GetMergableQuantity(OutputSlot.Itemstack, outStack, EnumMergePriority.AutoMerge);

                if (mergableQuantity > 0)
                {

                    OutputSlot.Itemstack.StackSize += outStack.StackSize;
                }
                else
                {
                    BlockFacing face = BlockFacing.HORIZONTALS[nowOutputFace];
                    nowOutputFace = (nowOutputFace + 1) % 4;

                    Block block = Api.World.BlockAccessor.GetBlock(this.Pos.AddCopy(face));
                    if (block.Replaceable < 6000) return;
                    Api.World.SpawnItemEntity(outStack, this.Pos.ToVec3d().Add(0.5 + face.Normalf.X * 0.7, 0.75, 0.5 + face.Normalf.Z * 0.7), new Vec3d(face.Normalf.X * 0.02f, 0, face.Normalf.Z * 0.02f));
                }
            }

            int rQty = CurrentRecipe.resolvedIngredients[0].Quantity;
            int iQty = CurrentRecipe.resolvedIngredients[1].Quantity;
            ReagentSlot.TakeOut(rQty);
            InputSlot.TakeOut(iQty);
            ReagentSlot.MarkDirty();
            InputSlot.MarkDirty();
            OutputSlot.MarkDirty();

            inputEnchantTime = 0;
            CurrentRecipe = null;
        }
        /// <summary>
        /// Toggles if TickEnchanting() can attempt EnchantInput()
        /// </summary>
        public void UpdateEnchantingState()
        {
            if (Api.World == null) return;

            if (Api.Side == EnumAppSide.Server)
            {
                if (!nowEnchanting)
                    nowEnchanting = true;
                else
                    nowEnchanting = false;
            }

            if (!nowEnchanting) return;

            inputEnchantTime = Api.World.Calendar.TotalHours;
            MarkDirty(true);
        }
        private void TickEnchanting(float dt)
        {
            if (clientDialog != null)
                CurrentRecipe = GetMatchingEnchantingRecipe();

            if (CanEnchant && nowEnchanting)
            {
                double hours = Api.World.Calendar.TotalHours - inputEnchantTime;

                if (hours >= MaxEnchantTime)
                {
                    enchantInput();

                    nowEnchanting = false;
                }

                if (clientDialog != null && CurrentRecipe != null)
                    clientDialog.Update(hours, MaxEnchantTime, nowEnchanting, OutputText, Api);
            }

            MarkDirty();
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);

            Inventory.FromTreeAttributes(tree.GetTreeAttribute("inventory"));
            if (Api != null)
            {
                Inventory.AfterBlocksLoaded(Api.World);
            }

            nowEnchanting = tree.GetBool("nowEnchanting");
            inputEnchantTime = tree.GetDouble("inputEnchantTime");
            prevInputEnchantTime = tree.GetDouble("prevInputEnchantTime");

            if (Api != null)
            {
                CurrentRecipe = GetMatchingEnchantingRecipe();
            }
            if (Api?.Side == EnumAppSide.Client)
            {
                // currentMesh = GenMesh();
                
                // invDialog?.UpdateContents();
                double hours = Api.World.Calendar.TotalHours - inputEnchantTime;
                if (clientDialog != null)
                    clientDialog.Update(hours, MaxEnchantTime, nowEnchanting, OutputText, Api);

                MarkDirty(true);
            }
        }
        void SetDialogValues(ITreeAttribute dialogTree)
        {

            dialogTree.SetDouble("inputEnchantTime", inputEnchantTime);
            dialogTree.SetDouble("maxEnchantTime", MaxEnchantTime);
            dialogTree.SetBool("nowEnchanting", nowEnchanting);
            dialogTree.SetString("outputText", OutputText);
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            ITreeAttribute invtree = new TreeAttribute();
            Inventory.ToTreeAttributes(invtree);
            tree["inventory"] = invtree;


            // if (Block != null) tree.SetString("forBlockCode", Block.Code.ToShortString());
            
            tree.SetBool("nowEnchanting", nowEnchanting);
            tree.SetDouble("inputEnchantTime", inputEnchantTime);
            tree.SetDouble("prevInputEnchantTime", prevInputEnchantTime);

        }
        protected void toggleInventoryDialogClient(IPlayer byPlayer)
        {
            if (clientDialog == null)
            {
                double hours = 0d;
                if (CurrentRecipe != null && nowEnchanting)
                    hours = Api.World.Calendar.TotalHours - inputEnchantTime;
                string outText = OutputText;

                ICoreClientAPI capi = Api as ICoreClientAPI;
                clientDialog = new GuiDialogEnchantingBE(DialogTitle, hours, MaxEnchantTime, nowEnchanting, outText, Inventory, Pos, capi);
                clientDialog.OnClosed += () =>
                {
                    clientDialog = null;
                    capi.Network.SendBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, (int)EnumBlockEntityPacketId.Close, null);
                    capi.Network.SendPacketClient(Inventory.Close(byPlayer));
                };
                clientDialog.OpenSound = AssetLocation.Create("sounds/block/barrelopen");
                clientDialog.CloseSound = AssetLocation.Create("sounds/block/barrelclose");

                clientDialog.TryOpen();
                capi.Network.SendPacketClient(Inventory.Open(byPlayer));
                capi.Network.SendBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, (int)EnumBlockEntityPacketId.Open, null);
                MarkDirty();
            }
            else
            {
                clientDialog.TryClose();
            }
        }
        #endregion
        #region Events
        private void OnSlotModified(int slotid)
        {
            // Stop Enchanting
            nowEnchanting = false;

            CurrentRecipe = GetMatchingEnchantingRecipe();

            double hours = Api.World.Calendar.TotalHours - inputEnchantTime;

            if (slotid == 0 || slotid > 1)
            {
                inputEnchantTime = 0.0d; //reset the progress to 0 if any of the input is changed.
                hours = 0d;
            }

            if (slotid == 1)
            {
                hours = 0d;
                // clientDialog.SingleComposer.ReCompose();
            }
            
            clientDialog?.Update(hours, MaxEnchantTime, nowEnchanting, OutputText, Api);
            MarkDirty();

            if (clientDialog != null && clientDialog.IsOpened())
            {
                clientDialog.SingleComposer.ReCompose();
            }
        }
        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            if (blockSel.SelectionBoxIndex == 1) return false;
            
            CurrentRecipe = GetMatchingEnchantingRecipe();

            if (Api.Side == EnumAppSide.Client)
            {
                toggleInventoryDialogClient(byPlayer);
            }

            return true;
        }

        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            base.OnReceivedClientPacket(player, packetid, data);

            if (packetid == (int)EnumBlockEntityPacketId.Open)
            {
                double hours = Api.World.Calendar.TotalHours - inputEnchantTime;
                if (clientDialog != null) { clientDialog.Update(hours, MaxEnchantTime, nowEnchanting, OutputText, Api); }
                
                MarkDirty();
                
                player.InventoryManager?.OpenInventory(Inventory);
                // toggleInventoryDialogClient(player);
            }

            if (packetid == 1337)
            {
                if (CanEnchant)
                    UpdateEnchantingState();
            }
        }
        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            base.OnReceivedServerPacket(packetid, data);

            if (packetid == (int)EnumBlockEntityPacketId.Close)
            {
                (Api.World as IClientWorldAccessor).Player.InventoryManager.CloseInventory(Inventory);
                clientDialog?.TryClose();
                clientDialog?.Dispose();
                clientDialog = null;
            }
        }
        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            base.OnBlockBroken(byPlayer);

            clientDialog?.TryClose();
            clientDialog?.Dispose();
            clientDialog = null;
        }
        #endregion
    }
}