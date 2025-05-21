using System.Collections.Generic;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using System.Linq;
using System.Text;
using ProtoBuf;
using Vintagestory.API.Util;
using System.Data;
using Vintagestory.API.Common.Entities;
using System.Globalization;

namespace KRPGLib.Enchantment
{
    public class AssessmentBE : BlockEntityOpenableContainer
    {
        // Config
        public int MsAssessTick = 1000;
        public int MsSoundTick = 4000;
        public AssetLocation EnchantSound;
        private long SoundTickListener;
        // Sync'd with client
        AssessmentInventory inventory;
        public bool IsAssessing = false;
        public double InputTime = 0;
        public double Delay = 3000;
        // public int SelectedEnchant = -1;
        // public Dictionary<string, bool> Readers = new Dictionary<string, bool>();
        // Internal use
        public AssessmentTableGui clientDialog;
        // public EnchantingRecipe CurrentRecipe;
        public string CurrentEnchantment;
        int nowOutputFace;
        #region Getters
        public string Material
        {
            get { return Block.LastCodePart(); }
        }
        public virtual string DialogTitle
        {
            get { return Lang.Get("krpgenchantment:block-assessment-table"); }
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
        public ItemSlot GetReagentSlot(int slot)
        {
            return inventory[slot];
        }
        public AssessmentBE()
        {
            inventory = new AssessmentInventory(null, null, this);
            inventory.SlotModified += OnSlotModified;
        }
        /// <summary>
        /// Returns True if an ItemStack in Slot has any Enchantments. Always returns false for Client.
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        public bool isEnchanted(ItemSlot slot)
        {
            if (Api.Side == EnumAppSide.Client || slot.Itemstack == null) return false;
            
            Dictionary<string, int> enchants = Api.EnchantAccessor().GetEnchantments(slot.Itemstack);

            if (enchants != null) return true;

            return false;
        }
        private bool CanEnchant
        {
            get
            {
                ICoreServerAPI sApi = Api as ICoreServerAPI;
                if (!sApi.EnchantAccessor().CanEnchant(InputSlot.Itemstack, ReagentSlot.Itemstack, CurrentEnchantment)) return false;
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] Current Enchantment {0} Matches ingredients.", CurrentEnchantment);
                return true;
            }
        }
        /// <summary>
        /// Can be overriden from EnchantingRecipeConfig. Default 7 days.
        /// </summary>
        /// <returns></returns>
        public double LatentEnchantResetTime
        {
            get { return GetLatentEnchantResetTime(); }
        }
        private double GetLatentEnchantResetTime()
        {
            // Return override first
            if (EnchantingConfigLoader.Config?.LatentEnchantResetDays != null)
                return EnchantingConfigLoader.Config.LatentEnchantResetDays;
            // Fall back to 7 days
            return 7.0d;
        }
        public bool ReagentSlotsEmpty
        {
            get { return GetReagentSlotsEmpty(); }
        }
        private bool GetReagentSlotsEmpty()
        {
            bool foundItems = true;
            foreach (ItemSlot slot in inventory.Slots)
            {
                if (!slot.Empty) return false;
            }
            return foundItems;
        }
        /// <summary>
        /// Returns a list of LatentEnchants, unencrypted, with domain
        /// </summary>
        public List<string> LatentEnchants
        {
            get { return Api.EnchantAccessor().GetLatentEnchants(InputSlot, false); }
        }
        /// <summary>
        /// Returns a list of LatentEnchants, encrypted, 8 characters
        /// </summary>
        public List<string> LatentEnchantsEncrypted
        {
            get { return Api.EnchantAccessor().GetLatentEnchants(InputSlot, true); }
        }
        /// <summary>
        /// Returns default 3 if not set in the config file
        /// </summary>
        public int EnchantRowCount
        {
            get
            {
                if (EnchantingConfigLoader.Config?.MaxLatentEnchants != null) return EnchantingConfigLoader.Config.MaxLatentEnchants;
                else return 3;
            }
        }
        /// <summary>
        /// Gets the current recipe's processing time in in-game hours.
        /// </summary>
        /// <returns></returns>
        public double MaxAssessmentTime
        {
            get { return GetMaxAssessmentTime(); }
        }
        private double GetMaxAssessmentTime()
        {
            double eto = -0.1d;
            // Return override first
            if (EnchantingConfigLoader.Config?.AssessReagentHours != null)
                eto = EnchantingConfigLoader.Config.AssessReagentHours;
            if (eto >= 0d)
                return eto;
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
            return Lang.Get("krpgenchantment:krpg-enchanter-enchant-prefix");
        }
        #endregion
        #region Main
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            Api = api;
            EnchantSound = new AssetLocation("game:sounds/effect/translocate-active");
            inventory.LateInitialize(Block.FirstCodePart() + "-" + Pos.X + "/" + Pos.Y + "/" + Pos.Z, api);

