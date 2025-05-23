using Vintagestory.API.Client;
using Vintagestory.GameContent;
using SkiaSharp;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Cairo;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using System.Reflection;

namespace KRPGLib.Enchantment
{
    public class ChargingTableGui : GuiDialogBlockEntity
    {
        #region Setup
        public override string ToggleKeyCombinationCode => "assessmenttablegui";

        // Gear
        public bool IsAssessing = false;
        public double MaxTime;
        public double InputTime;
        private float displayedFillPercent = 0f;
        private double rotationTimeAccum = 0f;
        private long tickListenerId;
        private double lastFrameTime;
        double rotationSpeed = 30.0; // Seconds per full rotation
        // double baseColorSpeed = 0.001;
        // double maxColorSpeed = 0.1;
        // double colorSpeed = 165; // Hue change
        float lerpSpeed = 0.1f; // Fade

        // Set GUI Element Bounds sizing
        int inputWidth = 300;
        int inputHeight = 300;

        // private double[] HsvToRgb(double h, double s, double v)
        // {
        //     h = h % 1.0;  // Wrap hue around [0,1]
        //     int i = (int)(h * 6);
        //     double f = h * 6 - i;
        //     double p = v * (1 - s);
        //     double q = v * (1 - f * s);
        //     double t = v * (1 - (1 - f) * s);
        // 
        //     switch (i % 6)
        //     {
        //         case 0: return new double[] { v, t, p };
        //         case 1: return new double[] { q, v, p };
        //         case 2: return new double[] { p, v, t };
        //         case 3: return new double[] { p, q, v };
        //         case 4: return new double[] { t, p, v };
        //         case 5: return new double[] { v, p, q };
        //         default: return new double[] { 1, 1, 1 }; // fallback
        //     }
        // }

        public ChargingTableGui(string DialogTitle, InventoryBase Inventory, BlockPos BlockEntityPosition, ICoreClientAPI capi) 
            : base(DialogTitle, Inventory, BlockEntityPosition, capi)
        {
            if (IsDuplicate) return;

            this.capi = capi;
            SetupDialog();
        }

        public void SetupDialog()
        {
            // 1. Setup hover event
            ItemSlot hoveredSlot = capi.World.Player.InventoryManager.CurrentHoveredSlot;
            if (hoveredSlot != null && hoveredSlot.Inventory == Inventory)
                capi.Input.TriggerOnMouseLeaveSlot(hoveredSlot);
            else
                hoveredSlot = null;

            // 2. Sanitize data

            // 3. Set bounds
            // Auto-sized dialog at the center of the screen
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
            // Bounds of the main Dialog
            ElementBounds inputBounds = ElementBounds.Fixed(0, GuiStyle.TitleBarHeight, inputWidth, inputHeight);
            // Bounds of the dynamic background
            ElementBounds gearBounds = ElementBounds.Fixed(0, 0, 300, 300);
            // Bounds of the inputs
            ElementBounds temporalSlot1Bounds = ElementStdBounds.SlotGrid(EnumDialogArea.CenterTop, 0, 20, 1, 1);
            ElementBounds temporalSlot2Bounds = ElementStdBounds.SlotGrid(EnumDialogArea.CenterMiddle, -120, 0, 1, 1);
            ElementBounds temporalSlot3Bounds = ElementStdBounds.SlotGrid(EnumDialogArea.CenterMiddle, 120, 0, 1, 1);
            ElementBounds temporalSlot4Bounds = ElementStdBounds.SlotGrid(EnumDialogArea.CenterBottom, -80, -20, 1, 1);
            ElementBounds temporalSlot5Bounds = ElementStdBounds.SlotGrid(EnumDialogArea.CenterBottom, 80, -20, 1, 1);
            ElementBounds inputSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.CenterMiddle, 0, 0, 1, 1);

            // Dialog background bounds
            ElementBounds bgBounds = ElementBounds.Fill
                .WithFixedPadding(GuiStyle.ElementToDialogPadding)
                .WithSizing(ElementSizing.FitToChildren)
                .WithChildren(inputBounds);

            // 5. Create GUI with fixed bounds for each element
            ClearComposers();
            SingleComposer = capi.Gui.CreateCompo("assessmenttablebe" + BlockEntityPosition, dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(DialogTitle, OnTitleBarCloseClicked)
                // 5a. Create GUI for Input
                .BeginChildElements(inputBounds)
                    .AddDynamicCustomDraw(gearBounds, OnRenderGearGradient, "symbolDrawer")
                    .AddItemSlotGrid(Inventory, SendInvPacket, 1, new int[] { 0 }, inputSlotBounds, "inputSlot")
                    .AddItemSlotGrid(Inventory, SendInvPacket, 1, new int[] { 1 }, temporalSlot1Bounds, "temporalSlot1")
                    .AddItemSlotGrid(Inventory, SendInvPacket, 1, new int[] { 2 }, temporalSlot2Bounds, "temporalSlot2")
                    .AddItemSlotGrid(Inventory, SendInvPacket, 1, new int[] { 3 }, temporalSlot3Bounds, "temporalSlot3")
                    .AddItemSlotGrid(Inventory, SendInvPacket, 1, new int[] { 4 }, temporalSlot4Bounds, "temporalSlot4")
                    .AddItemSlotGrid(Inventory, SendInvPacket, 1, new int[] { 5 }, temporalSlot5Bounds, "temporalSlot5")
                .EndChildElements();
            
            // 7. Compose
            SingleComposer.Compose();

            if (hoveredSlot != null)
            {
                SingleComposer.OnMouseMove(new MouseEvent(capi.Input.MouseX, capi.Input.MouseY));
            }
        }
        public void Update(double inputTime, double maxTime, bool isAssessing)
        {
            IsAssessing = isAssessing;
            InputTime = inputTime;
            MaxTime = maxTime;
        }

