using ProtoBuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace KRPGLib.Enchantment
{
    public class ChargingBE : BlockEntityOpenableContainer, IWrenchOrientable
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
            get { return Lang.Get("krpgenchantment:block-chargingtable"); }
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
        public Dictionary<string, int> validReagents 
        { 
            get 
            {
                if (this.Api.Side != EnumAppSide.Server) return null;
                return EnchantingConfigLoader.Config?.ValidReagents; 
            }
        }
        public Dictionary<string, float> validChargeItems
        {
            get
            {
                if (this.Api.Side != EnumAppSide.Server) return null;
                return EnchantingConfigLoader.Config?.ReagentChargeComponents;
            }
        }
        public int? MaxChargeValue
        {
            get
            {
                if (this.Api.Side != EnumAppSide.Server) return null; //I dont know the purpose of this check, it may be unnecessary.
                                                                      //if so, change function return to int
                return EnchantingConfigLoader.Config?.MaxReagentCharge;
            }
        }
        public ChargingBE() : base()
        {
            inventory = new ChargingInventory(null, null, this);
            inventory.SlotModified += OnSlotModified;
        }
        /// <summary>
        /// Returns false if any slot is occupied by an item.
        /// </summary>
        public bool ReagentSlotsEmpty
        {
            get { return GetReagentSlotsEmpty(); }
        }
        /// <summary>
        /// Returns false if any slot is occupied by an item.
        /// </summary>
        /// <returns></returns>
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
            this.facing = (BlockFacing.FromCode(base.Block.Code.EndVariant()) ?? BlockFacing.NORTH);
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
        private BlockFacing facing = BlockFacing.NORTH;
        public void Rotate(EntityAgent byEntity, BlockSelection blockSel, int dir)
        {
            this.facing = ((dir > 0) ? this.facing.GetCCW() : this.facing.GetCW());
            this.Api.World.BlockAccessor.ExchangeBlock(this.Api.World.GetBlock(base.Block.CodeWithVariant("side", this.facing.Code)).Id, this.Pos);
            this.MarkDirty(true, null);
        }
        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            UnregisterAllTickListeners();
        }
        /// <summary>
        /// Called by the ChargingBE to write the InputSlot's EnchantPotential value
        /// </summary>
        private void chargeInput()
        {
            if (!(Api is ICoreServerAPI sApi)) return;

            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.World.Logger.Event("[KRPGEnchantment] Attempting to charge a reagent: {0}.", InputSlot.Itemstack.Collectible.Code);
            
            ItemStack outStack = InputSlot.Itemstack.Clone();
            int sum = GetCurrentChargeSum();
            // Escape if 0 or less. Something must have gone wrong
            if (sum <= 0)
            {
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.World.Logger.Error("[KRPGEnchantment] Failed to charge a reagent! Could not calculate the component charge total");
                return;
            }
            // Reagent prior charge
            int chargePre = Api.EnchantAccessor().GetReagentCharge(InputSlot.Itemstack);
            sum += chargePre;
            // Attempt to Set the Charge
            int power = sApi.EnchantAccessor().SetReagentCharge(ref outStack, sum);
            if (sum <= 0)
            {
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.World.Logger.Error("[KRPGEnchantment] Failed to charge reagent: {0}!", outStack.GetName());
                return;
            }
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.World.Logger.Event("[KRPGEnchantment] Charged Reagent Output {0} has a power of {1}", outStack.GetName(), power);
            // Remove all items AFTER we've confirmed we can set the charge
            foreach (var slot in inventory.Slots)
            {
                if (slot.Empty) continue;
                slot.TakeOutWhole();
                slot.MarkDirty();
            }
            // Write back
            InputSlot.Itemstack = outStack.Clone();
            InputSlot.MarkDirty();
            MarkDirty();
        }
        /// <summary>
        /// Returns the sum of all charge components, not including existing reagent charge value. Applies Global Multiplier and normalizes.
        /// </summary>
        /// <returns></returns>
        public int GetCurrentChargeSum()
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.World.Logger.Event("[KRPGEnchantment] Attempting to determine a reagent charge sum for all components in ChargingTable.");

            float gMul = EnchantingConfigLoader.Config.GlobalChargeMultiplier;
            float rCharge = 0f;
            // Component Charge - Multiply each component individually
            foreach (ItemSlot slot in ReagentSlots)
            {
                if (slot.Empty) continue;
                string s = slot.Itemstack.Collectible.Code;
                if (EnchantingConfigLoader.Config?.ReagentChargeComponents.ContainsKey(s) == true)
                {
                    float cCharge = EnchantingConfigLoader.Config.ReagentChargeComponents[s];
                    rCharge += cCharge * gMul;
                }
            }
            // Bail if we failed to get the components for some reason
            if (rCharge <= 0f) return 0;
            // Prepare returns
            int charge = (int)MathF.Floor(rCharge);

            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.World.Logger.Event("[KRPGEnchantment] Total available Charge is: {0}.", charge);

            return charge;
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
        // public void ToggleAmbientSounds(bool on)
        // {
        //     if (Api.Side != EnumAppSide.Client)
        //     {
        //         // Api.Logger.Event("Tried to toggle ambient enchanter sound, but was not client side.");
        //         return;
        //     }
        // 
        //     if (on)
        //     {
        //         if (ambientSound == null || !ambientSound.IsPlaying)
        //         {
        //             ambientSound = ((IClientWorldAccessor)Api.World).LoadSound(new SoundParams
        //             {
        //                 Location = EnchantSound,
        //                 ShouldLoop = true,
        //                 Position = Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
        //                 DisposeOnFinish = false,
        //                 Volume = SoundLevel
        //             });
        //             if (ambientSound != null)
        //             {
        //                 ambientSound.Start();
        //                 ambientSound.PlaybackPosition = ambientSound.SoundLengthSeconds * (float)Api.World.Rand.NextDouble();
        //             }
        //         }
        //     }
        //     else
        //     {
        //         ambientSound?.Stop();
        //         ambientSound?.Dispose();
        //         ambientSound = null;
        //     }
        //     if (EnchantingConfigLoader.Config.Debug == true)
        //         Api.Logger.Event("[KRPGEnchantment] Toggled ambient enchanter sound for Enchanting Table: {0}.", Block.BlockId);
        // }
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
                if (IsCharging)
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