            if (api.Side == EnumAppSide.Server)
            {
                RegisterGameTickListener(TickAssessment, MsAssessTick);
            }
            if (api.Side == EnumAppSide.Client)
            {
                SoundTickListener = RegisterGameTickListener(TickSounds, MsSoundTick);
            }
        }
        /// <summary>
        /// Called by the EnchantingBE to write the CurrentRecipe to the InputSlot's Itemstack
        /// </summary>
        private void enchantInput()
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.World.Logger.Event("[KRPGEnchantment] Attempting to enchant an item.");
            ICoreServerAPI sApi = Api as ICoreServerAPI;
            ItemStack outStack = sApi.EnchantAccessor().EnchantItem(InputSlot, ReagentSlot, new Dictionary<string, int>() { { CurrentEnchantment, 0 } });

            if (OutputSlot.Empty)
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
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.World.Logger.Event("[KRPGEnchantment] Echanted Output {0} has a quantity of {1}", outStack.GetName(), outStack.StackSize);
            int rQty = sApi.EnchantAccessor().GetReagentQuantity(ReagentSlot.Itemstack);
            ReagentSlot.TakeOut(rQty);
            InputSlot.TakeOutWhole();
            ReagentSlot.MarkDirty();
            InputSlot.MarkDirty();
            OutputSlot.MarkDirty();
        }
        /// <summary>
        /// Smart toggle to enable EnchantInput() ticks. Sets appropriate values to true or false if CanEnchant. Be sure to MarkDirty() after to sync with clients
        /// </summary>
        private void UpdateEnchantingState()
        {
            if (Api.World == null) return;

            if (Api.Side == EnumAppSide.Server)
            {
            }

            MarkDirty();
        }
        /// <summary>
        /// Upkeep method for the Enchanting Table. Runs every 1000ms.
        /// </summary>
        /// <param name="dt"></param>
        private void TickAssessment(float dt)
        {
            if (InputSlot.Empty || ReagentSlotsEmpty == true)
                return;
            
            // Meets conditions to start Assment
            if (!IsAssessing)
            {
                InputTime = Api.World.Calendar.ElapsedHours;
                IsAssessing = true;
            }
            else if (IsAssessing)
            {
                double elapsedTime = Api.World.Calendar.ElapsedHours - InputTime;
                if (elapsedTime >= MaxAssessmentTime)
                {
                    InputTime = 0;
                    IsAssessing = false;
                }
            }
            MarkDirty();
        }
        private void TickSounds(float dt)
        {
            if (!IsAssessing || Api.Side != EnumAppSide.Client)
                return;

            Api.World.PlaySoundAt(EnchantSound, Pos, 0, null, false, 12, SoundLevel);
        }
        protected ILoadedSound ambientSound;
        public virtual float SoundLevel => 0.66f;
        public void ToggleAmbientSounds(bool on)
        {
            if (Api.Side != EnumAppSide.Client)
            {
                // Api.Logger.Event("Tried to toggle ambient enchanter sound, but was not client side.");
                return;
            }

            if (on)
            {
                if (ambientSound == null || !ambientSound.IsPlaying)
                {
                    ambientSound = ((IClientWorldAccessor)Api.World).LoadSound(new SoundParams
                    {
                        Location = EnchantSound,
                        ShouldLoop = true,
                        Position = Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                        DisposeOnFinish = false,
                        Volume = SoundLevel
                    });
                    if (ambientSound != null)
                    {
                        ambientSound.Start();
                        ambientSound.PlaybackPosition = ambientSound.SoundLengthSeconds * (float)Api.World.Rand.NextDouble();
                    }
                }
            }
            else
            {
                ambientSound?.Stop();
                ambientSound?.Dispose();
                ambientSound = null;
            }
            if (EnchantingConfigLoader.Config.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] Toggled ambient enchanter sound for Enchanting Table: {0}.", Block.BlockId);
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);

            Inventory.FromTreeAttributes(tree.GetTreeAttribute("inventory"));
            if (Api != null)
            {
                Inventory.AfterBlocksLoaded(Api.World);
            }
            InputTime = tree.GetDouble("inputEnchantTime");
            IsAssessing = tree.GetBool("isAssessing");

            if (clientDialog != null)
            {
                double elapsed = Api.World.Calendar.ElapsedHours - InputTime;
                clientDialog?.Update(elapsed, MaxAssessmentTime, IsAssessing);
            }
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            ITreeAttribute invtree = new TreeAttribute();
            Inventory.ToTreeAttributes(invtree);
            tree["inventory"] = invtree;
            tree.SetDouble("inputEnchantTime", InputTime);
            tree.SetBool("isAssessing", IsAssessing);
        }
        #endregion
        #region Events
        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            if (blockSel.SelectionBoxIndex == 1) return false;
            if (Api.Side == EnumAppSide.Server)
            {
            }
            // Setup the GUI for the client
            if (Api.Side == EnumAppSide.Client)
                toggleInventoryDialogClient(byPlayer);

            return true;
        }
        /// <summary>
        /// Master method for toggling the GUI and player's inventory
        /// </summary>
        /// <param name="byPlayer"></param>
        protected void toggleInventoryDialogClient(IPlayer byPlayer)
        {
            if (Api.Side != EnumAppSide.Client) return;

            if (clientDialog == null)
            {
                double hours = 0d;
                if (CurrentEnchantment != null && IsAssessing)
                    hours = Api.World.Calendar.TotalHours - InputTime;

                ICoreClientAPI capi = Api as ICoreClientAPI;
                clientDialog = new AssessmentTableGui(DialogTitle, Inventory, Pos, capi);
                clientDialog.OnClosed += () =>
                {
                    clientDialog = null;
                    capi.Network.SendBlockEntityPacket(Pos, (int)EnumBlockEntityPacketId.Close, null);
                    capi.Network.SendPacketClient(Inventory.Close(byPlayer));
                };
                clientDialog.OpenSound = AssetLocation.Create("sounds/block/barrelopen");
                clientDialog.CloseSound = AssetLocation.Create("sounds/block/barrelclose");

                clientDialog.TryOpen();
                capi.Network.SendPacketClient(Inventory.Open(byPlayer));
                capi.Network.SendBlockEntityPacket(Pos, (int)EnumBlockEntityPacketId.Open, null);
                MarkDirty();
            }
            else
            {
                clientDialog.TryClose();
            }
        }
        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            base.OnReceivedClientPacket(player, packetid, data);

            // Sync back to the client
            MarkDirty();
        }
        private void OnSlotModified(int slotid)
        {
            if (Api.Side == EnumAppSide.Server)
            {
                // Stop 
                IsAssessing = false;
                InputTime = 0.0d;
                MarkDirty();
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

            if (ambientSound != null)
            {
                ambientSound.Stop();
                ambientSound.Dispose();
            }

            clientDialog?.TryClose();
            clientDialog?.Dispose();
            clientDialog = null;
        }
        #endregion
    }
}