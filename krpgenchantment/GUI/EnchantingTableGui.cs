using Vintagestory.API.Client;
using Vintagestory.GameContent;
using SkiaSharp;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Cairo;
using Vintagestory.API.Config;
using System.ComponentModel;
using Vintagestory.API.Util;

namespace KRPGLib.Enchantment
{
    public class EnchantingTableGui : GuiDialogBlockEntity
    {
        public override string ToggleKeyCombinationCode => "enchantingtablegui";

        public string customFont = "dragon_alphabet.ttf";
        public SKTypeface customTypeface;

        public List<int> enchantButtons;
        public List<string> enchantNames;
        public int selectedEnchant = -1;

        long lastRedrawMs;
        string outputText;
        double inputEnchantTime;
        double maxEnchantTime;
        bool nowEnchanting;

        // Set GUI Element Bounds sizing
        int inputWidth = 480;
        int inputHeight = 180;
        int insetWidth = 220;
        int insetHeight = 180;
        int insetDepth = 3;
        int rowHeight = 48;
        int rowCount
        { get { if (EnchantingRecipeLoader.Config?.MaxLatentEnchants != null) return EnchantingRecipeLoader.Config.MaxLatentEnchants; else return 3; } }

        public EnchantingTableGui(string DialogTitle, double inputProcessTime, double maxProcessTime, bool isEnchanting, string outputText, InventoryBase Inventory, BlockPos BlockEntityPosition, ICoreClientAPI capi) 
            : base(DialogTitle, Inventory, BlockEntityPosition, capi)
        {
            if (IsDuplicate) return;

            this.capi = capi;
            this.outputText = outputText;
            this.inputEnchantTime = inputProcessTime;
            this.maxEnchantTime = maxProcessTime;
            this.nowEnchanting = isEnchanting;

            capi.World.Player.InventoryManager.OpenInventory(Inventory);
            customTypeface = capi.LoadCustomFont(customFont);
            SetupDialog();
        }

