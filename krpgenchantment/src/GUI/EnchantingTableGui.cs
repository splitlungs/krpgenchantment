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

namespace KRPGLib.Enchantment
{
    public class EnchantingTableGui : GuiDialogBlockEntity
    {
        #region Setup
        public override string ToggleKeyCombinationCode => "enchantingtablegui";

        public SKTypeface customTypeface;
        public EnchantingGuiConfig Config;

        // Set GUI Element Bounds sizing
        int inputWidth = 480;
        int inputHeight = 180;
        int insetWidth = 220;
        int insetHeight = 180;
        int insetDepth = 3;
        int rowHeight = 48;

        public EnchantingTableGui(string DialogTitle, InventoryBase Inventory, BlockPos BlockEntityPosition, ICoreClientAPI capi, EnchantingGuiConfig config) 
            : base(DialogTitle, Inventory, BlockEntityPosition, capi)
        {
            if (IsDuplicate) return;

            this.capi = capi;
            Config = config.Clone();
            customTypeface = capi.LoadCustomFont(Config.customFont);
            SetupDialog();

            // capi.World.Player.InventoryManager.OpenInventory(Inventory);
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
            // Get our font. Internet required if first time
            if (customTypeface == null)
                customTypeface = capi.LoadCustomFont(Config.customFont);
            // Create a new List of Latent Enchants if none found or malformed
            if (Config.enchantNamesEncrypted == null)
            {
                Config.enchantNamesEncrypted = new List<string>();
                for (int i = 0; i < Config.rowCount; i++)
                    Config.enchantNamesEncrypted.Add("");
            }
            else if (Config.enchantNamesEncrypted.Count != Config.rowCount)
            {
                Config.enchantNamesEncrypted = new List<string>();
                for (int i = 0; i < Config.rowCount; i++)
                    Config.enchantNamesEncrypted.Add("");
            }

            // 3. Set bounds
            ElementBounds reagentSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 30 + 45, 6, 1);
            reagentSlotBounds.fixedHeight += 10;
            double top = reagentSlotBounds.fixedHeight + reagentSlotBounds.fixedY;
            ElementBounds arrowBounds = ElementBounds.Fixed(0, top - 30, 200, 90);
            ElementBounds inputSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, top, 1, 1);
            ElementBounds outputSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 153, top, 1, 1);
            // ElementBounds enchantButton = ElementBounds.Fixed(145, 90, 64, 24);
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

            // Dialog background bounds
            ElementBounds bgBounds = ElementBounds.Fill
                .WithFixedPadding(GuiStyle.ElementToDialogPadding)
                .WithSizing(ElementSizing.FitToChildren)
                .WithChildren(inputBounds, enchantListBounds, scrollbarBounds);

            // 4. Enchanting Recipe Data
            string ot = Lang.Get("krpgenchantment:krpg-enchanter-enchant-prefix");
            if (Config.outputText != null)
            { ot = Config.outputText; }

            // 5. Create GUI with fixed bounds for each element
            ClearComposers();
            SingleComposer = capi.Gui.CreateCompo("enchantingtablebe" + BlockEntityPosition, dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(DialogTitle, OnTitleBarCloseClicked)
                // 5a. Create GUI for Input
                .BeginChildElements(inputBounds)
                    // .AddToggleButton(Lang.Get("krpgenchantment:krpg-enchant"), CairoFont.WhiteDetailText().WithOrientation(EnumTextOrientation.Left), onEnchantToggle, enchantButton, "enchantToggle")
                    .AddDynamicCustomDraw(arrowBounds, OnBgDraw, "symbolDrawer")
                    .AddDynamicText(ot, CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Left), ElementBounds.Fixed(0, 30, 210, 45), "outputText")
                    .AddItemSlotGrid(Inventory, SendInvPacket, 1, new int[] { 0 }, inputSlotBounds, "inputSlot")
                    .AddItemSlotGrid(Inventory, SendInvPacket, 1, new int[] { 2 }, reagentSlotBounds, "reagentSlot")
                    .AddItemSlotGrid(Inventory, SendInvPacket, 1, new int[] { 1 }, outputSlotBounds, "outputSlot")
                .EndChildElements()
                // 5b. Create GUI for Enchant List
                .BeginChildElements()
                    .AddInset(enchantListBounds, insetDepth)
                    .BeginClip(clipBounds)
                        .AddContainer(containerBounds, "scroll-content")
                    .EndClip()
                    .AddVerticalScrollbar(OnNewScrollbarValue, scrollbarBounds, "scrollbar")
                .EndChildElements();

            // 6. Fill the container
            GuiElementContainer scrollArea = SingleComposer.GetContainer("scroll-content");
            for (int i = 0; i < Config.rowCount; i++)
            {
                scrollArea.Add(new SkiaToggleButtonGuiElement(capi, i, "", Config.enchantNamesEncrypted[i], customTypeface, OnSelectEnchant, containerRowBounds, true));
                if (Config.selectedEnchant != -1 && i == Config.selectedEnchant && Config.enchantNamesEncrypted[i] != "")
                {
                    // capi.Logger.Warning("Found matching SkiaToggleButton. Attempting to set as the selectedEnchant.");
                    SkiaToggleButtonGuiElement button = scrollArea.Elements[i] as SkiaToggleButtonGuiElement;
                    button.SetValue(true);
                }
                // SingleComposer.AddAutoSizeHoverText("TEST", CairoFont.WhiteMediumText(), 1, containerRowBounds, "enchant" + i);
                containerRowBounds = containerRowBounds.BelowCopy();
            }

            // 7. Compose
            SingleComposer.Compose();

            // 8. After composing dialog, update dynamic elements
            if (Config.canRead == true && Config.nowEnchanting == true)
                Config.outputText += Lang.Get(Config.enchantNames[Config.selectedEnchant]);
            SingleComposer.GetDynamicText("outputText").SetNewText(Config.outputText);
            // SingleComposer.GetCustomDraw("symbolDrawer").Redraw();
            // lastRedrawMs = capi.ElapsedMilliseconds;

            if (hoveredSlot != null)
            {
                SingleComposer.OnMouseMove(new MouseEvent(capi.Input.MouseX, capi.Input.MouseY));
            }

            // 9. After composing dialog, need to set the scrolling area heights to enable scroll behavior
            float scrollVisibleHeight = (float)clipBounds.fixedHeight;
            float scrollTotalHeight = rowHeight * Config.rowCount;
            SingleComposer.GetScrollbar("scrollbar").SetHeights(scrollVisibleHeight, scrollTotalHeight);
        }
        /// <summary>
        /// Updates the SkiaToggleButtons only if Config.nowEnchanting is false. Be sure to ReCompose separately. 
        /// </summary>
        /// <param name="enchants"></param>
        public void UpdateEnchantList(List<string> enchants, List<string> enchantsEncrypted)
        {
            // Don't change while we're enchanting. Be sure to set this first, or else it will fail to update
            if (Config.nowEnchanting == true) return;
            // Create a new List of Latent Enchants if none found or malformed
            if (enchants == null || enchantsEncrypted == null)
            {
                Config.enchantNames = new List<string>();
                Config.enchantNamesEncrypted = new List<string>();
                for (int i = 0; i < Config.rowCount; i++)
                {
                    Config.enchantNames.Add("");
                    Config.enchantNamesEncrypted.Add("");
                }
            }
            else if (enchants.Count != Config.rowCount || enchantsEncrypted.Count != Config.rowCount)
            {
                Config.enchantNames = new List<string>();
                Config.enchantNamesEncrypted = new List<string>();
                for (int i = 0; i < Config.rowCount; i++)
                {
                    Config.enchantNames.Add("");
                    Config.enchantNamesEncrypted.Add("");
                }
            }
            else
            {
                Config.enchantNames = enchants;
                Config.enchantNamesEncrypted = enchantsEncrypted;
            }

            // Clean and Refill the Container
            GuiElementContainer scrollArea = SingleComposer.GetContainer("scroll-content");
            for (int i = 0; i < Config.rowCount; i++)
            {
                var element = scrollArea.Elements[i] as SkiaToggleButtonGuiElement;
                // Make new buttons if needed, or config with existing values
                if (element == null)
                {
                    ElementBounds containerRowBounds = ElementBounds.Fixed(0, 0, insetWidth, rowHeight);
                    for (int j = 0; j < i; i++)
                        containerRowBounds = containerRowBounds.BelowCopy();
                    scrollArea.Add(new SkiaToggleButtonGuiElement(capi, i, "", Config.enchantNamesEncrypted[i], customTypeface, OnSelectEnchant, containerRowBounds, true));
                }
                else
                {
                    element.textToRender = Config.enchantNamesEncrypted[i];
                    element.Toggleable = true;
                    
                }
                // Set toggle if it should be selected
                if (Config.selectedEnchant == i)
                    element.SetValue(true);
                else
                    element.SetValue(false);
            }
            SingleComposer.ReCompose();
        }
        public void Update(double inputProcessTime, double maxProcessTime, bool isEnchanting, string outputText, int selected, bool canRead)
        {
            this.Config.outputText = outputText;
            this.Config.inputEnchantTime = inputProcessTime;
            this.Config.maxEnchantTime = maxProcessTime;
            this.Config.nowEnchanting = isEnchanting;
            this.Config.selectedEnchant = selected;
            this.Config.canRead = canRead;

            if (!IsOpened()) return;

            if (canRead == true)
            {
                string[] strings = Config.enchantNames[selected].Split(":");
                string s = outputText + Lang.Get("krpgenchantment:" + strings[1]);
                Config.outputText = s;
            }

            // capi.World.Logger.Event("Attempting to write OutputText: {0}", Config.outputText);
            SingleComposer.GetDynamicText("outputText").SetNewText(Config.outputText, true, true);
            // SingleComposer.GetCustomDraw("symbolDrawer").Redraw();
            SingleComposer.ReCompose();
        }
        #endregion
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
                if (skiaButton.ButtonID == index && Config.selectedEnchant == index)
                    Config.selectedEnchant = -1;
                // Button IS the one clicked
                else if (skiaButton.ButtonID == index && Config.selectedEnchant == -1)
                    Config.selectedEnchant = index;
                else
                    skiaButton.SetValue(false);
            }
            // if (Config.selectedEnchant == index)
            //     Config.selectedEnchant = -1;
            // else
            //     Config.selectedEnchant = index;
            Config.inputEnchantTime = 0;
            // SingleComposer.GetCustomDraw("symbolDrawer").Redraw();
            // Click
            capi.Gui.PlaySound("toggleswitch");
            // Notify the table which one we clicked
            EnchantingGuiPacket packet = new EnchantingGuiPacket() { SelectedEnchant = Config.selectedEnchant };
            byte[] data = SerializerUtil.Serialize(packet);
            // EnchantingBE enchanter = capi.World.BlockAccessor.GetBlockEntity(BlockEntityPosition) as EnchantingBE;
            capi.Network.SendBlockEntityPacket(BlockEntityPosition, 1337, data);
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

            double dx = Config.inputEnchantTime / Config.maxEnchantTime;

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
            // Notify the table which one we clicked
            Config.selectedEnchant = -1;
            Config.inputEnchantTime = 0;
            EnchantingGuiPacket packet = new EnchantingGuiPacket() { SelectedEnchant = Config.selectedEnchant };
            byte[] data = SerializerUtil.Serialize(packet);
            // EnchantingBE enchanter = capi.World.BlockAccessor.GetBlockEntity(BlockEntityPosition) as EnchantingBE;
            capi.Network.SendBlockEntityPacket(BlockEntityPosition, 1337, data);
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
        }

        public override void OnGuiClosed()
        {
            Inventory.SlotModified -= OnInventorySlotModified;

            SingleComposer.GetSlotGrid("inputSlot").OnGuiClosed(capi);
            SingleComposer.GetSlotGrid("outputSlot").OnGuiClosed(capi); 
            SingleComposer.GetSlotGrid("reagentSlot").OnGuiClosed(capi);

            // Notify the table to remove us from the Readers list
            capi.Network.SendBlockEntityPacket(BlockEntityPosition, 1338);

            base.OnGuiClosed();
        }
        public override void Dispose()
        {
            customTypeface.Dispose();
            base.Dispose();
            TryClose();
        }
        #endregion
    }
}