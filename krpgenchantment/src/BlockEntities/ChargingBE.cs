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
using System.Collections;

namespace KRPGLib.Enchantment
{
    public class ChargingBE : BlockEntityOpenableContainer
    {
        // Config
        public int MsAssessTick = 1000;
        public int MsSoundTick = 4000;
        public AssetLocation EnchantSound;
        private long SoundTickListener;
        // Sync'd with client
        ChargingInventory inventory;
        public bool IsCharging = false;
        public double InputTime = 0;
        public double Delay = 3000;
        // Internal use
        public ChargingTableGui clientDialog;
        public string CurrentEnchantment;
        int nowOutputFace;
        #region Getters
        public string Material
        {
            get { return Block.LastCodePart(); }
        }
        public virtual string DialogTitle
        {
            get { return Lang.Get("krpgenchantment:block-charging-table"); }
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
        public ItemSlot[] ReagentSlots
        {
            get { return inventory.Slots.Skip(1).ToArray(); }
        }
        public ItemSlot GetReagentSlot(int slot)
        {
            return inventory[slot];
        }
        public ChargingBE()
        {
            inventory = new ChargingInventory(null, null, this);
            inventory.SlotModified += OnSlotModified;
        }
        public bool ReagentSlotsEmpty
        {
            get { return GetReagentSlotsEmpty(); }
        }
        private bool GetReagentSlotsEmpty()
        {
            bool foundItems = true;
            for(int i = 1; i < inventory.Slots.Length; i++)
            {
                if (!inventory.Slots[i].Empty) return false;
            }
            return foundItems;
        }
        /// <summary>
        /// Gets the current recipe's processing time in in-game hours.
        /// </summary>
        /// <returns></returns>
        public double MaxChargeTime
        {
            get { return GetMaxChargeTime(); }
        }
        private double GetMaxChargeTime()
        {
            double eto = -0.1d;
            // Return override first
            if (EnchantingConfigLoader.Config?.ChargeReagentHours != null)
                eto = EnchantingConfigLoader.Config.ChargeReagentHours;
            if (eto >= 0d)
                return eto;
            return 1d;
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
                RegisterGameTickListener(TickCharge, MsAssessTick);
            }
            if (api.Side == EnumAppSide.Client)
            {
                SoundTickListener = RegisterGameTickListener(TickSounds, MsSoundTick);
            }
        }
        /// <summary>
        /// Called by the ChargingBE to write the InputSlot's EnchantPotential value
        /// </summary>
        private void chargeInput()
        {
            if (Api.Side != EnumAppSide.Server) return;

            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.World.Logger.Event("[KRPGEnchantment] Attempting to charge a reagent: {0}.", InputSlot.Itemstack.Collectible.Code);
            ICoreServerAPI sApi = Api as ICoreServerAPI;
            ItemStack outStack = InputSlot.Itemstack.Clone();
            int tGears = 0;
            for(int i = 1; i < inventory.Slots.Length; i++)
            {
                if (inventory.Slots[i].Empty) continue;

                if (inventory.Slots[i].Itemstack.Collectible.Code.Equals("gear-temporal"))
                {
                    if (EnchantingConfigLoader.Config?.Debug == true)
                        Api.World.Logger.Event("[KRPGEnchantment] Temporal Gear has been found while attempting to charge a reagent");
                    tGears++;
                    inventory.Slots[i].TakeOutWhole();
                    inventory.Slots[i].MarkDirty();
                }
            }
            int power = sApi.EnchantAccessor().SetReagentCharge(ref outStack, tGears);
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.World.Logger.Event("[KRPGEnchantment] Charged Reagent Output {0} has a power of {1}", outStack.GetName(), power);

            InputSlot.Itemstack = outStack.Clone();
            InputSlot.MarkDirty();
            MarkDirty();

        }
        /// <summary>
        /// Upkeep method for the Enchanting Table. Runs every 1000ms.
        /// </summary>
        /// <param name="dt"></param>
        private void TickCharge(float dt)
        {
            if (InputSlot.Empty || ReagentSlotsEmpty == true)
                return;
            
            // Meets conditions to start Assment
            if (!IsCharging && !InputSlot.Empty && !ReagentSlotsEmpty)
            {
                InputTime = Api.World.Calendar.ElapsedHours;
                IsCharging = true;
            }
            else if (IsCharging == true)
            {
                double elapsedTime = Api.World.Calendar.ElapsedHours - InputTime;
                if (elapsedTime >= MaxChargeTime)
                {
                    chargeInput();
                    InputTime = 0;
                    IsCharging = false;
                }
            }
            MarkDirty();
        }
        private void TickSounds(float dt)
        {
            if (!IsCharging || Api.Side != EnumAppSide.Client)
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
            InputTime = tree.GetDouble("inputTime");
            IsCharging = tree.GetBool("isCharging");

            if (clientDialog != null)
            {
                double elapsed = Api.World.Calendar.ElapsedHours - InputTime;
                clientDialog?.Update(elapsed, MaxChargeTime, IsCharging);
            }
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            ITreeAttribute invtree = new TreeAttribute();
            Inventory.ToTreeAttributes(invtree);
            tree["inventory"] = invtree;
            tree.SetDouble("inputTime", InputTime);
            tree.SetBool("isCharging", IsCharging);
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
                if (CurrentEnchantment != null && IsCharging)
                    hours = Api.World.Calendar.TotalHours - InputTime;

                ICoreClientAPI capi = Api as ICoreClientAPI;
                clientDialog = new ChargingTableGui(DialogTitle, Inventory, Pos, capi);
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
                IsCharging = false;
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