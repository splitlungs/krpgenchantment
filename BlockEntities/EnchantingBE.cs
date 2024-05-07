﻿using Cairo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace KRPGLib.Enchantment
{
    public class EnchantingBE : BlockEntityOpenableContainer
    {
        GuiDialogEnchantingBE clientDialog;
        EnchantingInventory inventory;
        public int msEnchantTick = 3000;
        public double inputEnchantTime;
        public double prevInputEnchantTime;
        public double maxEnchantTime = 1;
        public bool nowEnchanting = false;
        // public bool invLocked = false;
        // public ItemStack[] lockedInv = new ItemStack[3];
        public EnchantingRecipe CurrentRecipe;
        int nowOutputFace;

        // Server side only
        // Dictionary<string, long> playersEnchanting = new Dictionary<string, long>();
        // Client and serverside
        // int quantityPlayersEnchanting;
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

            int power = 0;
            power += slot.Itemstack.Attributes.GetInt("chilling", 0);
            power += slot.Itemstack.Attributes.GetInt("igniting", 0);
            power += slot.Itemstack.Attributes.GetInt("lightning", 0);
            power += slot.Itemstack.Attributes.GetInt("pit", 0);

            if (power < 0) return false;

            return true;
        }
        private bool CanEnchant
        {
            get
            {
                // GetMatchingEnchantingRecipe();
                if (CurrentRecipe == null) return false;
                if (!CurrentRecipe.Matches(InputSlot, ReagentSlot)) return false;

                return true;
            }
        }
        /// <summary>
        /// Returns Matching EnchantingRecipe and sets to the CurrentRecipe
        /// </summary>
        /// <returns></returns>
        public EnchantingRecipe GetMatchingEnchantingRecipe()
        {
            for (int i = 0; i < EnchantingRecipeSystem.EnchantingRecipes.Count; i++)
            {
                if (EnchantingRecipeSystem.EnchantingRecipes[i].Matches(InputSlot, ReagentSlot))
                {
                    CurrentRecipe = EnchantingRecipeSystem.EnchantingRecipes[i].Clone();
                    maxEnchantTime = CurrentRecipe.processingHours;

                    return CurrentRecipe;
                }
            }

            CurrentRecipe = null;
            return null;
        }
        /// <summary>
        /// Gets Name of Matching Enchanting Recipe with prefix
        /// </summary>
        /// <returns></returns>
        public string GetOutputText()
        {
            GetMatchingEnchantingRecipe();
            string outText = Lang.Get("krpgenchantment:krpg-enchanter-enchant-prefix");

            if (CurrentRecipe != null)
                return outText + Lang.Get(CurrentRecipe.Name.ToShortString());
            else
                return outText;
        }
        #endregion
        #region Main
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            Api = api;

            inventory.LateInitialize(Block.FirstCodePart() + "-" + Pos.X + "/" + Pos.Y + "/" + Pos.Z, api);
            
            // Can we listen only when Enchanting? Maybe use an event?
            if (api.Side == EnumAppSide.Server)
                RegisterGameTickListener(TickEnchanting, 3000);
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
            GetMatchingEnchantingRecipe();
            
            double hours = 0d;

            if (CanEnchant && nowEnchanting)
            {
                hours = Api.World.Calendar.TotalHours - inputEnchantTime;

                if (hours >= maxEnchantTime)
                {
                    enchantInput();

                    nowEnchanting = false;
                }
            }
            
            if (Api.Side == EnumAppSide.Client && clientDialog != null)
                clientDialog.Update(hours, maxEnchantTime, nowEnchanting, GetOutputText(), Api);

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
                GetMatchingEnchantingRecipe();
            }
            if (Api?.Side == EnumAppSide.Client)
            {
                // currentMesh = GenMesh();
                
                // invDialog?.UpdateContents();
                double hours = Api.World.Calendar.TotalHours - inputEnchantTime;
                if (clientDialog != null)
                    clientDialog.Update(hours, maxEnchantTime, nowEnchanting, GetOutputText(), Api);

                MarkDirty(true);
            }
        }
        void SetDialogValues(ITreeAttribute dialogTree)
        {

            dialogTree.SetDouble("inputEnchantTime", inputEnchantTime);
            dialogTree.SetDouble("maxEnchantTime", maxEnchantTime);
            dialogTree.SetBool("nowEnchanting", nowEnchanting);
            dialogTree.SetString("outputText", GetOutputText());
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
        #endregion
        #region Events
        private void OnSlotModified(int slotid)
        {
            // Stop Enchanting
            nowEnchanting = false;

            GetMatchingEnchantingRecipe();

            double hours = Api.World.Calendar.TotalHours - inputEnchantTime;

            if (slotid == 0 || slotid > 1)
            {
                inputEnchantTime = 0.0d; //reset the progress to 0 if any of the input is changed.
                hours = 0d;
                MarkDirty();
            }

            if (slotid == 1)
            {
                hours = 0d;
                MarkDirty();
                // clientDialog.SingleComposer.ReCompose();
            }

            if (Api?.Side == EnumAppSide.Client)
            {

                clientDialog.Update(hours, maxEnchantTime, nowEnchanting, GetOutputText(), Api);
            }
        }
        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            if (blockSel.SelectionBoxIndex == 1) return false;

            GetMatchingEnchantingRecipe();

            double hours = 0d;
            if (CurrentRecipe != null && nowEnchanting)
                 hours = Api.World.Calendar.TotalHours - inputEnchantTime;
            string outText = GetOutputText();

            if (Api.Side == EnumAppSide.Client)
            {
                toggleInventoryDialogClient(byPlayer, () => {
                    clientDialog = new GuiDialogEnchantingBE(DialogTitle, hours, maxEnchantTime, nowEnchanting, GetOutputText(), Inventory, Pos, Api as ICoreClientAPI);
                    //clientDialog = new GuiDialogEnchantingBE(DialogTitle, Inventory, Pos, Api as ICoreClientAPI);
                    clientDialog.Update(hours, maxEnchantTime, nowEnchanting, outText, Api);
                    return clientDialog;
                });
                
            }

            /*
            if (Api.World is IServerWorldAccessor)
            {

                ((ICoreServerAPI)Api).Network.SendBlockEntityPacket(
                    (IServerPlayer)byPlayer,
                    Pos.X, Pos.Y, Pos.Z,
                    (int)EnumBlockEntityPacketId.Open,
                    null
                );

                //byPlayer.InventoryManager.OpenInventory(inventory);
                // MarkDirty();
            }*/

            return true;
        }

        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            base.OnReceivedClientPacket(player, packetid, data);

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

            // invDialog?.TryClose();
            // invDialog = null;
        }
        #endregion
    }
}