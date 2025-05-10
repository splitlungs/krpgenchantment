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
    public class EnchantingBE : BlockEntityOpenableContainer
    {
        // Config
        public int MsEnchantTick = 1000;
        public int MsSoundTick = 4000;
        public AssetLocation EnchantSound;
        private long SoundTickListener;
        // Sync'd with client
        EnchantingInventory inventory;
        public bool NowEnchanting = false;
        public double InputEnchantTime;
        public int SelectedEnchant = -1;
        public Dictionary<string, bool> Readers = new Dictionary<string, bool>();
        // Internal use
        public EnchantingTableGui clientDialog;
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
                if (CurrentRecipe == null) return false;
                if (!CurrentRecipe.Matches(Api, InputSlot, ReagentSlot)) return false;
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] Current Recipe {0} Matches ingredients.", CurrentRecipe.Name);
                Dictionary<string, int> enchantments = Api.EnchantAccessor().GetEnchantments(InputSlot.Itemstack);
                if (enchantments != null && enchantments.Count >= EnchantingConfigLoader.Config.MaxEnchantsPerItem) return false;
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] {0} can receive the {1} enchantment.", InputSlot.GetStackName(), CurrentRecipe.Name);
                return true;
            }
        }
        /// <summary>
        /// Deprecated? Gets valid Enchanting Recipes based on current InputSlot and ReagentSlot and stores them in ValidRecipes. Returns null if none found.
        /// </summary>
        /// <returns></returns>
        public List<EnchantingRecipe> ValidRecipes
        {
            get 
            {
                ICoreServerAPI sApi = (ICoreServerAPI)Api;
                return sApi.EnchantAccessor().GetValidEnchantingRecipes(InputSlot, ReagentSlot); 
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
        public double MaxEnchantTime
        {
            get { return GetMaxEnchantTime(); }
        }
        private double GetMaxEnchantTime()
        {
            double eto = -0.1d;
            // Return override first
            if (EnchantingConfigLoader.Config?.EnchantTimeOverride != null)
                eto = EnchantingConfigLoader.Config.EnchantTimeOverride;
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
                RegisterGameTickListener(TickEnchanting, MsEnchantTick);
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
            ItemStack outStack = CurrentRecipe.OutStack(Api, InputSlot, ReagentSlot).Clone();
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
            int rQty = CurrentRecipe.IngredientQuantity(ReagentSlot);
            int iQty = CurrentRecipe.IngredientQuantity(InputSlot);
            ReagentSlot.TakeOut(rQty);
            InputSlot.TakeOut(iQty);
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
                if (!NowEnchanting && CanEnchant && SelectedEnchant >= 0)
                {
                    InputEnchantTime = Api.World.Calendar.TotalHours;
                    // CurrentRecipe.ResolveIngredients(Api.World);

                    foreach (KeyValuePair<string, bool> keyValuePair in Readers)
                    {
                        if (Api.EnchantAccessor().CanReadEnchant(keyValuePair.Key, CurrentRecipe) == true)
                            Readers[keyValuePair.Key] = true;
                        else
                            Readers[keyValuePair.Key] = false;
                    }
                    NowEnchanting = true;
                    ToggleAmbientSounds(true);
                }
                else
                {
                    NowEnchanting = false;
                    CurrentRecipe = null;
                    SelectedEnchant = -1;
                    InputEnchantTime = 0;
                    foreach(KeyValuePair<string, bool> keyValuePair in Readers)
                    {
                        Readers[keyValuePair.Key] = false;
                    }
                    ToggleAmbientSounds(false);
                }
            }

            MarkDirty();
            // if (Api.Side == EnumAppSide.Client)
            // {
            //     if (NowEnchanting)
            //     {
            //         ToggleAmbientSounds(true);
            //         SoundTickListener = RegisterGameTickListener(TickSounds, MsSoundTick);
            //     }
            //     else
            //     {
            //         ToggleAmbientSounds(false);
            //         UnregisterGameTickListener(SoundTickListener);
            //     }
            // }
        }
        /// <summary>
        /// Upkeep method for the Enchanting Table. Runs every 1000ms.
        /// </summary>
        /// <param name="dt"></param>
        private void TickEnchanting(float dt)
        {
            if (NowEnchanting)
            {   
                double hours = Api.World.Calendar.TotalHours - InputEnchantTime;
                if (hours >= MaxEnchantTime)
                {
                    enchantInput();
                    UpdateEnchantingState();
                }
                MarkDirty();
            }
        }
        private void TickSounds(float dt)
        {
            if (!NowEnchanting || Api.Side != EnumAppSide.Client)
                return;

            Api.World.PlaySoundAt(EnchantSound, Pos, 0, null, false, 12, 0.75f);
            Api.World.SpawnParticles(particle);
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
            NowEnchanting = tree.GetBool("nowEnchanting");
            InputEnchantTime = tree.GetDouble("inputEnchantTime");
            SelectedEnchant = tree.GetInt("selectedEnchant");
            Readers = new Dictionary<string, bool>();
            string readerString = tree.GetString("readers");
            if (readerString != null)
            {
                string[] readers = readerString.Split(";", StringSplitOptions.RemoveEmptyEntries);
                foreach (string s in readers)
                {
                    string[] keyval = s.Split(":");
                    if (keyval[1] == "1")
                        Readers.Add(keyval[0], true);
                    else
                        Readers.Add(keyval[0], false);
                }
            }

            // Update the GUI after sync from server
            if (clientDialog != null)
            {
                // Update GUI Enchanting List
                clientDialog?.UpdateEnchantList(LatentEnchants, LatentEnchantsEncrypted);
                
                // Update main GUI
                double hours = 0;
                bool canRead = false;
                if (SelectedEnchant >= 0 && NowEnchanting == true)
                {
                    hours = Api.World.Calendar.TotalHours - InputEnchantTime;

                    ICoreClientAPI capi = Api as ICoreClientAPI;
                    string player = capi.World.Player.PlayerUID;
                    if (Readers.TryGetValue(player, out var value))
                        canRead = Readers[player];

                    // if (canRead == true)
                    //     Api.Logger.Event("Load. {0} can read the CurrentRecipe!", capi.World.Player.PlayerName);
                    // else
                    //     Api.Logger.Event("Load. {0} cannot read the CurrentRecipe!", capi.World.Player.PlayerName);
                }
                clientDialog?.Update(hours, MaxEnchantTime, NowEnchanting, OutputText, SelectedEnchant, canRead);
            }
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            ITreeAttribute invtree = new TreeAttribute();
            Inventory.ToTreeAttributes(invtree);
            tree["inventory"] = invtree;
            tree.SetBool("nowEnchanting", NowEnchanting);
            tree.SetDouble("inputEnchantTime", InputEnchantTime);
            tree.SetInt("selectedEnchant", SelectedEnchant);
            string readers = null;
            if (Readers != null)
            {
                foreach (KeyValuePair<string, bool> keyValuePair in Readers)
                {
                    // 0 = False, 1 = True
                    int val = 0;
                    if (keyValuePair.Value == true) val = 1;
                    readers += keyValuePair.Key + ":" + val + ";";
                    // Api.Logger.Event("Save. Adding a reader {0} with value of {1}.", keyValuePair.Key, val);
                }
            }
            tree.SetString("readers", readers);
        }
        #endregion
        #region Events
        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            if (blockSel.SelectionBoxIndex == 1) return false;
            if (Api.Side == EnumAppSide.Server)
            {
                // Prepare them for the translator
                if (clientDialog != null && Readers.ContainsKey(byPlayer.PlayerUID))
                {
                    Readers.Remove(byPlayer.PlayerUID);
                    if (EnchantingConfigLoader.Config.Debug == true)
                        Api.Logger.Event("[KRPGEnchantment] Server received RClick. Removing player {0} from Readers list.", byPlayer.PlayerUID);
                }
                else if (clientDialog == null && !Readers.ContainsKey(byPlayer.PlayerUID))
                {
                    bool canRead = Api.EnchantAccessor().CanReadEnchant(byPlayer.PlayerUID, CurrentRecipe);
                    Readers.Add(byPlayer.PlayerUID, canRead);
                    if (EnchantingConfigLoader.Config.Debug == true)
                        Api.Logger.Event("[KRPGEnchantment] Server received RClick. Adding player {0} from Readers list with value of {1}.", byPlayer.PlayerUID, canRead);
                }
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
                if (CurrentRecipe != null && NowEnchanting)
                    hours = Api.World.Calendar.TotalHours - InputEnchantTime;

                // bool canRead = Api.CanReadEnchant(byPlayer, CurrentRecipe);

                ICoreClientAPI capi = Api as ICoreClientAPI;
                EnchantingGuiConfig config = new EnchantingGuiConfig()
                {
                    maxEnchantTime = MaxEnchantTime,
                    outputText = OutputText,
                    inputEnchantTime = hours,
                    nowEnchanting = NowEnchanting,
                    selectedEnchant = SelectedEnchant,
                    enchantNames = LatentEnchants,
                    enchantNamesEncrypted = LatentEnchantsEncrypted,
                    rowCount = EnchantRowCount
                };
                clientDialog = new EnchantingTableGui(DialogTitle, Inventory, Pos, capi, config);
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

            // Player modified a Slot or clicked an Enchant button
            if (packetid == 1337 && Api.Side == EnumAppSide.Server)
            {
                EnchantingGuiPacket packet = SerializerUtil.Deserialize<EnchantingGuiPacket>(data);
                SelectedEnchant = packet.SelectedEnchant;
                CurrentRecipe = null;
                // Set the selected latent enchant if it's valid, or un-set them if not
                if (SelectedEnchant >= 0 && LatentEnchants != null)
                {
                    // Find and assign a Current Recipe
                    ICoreServerAPI sApi = (ICoreServerAPI)Api;
                    List <EnchantingRecipe> recipes = sApi.EnchantAccessor().GetValidEnchantingRecipes(InputSlot, ReagentSlot);
                    foreach (EnchantingRecipe er in recipes)
                    {
                        if (er.Name.ToShortString() == LatentEnchants[SelectedEnchant] && er.Matches(Api, InputSlot, ReagentSlot) != false)
                            CurrentRecipe = er.Clone();
                    }

                    // Remove button selection if Current Recipe is not found
                    if (CurrentRecipe == null)
                    {
                        if (EnchantingConfigLoader.Config?.Debug == true)
                            Api.World.Logger.Warning("[KRPGEnchantment] Selected enchant button {0} is invalid. Not setting as CurrentRecipe.", SelectedEnchant);
                        SelectedEnchant = -1;
                    }

                    // List<EnchantingRecipe> recipes = Api.GetEnchantingRecipes();
                    // if (recipes != null)
                    // {
                    //     foreach (EnchantingRecipe e in recipes)
                    //     {
                    //         if (e.Name.ToShortString() == LatentEnchants[SelectedEnchant])
                    //         {
                    //             CurrentRecipe = e.Clone();
                    //             if (EnchantingConfigLoader.Config?.Debug == true)
                    //                 Api.World.Logger.Event("[KRPGEnchantment] Found selected enchant in the registry. Setting {0} as CurrentRecipe.", e.Name);
                    //         }
                    //     }
                    // }
                    // else
                    //     Api.World.Logger.Error("[KRPGEnchantment] Could not get Recipes from the Regisitry! Mod may be corrupted. Please re-download the KRPG Enchantment and make an issue report if this continues.");
                }
                else
                {
                    if (EnchantingConfigLoader.Config?.Debug == true)
                        Api.World.Logger.Warning("[KRPGEnchantment] Selected enchant button {0} is invalid. Not setting as CurrentRecipe.", SelectedEnchant);
                    SelectedEnchant = -1;
                }
                UpdateReaders();
                UpdateEnchantingState();
            }
            if (packetid == 1338)
            {
                // Attempt to remove player from Readers list
                if (Readers.ContainsKey(player.PlayerUID))
                    Readers.Remove(player.PlayerUID);
            }
            // Sync back to the client
            MarkDirty();
        }
        /// <summary>
        /// Checks all readers in the list if they can read the Current Enchantment. Call this before calling clientDialog.Update()
        /// </summary>
        void UpdateReaders()
        {
            if (CurrentRecipe != null && Api.Side == EnumAppSide.Server)
            {
                foreach (KeyValuePair<string, bool> pair in Readers)
                {
                    // ICoreServerAPI sApi = Api as ICoreServerAPI;
                    Api.EnchantAccessor().CanReadEnchant(pair.Key, CurrentRecipe);
                }
            }
            else
            {
                foreach (KeyValuePair<string, bool> pair in Readers)
                {
                    Readers[pair.Key] = false;
                }
            }
        }
        private void OnSlotModified(int slotid)
        {
            if (Api.Side == EnumAppSide.Server)
            {
                // Reset the progress to 0 if any of the input is changed.
                if (slotid == 0 || slotid > 1)
                {
                    // Stop Enchanting
                    NowEnchanting = false;
                    CurrentRecipe = null;
                    SelectedEnchant = -1;
                    InputEnchantTime = 0.0d;
                    // ICoreServerAPI sApi = Api as ICoreServerAPI;
                    bool assed = Api.EnchantAccessor().AssessItem(InputSlot, ReagentSlot);
                    if (EnchantingConfigLoader.Config?.Debug == true)
                        if (!assed) Api.Logger.Warning("[KRPGEnchantment] EnchantingTable could not Assess an item!");
                }
                UpdateEnchantingState();
                UpdateReaders();
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
            Readers.Clear();
        }
        #endregion
    }
}