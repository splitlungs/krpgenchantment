using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

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
        private Dictionary<string, int> validReagents = new Dictionary<string, int>();
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

            // Limit according to code and qty in configs
            foreach (KeyValuePair<string, int> pair in EnchantingConfigLoader.Config?.ValidReagents)
            {
                if (sourceSlot.Itemstack.Collectible.Code == pair.Key)
                {
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
            if (inventory.Api.Side == EnumAppSide.Client) return;

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
            MarkDirty();
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

            if (sourceSlot.Itemstack.Collectible.Code != "game:gear-temporal")
                return false;

            return base.CanHold(sourceSlot);
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
