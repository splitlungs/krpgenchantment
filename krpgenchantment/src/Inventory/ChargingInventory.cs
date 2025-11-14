using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace KRPGLib.Enchantment
{
    #region Inventory
    public class ChargingInventory : InventoryBase, ISlotProvider
    {
        ItemSlot[] slots;
        public ItemSlot[] Slots { get { return slots; } }
        ChargingBE bEntity;
        public ChargingInventory(int quantitySlots, string invId, ICoreAPI api, NewSlotDelegate onNewSlot = null) : base(invId, api)
        {
            // bEntity = eTable;
            // slot 0 = Input Item
            // slot 1 - 5: Temporal Item
            slots = GenEmptySlots(quantitySlots);
        }
        public ChargingInventory(string inventoryID, ICoreAPI api, ChargingBE eTable) : base(inventoryID, api)
        {
            bEntity = eTable;
            // slot 0 = Input Item
            // slot 1 - 5: Temporal Item
            slots = GenEmptySlots(6);
        }
        public override int Count
        {
            get { return 6; }
        }
        public override ItemSlot this[int slotId]
        {
            get
            {
                return slots[slotId];
            }
            set
            {
                slots[slotId] = value;
            }
        }
        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            slots = SlotsFromTreeAttributes(tree, slots);
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            SlotsToTreeAttributes(slots, tree);
        }
        protected override ItemSlot NewSlot(int i)
        {
            if (i == 0) return new ItemSlotAssessmentInput(this, bEntity, i);
            else return new ItemSlotTemporalInput(this, bEntity, i);
        }
        public override bool CanPlayerAccess(IPlayer player, EntityPos position)
        {
            bool result = base.CanPlayerAccess(player, position);
            if (!result) return result;
            return result;
        }
        public float GetCurrentChargeSum()
        {
            float ChargeSum = 0.0;
            foreach (var slot in slots)
            {
                if (!slot.Empty) // TODO I don't have VS runtime, so specific function names are not available to me 
                {
                    // assume the slot is containing a valid charging compound,
                    // since its handled in the slot's check below
                    ChargeSum += bEntity.validChargeItems.TryGetValue(slot.Itemstack.Collectible.Code);
                }
            }
            return ChargeSum;
        }
        //would be used for slots accepting or disallowing charge compounds when over maximum
        public bool NewChargeWithinMaximum(float addedCharge)
        {
            float currentCharge = GetCurrentChargeSum();
            //two options here
            /* Option 1
            if (currentCharge < bEntity.MaxChargeValue)
            {
                return true; //Accept any value while under maximum, so the player can get max charge even if some is wasted
            }
            else
            {
                return false; //otherwise reject
            }
            */

            //Option 2
            if (currentCharge + addedCharge <= bEntity.MaxChargeValue)
            {
                return true; //Strictly allow charge within maximum
            }
            else
            {
                return false;
            }
        }
    }
    #endregion
    #region Slots
    public class ItemSlotAssessmentInput : ItemSlot
    {
        ChargingBE bEntity;
        int stackNum;
        string inputCode() 
        {
            if (!Empty) return this.itemstack?.Collectible.Code;
            else return null;
        }
        public ItemSlotAssessmentInput(InventoryBase inventory, ChargingBE assessmentTable, int itemNumber) : base(inventory)
        {
            MaxSlotStackSize = 1;
            bEntity = assessmentTable;
            stackNum = itemNumber;
        }
        public void Set(ChargingBE assessmentTable, int num)
        {
            bEntity = assessmentTable;
            stackNum = num;
        }
        public override int GetRemainingSlotSpace(ItemStack forItemstack)
        {
            return Math.Max(0, MaxSlotStackSize - StackSize);
        }
        public override bool CanHold(ItemSlot sourceSlot)
        {
            // if (bEntity.invLocked) return false;
            if (this.inventory.Api.Side != EnumAppSide.Server) return false;

            if (EnchantingConfigLoader.Config?.Debug == true)
                inventory.Api.Logger.Event("[KRPGEnchantment] Testing if ChargingTable CanHold {0}.", sourceSlot?.Itemstack?.Collectible.Code);

            if (bEntity.validReagents == null) return false;

            foreach (KeyValuePair<string, int> pair in bEntity.validReagents)
            {
                if (EnchantingConfigLoader.Config?.Debug == true)
                    inventory.Api.Logger.Event("[KRPGEnchantment] {0} is a Valid Reagent with value of {1}.", pair.Key, pair.Value);
            }

            // Limit according to code and qty in configs
            foreach (KeyValuePair<string, int> pair in bEntity.validReagents)
            {
                string code = sourceSlot?.Itemstack?.Collectible?.Code;
                string code2 = pair.Key;
                if (code == code2)
                {
                    if (EnchantingConfigLoader.Config?.Debug == true)
                        inventory.Api.Logger.Event("[KRPGEnchantment] {0} is equal to {1}.", code, code2);
                    MaxSlotStackSize = pair.Value;
                    return true;
                }
            }

            return false;
        }
        public override bool CanTake()
        {
            return base.CanTake();
        }
        public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
        {
            return base.CanTakeFrom(sourceSlot, priority);
        }
        public override void OnItemSlotModified(ItemStack stack)
        {
            base.OnItemSlotModified(stack);
        }
        protected override void ActivateSlotLeftClick(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
        {
            if (!(inventory.Api is ICoreServerAPI sapi)) return;

            base.ActivateSlotLeftClick(sourceSlot, ref op);

        }
        protected override void ActivateSlotRightClick(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
        {
            base.ActivateSlotRightClick(sourceSlot, ref op);
        }
    }
    public class ItemSlotTemporalInput : ItemSlot
    {
        ChargingBE bEntity;
        int stackNum;
        public ItemSlotTemporalInput(InventoryBase inventory, ChargingBE assessmentTable, int itemNumber) : base(inventory)
        {
            bEntity = assessmentTable;
            stackNum = itemNumber;
        }
        public override bool CanHold(ItemSlot sourceSlot)
        {
            // if (bEntity.invLocked) return false;

            //if (sourceSlot.Itemstack.Collectible.Code != "game:gear-temporal")
            //    return false;

            //return base.CanHold(sourceSlot);


            // checking every entry in the charging compound config
            foreach (KeyValuePair<string, float> chargingCompound in bEntity.validChargeItems)
            {
                if (sourceSlot.Itemstack.Collectible.Code = chargingCompound.Key)
                {
                    // could also add an option to check if the rolling sum is greater than the config's max reagent setting,
                    // and if so also reject a new reagent
                    // Would need to call through to the inventory that contains this slot, and the slot would ask the inventory for charge
                    ChargingInventory owningInventory = (ChargingInventory)bEntity.Inventory;
                    if (owningInventory.NewChargeWithinMaximum(chargingCompound.Value))
                    {
                        return base.CanHold(sourceSlot);
                    }
                    else
                    {
                        return false;
                    }

                    // otherwise just this
                    //return base.CanHold(sourceSlot);
                }
            }
            // if none applied, this is not a valid charging item
            
            return false;
        }
        public override bool CanTake()
        {
            return base.CanTake();
        }
        public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
        {
            // return sourceSlot.Itemstack?.Collectible is ReagentItem && base.CanTakeFrom(sourceSlot, priority);
            return base.CanTakeFrom(sourceSlot, priority);
        }
        protected override void ActivateSlotLeftClick(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
        {
            if (Empty)
            {
                if (CanHold(sourceSlot))
                {
                    int val = Math.Min(sourceSlot.StackSize, MaxSlotStackSize);
                    val = Math.Min(val, GetRemainingSlotSpace(sourceSlot.Itemstack));
                    itemstack = sourceSlot.TakeOut(val);
                    op.MovedQuantity = itemstack.StackSize;
                    OnItemSlotModified(itemstack);
                }

                return;
            }

            if (sourceSlot.Empty)
            {
                op.RequestedQuantity = StackSize;
                TryPutInto(sourceSlot, ref op);
                return;
            }

            int mergableQuantity = itemstack.Collectible.GetMergableQuantity(itemstack, sourceSlot.Itemstack, op.CurrentPriority);
            if (mergableQuantity > 0)
            {
                int requestedQuantity = op.RequestedQuantity;
                op.RequestedQuantity = GameMath.Min(mergableQuantity, sourceSlot.Itemstack.StackSize, GetRemainingSlotSpace(sourceSlot.Itemstack));
                ItemStackMergeOperation op2 = (ItemStackMergeOperation)(op = op.ToMergeOperation(this, sourceSlot));
                itemstack.Collectible.TryMergeStacks(op2);
                sourceSlot.OnItemSlotModified(itemstack);
                OnItemSlotModified(itemstack);
                op.RequestedQuantity = requestedQuantity;
            }
            else
            {
                TryFlipWith(sourceSlot);
            }
        }

        protected override void ActivateSlotRightClick(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
        {
            base.ActivateSlotRightClick(sourceSlot, ref op);
        }
    }
    #endregion
}