        public void SetupDialog()
        {
            // 1. Setup Enchanting Input/Output
            ItemSlot hoveredSlot = capi.World.Player.InventoryManager.CurrentHoveredSlot;
            if (hoveredSlot != null && hoveredSlot.Inventory == Inventory)
                capi.Input.TriggerOnMouseLeaveSlot(hoveredSlot);
            else
                hoveredSlot = null;
            ElementBounds reagentSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 30 + 45, 6, 1);
            reagentSlotBounds.fixedHeight += 10;
            double top = reagentSlotBounds.fixedHeight + reagentSlotBounds.fixedY;
            ElementBounds arrowBounds = ElementBounds.Fixed(0, top - 30, 200, 90);
            ElementBounds inputSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, top, 1, 1);
            ElementBounds outputSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 153, top, 1, 1);
            ElementBounds enchantButton = ElementBounds.Fixed(145, 90, 64, 24);

            // 2. Setup Enchant Selector
            // Setup Font for Encrypted Enchantments
            if (customTypeface == null)
                customTypeface = capi.LoadCustomFont(customFont);
            // Create a new List of Latent Enchants if none found
            if (enchantNames == null)
                enchantNames = new List<string>(3) { "", "", "" };
            // Setup the Enchants to be listed - OVERRIDE FOR TESTING
            // if (enchantNames == null)
            //     enchantNames = new List<string>() { "chilling", "flaming", "shocking", "frost", "harming" };

            // Auto-sized dialog at the center of the screen
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
            // Bounds of the main Dialog
            ElementBounds inputBounds = ElementBounds.Fixed(10, GuiStyle.TitleBarHeight -20, inputWidth, inputHeight);
            // Bounds of main inset for scrolling content in the GUI
            ElementBounds enchantListBounds = ElementBounds.Fixed(inputWidth - insetWidth, GuiStyle.TitleBarHeight, insetWidth, insetHeight);
            ElementBounds scrollbarBounds = enchantListBounds.RightCopy().WithFixedWidth(20);
            // Create child elements bounds for within the inset
            ElementBounds clipBounds = enchantListBounds.ForkContainingChild(GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding);
            ElementBounds containerBounds = enchantListBounds.ForkContainingChild(GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding);
            ElementBounds containerRowBounds = ElementBounds.Fixed(0, 0, insetWidth, rowHeight);

            // 3. Enchanting Recipe Data
            string ot = Lang.Get("krpgenchantment:krpg-enchanter-enchant-prefix");
            if (outputText != null)
            { ot = outputText; }

            // Dialog background bounds
            ElementBounds bgBounds = ElementBounds.Fill
                .WithFixedPadding(GuiStyle.ElementToDialogPadding)
                .WithSizing(ElementSizing.FitToChildren)
                .WithChildren(inputBounds, enchantListBounds, scrollbarBounds);

            ClearComposers();

            // 4. Create GUI with fixed bounds for each element
            SingleComposer = capi.Gui.CreateCompo("enchantingtablebe" + BlockEntityPosition, dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(DialogTitle, OnTitleBarCloseClicked)
                // 4a. Create GUI for Input
                .BeginChildElements(inputBounds)
                    .AddToggleButton(Lang.Get("krpgenchantment:krpg-enchant"), CairoFont.WhiteDetailText().WithOrientation(EnumTextOrientation.Left), onEnchantToggle, enchantButton, "enchantToggle")
                    .AddDynamicCustomDraw(arrowBounds, OnBgDraw, "symbolDrawer")
                    .AddDynamicText(ot, CairoFont.WhiteSmallishText().WithOrientation(EnumTextOrientation.Left), ElementBounds.Fixed(0, 30, 210, 45), "outputText")
                    .AddItemSlotGrid(Inventory, SendInvPacket, 1, new int[] { 0 }, inputSlotBounds, "inputSlot")
                    .AddItemSlotGrid(Inventory, SendInvPacket, 1, new int[] { 2 }, reagentSlotBounds, "reagentSlot")
                    .AddItemSlotGrid(Inventory, SendInvPacket, 1, new int[] { 1 }, outputSlotBounds, "outputSlot")
                .EndChildElements()
                // 4b. Create GUI for Enchant List
                .BeginChildElements()
                    .AddInset(enchantListBounds, insetDepth)
                    .BeginClip(clipBounds)
                        .AddContainer(containerBounds, "scroll-content")
                    .EndClip()
                    .AddVerticalScrollbar(OnNewScrollbarValue, scrollbarBounds, "scrollbar")
                .EndChildElements();

            // 5. Fill the container
            GuiElementContainer scrollArea = SingleComposer.GetContainer("scroll-content");
            for (int i = 0; i < rowCount; i++)
            {
                scrollArea.Add(new SkiaToggleButtonGuiElement(capi, i, "enchant", Lang.Get(enchantNames[i]), customTypeface, OnSelectEnchant, containerRowBounds, true));
                containerRowBounds = containerRowBounds.BelowCopy();
            }

            // 6. Compose
            SingleComposer.Compose();

            // 7. Get Interactables
            SingleComposer.GetDynamicText("outputText").SetNewText(outputText);
            SingleComposer.GetToggleButton("enchantToggle").SetValue(nowEnchanting);
            SingleComposer.GetCustomDraw("symbolDrawer").Redraw();

            lastRedrawMs = capi.ElapsedMilliseconds;

            if (hoveredSlot != null)
            {
                SingleComposer.OnMouseMove(new MouseEvent(capi.Input.MouseX, capi.Input.MouseY));
            }

            // 8. After composing dialog, need to set the scrolling area heights to enable scroll behavior
            float scrollVisibleHeight = (float)clipBounds.fixedHeight;
            float scrollTotalHeight = rowHeight * rowCount;
            SingleComposer.GetScrollbar("scrollbar").SetHeights(scrollVisibleHeight, scrollTotalHeight);
        }
        /// <summary>
        /// Updates the SkiaToggleButtons. Be sure to ReCompose separately.
        /// </summary>
        /// <param name="enchants"></param>
        public void UpdateEnchantList(List<string> enchants)
        {
            if (enchants != null)
                this.enchantNames = enchants;
            else
                this.enchantNames = null;
            
            // Clean and Refill the Container
            GuiElementContainer scrollArea = SingleComposer.GetContainer("scroll-content");
            for (int i = 0; i < rowCount; i++)
            {
                SkiaToggleButtonGuiElement element = scrollArea.Elements[i] as SkiaToggleButtonGuiElement;
                if (enchantNames != null)
                {
                    element.textToRender = Lang.Get(enchants[i]);
                    element.Toggleable = true;
                    element.SetValue(false);
                }
                else
                {
                    element.textToRender = "";
                    element.Toggleable = false;
                    element.SetValue(false);
                }
            }
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
        #region Events
        private void OnNewScrollbarValue(float value)
        {
            ElementBounds bounds = SingleComposer.GetContainer("scroll-content").Bounds;
            bounds.fixedY = 5 - value;
            bounds.CalcWorldBounds();
        }
        private void OnSelectEnchant(bool state, int index)
        {
            GuiElementContainer scrollArea = SingleComposer.GetContainer("scroll-content");
            for (int i = 0; i < scrollArea.Elements.Count; i++)
            {
                SkiaToggleButtonGuiElement skiaButton = scrollArea.Elements[i] as SkiaToggleButtonGuiElement;
                // Button is the one clicked AND is NOT depressed
                if (skiaButton.ButtonID == index && !skiaButton.On)
                {
                    skiaButton.SetValue(false);
                    selectedEnchant = index;
                }
                // Button is the one clicked AND IS depressed
                else if (skiaButton.ButtonID == index && skiaButton.On)
                {
                    skiaButton.SetValue(true);
                    selectedEnchant = -1;
                }
                else
                    skiaButton.SetValue(false);
            }
            capi.Gui.PlaySound("toggleswitch");
        }
        private void onEnchantToggle(bool state)
        {
            EnchantingBE enchanter = capi.World.BlockAccessor.GetBlockEntity(BlockEntityPosition) as EnchantingBE;
            capi.Network.SendBlockEntityPacket(enchanter.Pos, 1337);
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
        public void OnInventorySlotModified(int slotid)
        {
            // Direct call can cause InvalidOperationException
            capi.Event.EnqueueMainThreadTask(SetupDialog, "setupenchanterdlg");
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
            Inventory.SlotModified += OnInventorySlotModified;
            UpdateEnchantList(enchantNames);
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
        public override void Dispose()
        {
            customTypeface.Dispose();
            base.Dispose();
        }
        #endregion
    }
}