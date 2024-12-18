﻿using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace KRPGLib.Enchantment
{
    #region Inventory
    public class EnchantingInventory : InventoryBase, ISlotProvider
    {
        ItemSlot[] slots;
        public ItemSlot[] Slots { get { return slots; } }
        EnchantingBE enchanter;
        public EnchantingInventory(int quantitySlots, string invId, ICoreAPI api, NewSlotDelegate onNewSlot = null) : base(invId, api)
        {
            // enchanter = eTable;
            // slot 0 = Input Item
            // slot 1 = Output
            // slot 2 = Input Reagent
            slots = GenEmptySlots(quantitySlots);
        }
        public EnchantingInventory(string inventoryID, ICoreAPI api, EnchantingBE eTable) : base(inventoryID, api)
        {
            enchanter = eTable;
            // slot 0 = Input Item
            // slot 1 = Output
            // slot 2 = Input Reagent
            slots = GenEmptySlots(3);
        }
        public override int Count
        {
            get { return 3; }
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
            if (i == 0) return new ItemSlotEnchantInput(this, enchanter, i);
            else if (i == 1) return new ItemSlotEnchantOutput(this, enchanter, i);
            else return new ItemSlotReagentInput(this, enchanter, i);
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
    public class ItemSlotEnchantInput : ItemSlot
    {
        EnchantingBE enchanter;
        int stackNum;

        public ItemSlotEnchantInput(InventoryBase inventory, EnchantingBE enchantingTable, int itemNumber) : base(inventory)
        {
            // MaxSlotStackSize = 1;
            enchanter = enchantingTable;
            stackNum = itemNumber;
        }
        public void Set(EnchantingBE enchantingTable, int num)
        {
            enchanter = enchantingTable;
            stackNum = num;
        }
        public override bool CanHold(ItemSlot sourceSlot)
        {
            // if (enchanter.invLocked) return false;

            return base.CanHold(sourceSlot);
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
            /*
            if (sourceSlot.Empty)
            {
                base.ActivateSlotRightClick(sourceSlot, ref op);
                return;
            }

            IWorldAccessor world = inventory.Api.World;

            if (itemstack != null && sourceSlot.Itemstack.ItemAttributes["contentItem2BlockCodes"].Exists == true)
            {
                string outBlockCode = sourceSlot.Itemstack.ItemAttributes["contentItem2BlockCodes"][itemstack.Collectible.Code.ToShortString()].AsString();

                if (outBlockCode != null)
                {
                    ItemStack outBlockStack = new ItemStack(world.GetBlock(AssetLocation.Create(outBlockCode, sourceSlot.Itemstack.Collectible.Code.Domain)));

                    if (sourceSlot.StackSize == 1)
                    {
                        sourceSlot.Itemstack = outBlockStack;
                    }
                    else
                    {
                        sourceSlot.Itemstack.StackSize--;
                        if (!op.ActingPlayer.InventoryManager.TryGiveItemstack(outBlockStack))
                        {
                            world.SpawnItemEntity(outBlockStack, op.ActingPlayer.Entity.Pos.XYZ);
                        }
                    }

                    sourceSlot.MarkDirty();
                    TakeOut(1);
                }

                return;
            }

            if (sourceSlot.Itemstack?.ItemAttributes?["contentItem2BlockCodes"].Exists == true || sourceSlot.Itemstack?.ItemAttributes?["contentItemCode"].AsString() != null) return;
            */
            base.ActivateSlotRightClick(sourceSlot, ref op);
        }
    }
    public class ItemSlotEnchantOutput : ItemSlot
    {
        EnchantingBE enchanter;
        int stackNum;
        public ItemSlotEnchantOutput(InventoryBase inventory, EnchantingBE enchantingTable, int itemNumber) : base(inventory)
        {
            enchanter = enchantingTable;
            stackNum = itemNumber;
        }
        public override bool CanHold(ItemSlot itemstackFromSourceSlot)
        {
            return false;
        }
        public override bool CanTake()
        {
            return true;
        }
        public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
        {
            return false;
        }
        public override void ActivateSlot(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
        {
            if (Empty) return;
            if (sourceSlot.CanHold(this))
            {
                if (sourceSlot.Itemstack != null && sourceSlot.Itemstack != null && sourceSlot.Itemstack.Collectible.GetMergableQuantity(sourceSlot.Itemstack, itemstack, op.CurrentPriority) < itemstack.StackSize) return;

                op.RequestedQuantity = StackSize;

                TryPutInto(sourceSlot, ref op);

                if (op.MovedQuantity > 0)
                {
                    OnItemSlotModified(itemstack);
                }
            }
        }
    }
    public class ItemSlotReagentInput : ItemSlot
    {
        EnchantingBE enchanter;
        int stackNum;
        public ItemSlotReagentInput(InventoryBase inventory, EnchantingBE enchantingTable, int itemNumber) : base(inventory)
        {
            enchanter = enchantingTable;
            stackNum = itemNumber;
        }
        public override bool CanHold(ItemSlot slot)
        {
            return base.CanHold(slot);
            /*
            bool isValid = false;
            foreach (KeyValuePair<string, int> keyValuePair in EnchantingRecipeLoader.Config?.ValidReagents)
            {
                AssetLocation asset = new AssetLocation(keyValuePair.Key);
                if (asset.Equals(slot.Itemstack?.Collectible.Code)) isValid = true;
            }
            isValid = base.CanHold(slot);
            return isValid;
            */
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

            /*
            base.ActivateSlotRightClick(sourceSlot, ref op);

            if (sourceSlot.Empty)
            {
                base.ActivateSlotRightClick(sourceSlot, ref op);
                return;
            }
            IWorldAccessor world = inventory.Api.World;

            if (itemstack != null && sourceSlot.Itemstack.ItemAttributes["contentItem2BlockCodes"].Exists == true)
            {
                string outBlockCode = sourceSlot.Itemstack.ItemAttributes["contentItem2BlockCodes"][itemstack.Collectible.Code.ToShortString()].AsString();

                if (outBlockCode != null)
                {
                    ItemStack outBlockStack = new ItemStack(world.GetBlock(AssetLocation.Create(outBlockCode, sourceSlot.Itemstack.Collectible.Code.Domain)));

                    if (sourceSlot.StackSize == 1)
                    {
                        sourceSlot.Itemstack = outBlockStack;
                    }
                    else
                    {
                        sourceSlot.Itemstack.StackSize--;
                        if (!op.ActingPlayer.InventoryManager.TryGiveItemstack(outBlockStack))
                        {
                            world.SpawnItemEntity(outBlockStack, op.ActingPlayer.Entity.Pos.XYZ);
                        }
                    }

                    sourceSlot.MarkDirty();
                    TakeOut(1);
                }

                return;
            }

            if (sourceSlot.Itemstack?.ItemAttributes?["contentItem2BlockCodes"].Exists == true || sourceSlot.Itemstack?.ItemAttributes?["contentItemCode"].AsString() != null) return;

            base.ActivateSlotRightClick(sourceSlot, ref op);
            */
        }
    }
    #endregion
}