        private void OnTickFrame(float dt)
        {
            if (!IsAssessing) return;

            double now = capi.World.Calendar.ElapsedSeconds;
            double deltaTime = now - lastFrameTime;
            lastFrameTime = now;

            rotationTimeAccum += deltaTime;

            float targetFill = (float)(MaxTime > 0 ? Math.Clamp(InputTime / MaxTime, 0, 1) : 0);
            displayedFillPercent += (targetFill - displayedFillPercent) * (float)(lerpSpeed * deltaTime);

            if (IsOpened())
            {
                SingleComposer.GetCustomDraw("symbolDrawer")?.Redraw();
            }
        }

        #endregion
        #region Events
        private void OnRenderGearGradient(Context cr, ImageSurface surface, ElementBounds bounds)
        {
            if (!IsAssessing) return;

            cr.Save();

            // Step 1: Clear the previous draw to prevent flickering
            cr.SetSourceRGBA(0, 0, 0, 0);
            cr.Operator = Operator.Source;
            cr.Paint();
            cr.Operator = Operator.Over;

            // Step 2: Use interpolated fill percent for alpha and rotation
            float fillPercent = displayedFillPercent;
            double alpha = Math.Clamp(fillPercent, 0, 1);

            double centerX = bounds.OuterWidth / 2.0;
            double centerY = bounds.OuterHeight / 2.0;

            double angleRadians = (rotationTimeAccum / rotationSpeed) * 2 * Math.PI;

            cr.Translate(centerX, centerY);
            cr.Rotate(angleRadians);

            // double[] rgba = new double[] { 0.7, 0.4, 1.0, alpha }; // Purple
            double[] rgba = new double[] { 0.2, 0.7, 0.5, alpha }; // Aquamarine tEmPoRaL
            // double speedFactor = baseColorSpeed + (maxColorSpeed - baseColorSpeed) * fillPercent;

            // double hue = (rotationTimeAccum / 8.0) % 1.0;
            // double hue = (rotationTimeAccum * speedFactor) % 1.0;

            // double[] rgb = HsvToRgb(hue, 1.0, 1.0);
            // double[] rgb = HsvToRgb(colorSpeed, 1.0, 1.0);
            // double[] rgba = new double[] { rgb[0], rgb[1], rgb[2], alpha };

            int x = -(int)(bounds.OuterWidth / 2);
            int y = -(int)(bounds.OuterHeight / 2);
            float w = (float)bounds.OuterWidth;
            float h = (float)bounds.OuterHeight;

            capi.Gui.Icons.DrawVSGear(
                cr,
                surface,
                x, y,
                w, h,
                rgba
            );

            cr.Restore();
        }

        public void OnInventorySlotModified(int slotid)
        {
            // Notify the table which one we clicked
            // Config.selectedEnchant = -1;
            // Config.inputEnchantTime = 0;
            // EnchantingGuiPacket packet = new EnchantingGuiPacket() { SelectedEnchant = Config.selectedEnchant };
            // byte[] data = SerializerUtil.Serialize(packet);
            // capi.Network.SendBlockEntityPacket(BlockEntityPosition, 1337, data);
        }
        private void SendInvPacket(object packet)
        {
            capi.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z, packet);
        }
        private void OnTitleBarCloseClicked()
        {
            TryClose();
        }
        public override void OnGuiOpened()
        {
            base.OnGuiOpened();
            // Inventory.SlotModified += OnInventorySlotModified;

            lastFrameTime = capi.World.Calendar.ElapsedSeconds;
            // Roughly 60 FPS, SUPPOSEDLY. If you believe the machines.
            tickListenerId = capi.World.RegisterGameTickListener(OnTickFrame, 16);
        }

        public override void OnGuiClosed()
        {
            // Inventory.SlotModified -= OnInventorySlotModified;

            SingleComposer.GetSlotGrid("inputSlot").OnGuiClosed(capi);
            SingleComposer.GetSlotGrid("temporalSlot1").OnGuiClosed(capi); 
            SingleComposer.GetSlotGrid("temporalSlot2").OnGuiClosed(capi);
            SingleComposer.GetSlotGrid("temporalSlot3").OnGuiClosed(capi);
            SingleComposer.GetSlotGrid("temporalSlot4").OnGuiClosed(capi);
            SingleComposer.GetSlotGrid("temporalSlot5").OnGuiClosed(capi);

            capi.World.UnregisterGameTickListener(tickListenerId);

            base.OnGuiClosed();
        }
        public override void Dispose()
        {
            base.Dispose();
            TryClose();
        }
        #endregion
    }
}