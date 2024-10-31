using Cairo;
using SkiaSharp;
using System;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace KRPGLib.Enchantment
{
    public class GuiDialogEnchantingBE : GuiDialogBlockEntity
    {
        long lastRedrawMs;
        string outputText;
        double inputEnchantTime;
        double maxEnchantTime;
        bool nowEnchanting;

        // protected override double FloatyDialogPosition => 0.75;
        //public GuiDialogEnchantingBE(string DialogTitle, InventoryBase Inventory, BlockPos BlockEntityPosition, ICoreClientAPI capi)
        public GuiDialogEnchantingBE(string DialogTitle, double inputProcessTime, double maxProcessTime, bool isEnchanting, string outputText, InventoryBase Inventory, BlockPos BlockEntityPosition, ICoreClientAPI capi)
            : base(DialogTitle, Inventory, BlockEntityPosition, capi)
        {
            if (IsDuplicate) return;

            this.outputText = outputText;
            this.inputEnchantTime = inputProcessTime;
            this.maxEnchantTime = maxProcessTime;
            this.nowEnchanting = isEnchanting;

            capi.World.Player.InventoryManager.OpenInventory(Inventory);

            SetupDialog();
        }

        void SetupDialog()
        {
            ItemSlot hoveredSlot = capi.World.Player.InventoryManager.CurrentHoveredSlot;
            if (hoveredSlot != null && hoveredSlot.Inventory == Inventory)
            {
                capi.Input.TriggerOnMouseLeaveSlot(hoveredSlot);
            }
            else
            {
                hoveredSlot = null;
            }

            ElementBounds reagentSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 30 + 45, 6, 1);
            reagentSlotBounds.fixedHeight += 10;

            double top = reagentSlotBounds.fixedHeight + reagentSlotBounds.fixedY;

            ElementBounds arrowBounds = ElementBounds.Fixed(0, top - 30, 200, 90);

            ElementBounds inputSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, top, 1, 1);

            ElementBounds outputSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 153, top, 1, 1);

            ElementBounds enchantButton = ElementBounds.Fixed(145, 90, 64, 24);

            // 2. Around all that is 10 pixel padding
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;

            // 3. Dialog
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightMiddle)
                .WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0);

            // 4. Enchanting Recipe Data
            string ot = Lang.Get("krpgenchantment:krpg-enchanter-enchant-prefix");
            if (outputText != null)
            { ot = outputText; }

            ClearComposers();

            SingleComposer = capi.Gui
                .CreateCompo("enchantingtablebe" + BlockEntityPosition, dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(DialogTitle, OnTitleBarClose)
                .BeginChildElements(bgBounds)
                    .AddToggleButton(Lang.Get("krpgenchantment:krpg-enchant"), CairoFont.WhiteDetailText().WithOrientation(EnumTextOrientation.Left), onEnchantToggle, enchantButton, "enchantToggle")
                    .AddDynamicCustomDraw(arrowBounds, OnBgDraw, "symbolDrawer")
                    .AddDynamicText(ot, CairoFont.WhiteDetailText().WithOrientation(EnumTextOrientation.Left), ElementBounds.Fixed(0, 30, 210, 45), "outputText")
                    .AddItemSlotGrid(Inventory, SendInvPacket, 1, new int[] { 0 }, inputSlotBounds, "inputSlot")
                    .AddItemSlotGrid(Inventory, SendInvPacket, 1, new int[] { 2 }, reagentSlotBounds, "reagentSlot")
                    .AddItemSlotGrid(Inventory, SendInvPacket, 1, new int[] { 1 }, outputSlotBounds, "outputSlot")
                .EndChildElements()
                .Compose()
            ;

            SingleComposer.GetDynamicText("outputText").SetNewText(outputText);
            SingleComposer.GetToggleButton("enchantToggle").SetValue(nowEnchanting);
            SingleComposer.GetCustomDraw("symbolDrawer").Redraw();

            lastRedrawMs = capi.ElapsedMilliseconds;

            if (hoveredSlot != null)
            {
                SingleComposer.OnMouseMove(new MouseEvent(capi.Input.MouseX, capi.Input.MouseY));
            }
        }

        private void OnNewScrollbarValue(float value)
        {
            ElementBounds bounds = SingleComposer.GetContainer("scroll-content").Bounds;
            bounds.fixedY = 5 - value;
            bounds.CalcWorldBounds();
        }

        private void onEnchantToggle(bool state)
        {
            EnchantingBE enchanter = capi.World.BlockAccessor.GetBlockEntity(BlockEntityPosition) as EnchantingBE;
            capi.Network.SendBlockEntityPacket(enchanter.Pos, 1337);
        }

        public void Update(double inputProcessTime, double maxProcessTime, bool isEnchanting, string outputText, ICoreAPI api)
        {
            this.outputText = outputText;
            this.inputEnchantTime = inputProcessTime;
            this.maxEnchantTime = maxProcessTime;
            this.nowEnchanting = isEnchanting;
            
            if (!IsOpened()) return;

            SingleComposer.GetDynamicText("outputText").SetNewText(outputText, true, true);
            SingleComposer.GetToggleButton("enchantToggle").SetValue(isEnchanting);

            if (!isEnchanting) return;

            if (capi.ElapsedMilliseconds - lastRedrawMs > 1000)
            {
                if (SingleComposer != null) SingleComposer.GetCustomDraw("symbolDrawer").Redraw();
                lastRedrawMs = capi.ElapsedMilliseconds;
            }
            
        }

        private void OnBgDraw(Context ctx, ImageSurface surface, ElementBounds currentBounds)
        {
            double top = 30;

            // Arrow Right
            ctx.Save();
            Matrix m = ctx.Matrix;
            m.Translate(GuiElement.scaled(63), GuiElement.scaled(top + 2));
            m.Scale(GuiElement.scaled(0.6), GuiElement.scaled(0.6));
            ctx.Matrix = m;
            capi.Gui.Icons.DrawArrowRight(ctx, 2);

            double dx = inputEnchantTime / maxEnchantTime;

            ctx.Rectangle(GuiElement.scaled(5), 0, GuiElement.scaled(125 * dx), GuiElement.scaled(100));
            ctx.Clip();
            LinearGradient gradient = new LinearGradient(0, 0, GuiElement.scaled(200), 0);
            gradient.AddColorStop(0, new Color(0, 0.4, 0, 1));
            gradient.AddColorStop(1, new Color(0.2, 0.6, 0.2, 1));
            ctx.SetSource(gradient);
            capi.Gui.Icons.DrawArrowRight(ctx, 0, false, false);
            gradient.Dispose();
            ctx.Restore();
        }

        private void OnInventorySlotModified(int slotid)
        {
            // Direct call can cause InvalidOperationException
            capi.Event.EnqueueMainThreadTask(SetupDialog, "setupenchanterdlg");
        }

        private void SendInvPacket(object packet)
        {
            capi.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z, packet);
        }

        private void OnTitleBarClose()
        {
            TryClose();
        }

        public override void OnGuiOpened()
        {
            base.OnGuiOpened();
            Inventory.SlotModified += OnInventorySlotModified;
            Update(inputEnchantTime, maxEnchantTime, nowEnchanting, outputText, capi);
        }

        public override void OnGuiClosed()
        {
            Inventory.SlotModified -= OnInventorySlotModified;

            SingleComposer.GetSlotGrid("inputSlot").OnGuiClosed(capi);
            SingleComposer.GetSlotGrid("outputSlot").OnGuiClosed(capi);
            SingleComposer.GetSlotGrid("reagentSlot").OnGuiClosed(capi);

            base.OnGuiClosed();
        }
    }
}