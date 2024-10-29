using Vintagestory.API.Client;
using Vintagestory.GameContent;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace KRPGLib.Enchantment
{
    public class EnchantSelectGui : GuiDialog
    {
        public override string ToggleKeyCombinationCode => "enchantselectgui";

        public string customFont = "dragon_alphabet.ttf";
        public SKTypeface customTypeface;

        public List<string> enchantNames;
        public int selectedEnchant = -1;

        public EnchantSelectGui(ICoreClientAPI capi) : base(capi)
        {
            customTypeface = capi.LoadCustomFont(customFont);
            SetupDialog();
        }

        private void SetupDialog()
        {
            // Setup Font for Encrypted Enchantments
            if (customTypeface == null)
                customTypeface = capi.LoadCustomFont(customFont);
            // Setup the Enchants to be listed - OVERRIDE FOR TESTING
            if (enchantNames == null)
                enchantNames = new List<string>() { "chilling", "flaming", "shocking", "frost", "harming" };

            // Set up GUI elements here using the loaded custom font
            int insetWidth = 220;
            int insetHeight = 180;
            int insetDepth = 3;
            int rowHeight = 48;
            int rowCount = 3;
            // Check config for override
            if (EnchantingRecipeLoader.Config?.MaxLatentEnchants != null)
                rowCount = EnchantingRecipeLoader.Config.MaxLatentEnchants;

            // Auto-sized dialog at the center of the screen
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

            // Bounds of main inset for scrolling content in the GUI
            ElementBounds insetBounds = ElementBounds.Fixed(0, GuiStyle.TitleBarHeight, insetWidth, insetHeight);
            ElementBounds scrollbarBounds = insetBounds.RightCopy().WithFixedWidth(20);

            // Create child elements bounds for within the inset
            ElementBounds clipBounds = insetBounds.ForkContainingChild(GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding);
            ElementBounds containerBounds = insetBounds.ForkContainingChild(GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding);
            ElementBounds containerRowBounds = ElementBounds.Fixed(0, 0, insetWidth, rowHeight);

            // Dialog background bounds
            ElementBounds bgBounds = ElementBounds.Fill
                .WithFixedPadding(GuiStyle.ElementToDialogPadding)
                .WithSizing(ElementSizing.FitToChildren)
                .WithChildren(insetBounds, scrollbarBounds);

            // Create GUI with fixed bounds for each element
            SingleComposer = capi.Gui.CreateCompo("demoScrollGui", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar("Enchantments", OnTitleBarCloseClicked)
                .BeginChildElements()
                    .AddInset(insetBounds, insetDepth)
                    .BeginClip(clipBounds)
                        .AddContainer(containerBounds, "scroll-content")
                    .EndClip()
                    .AddVerticalScrollbar(OnNewScrollbarValue, scrollbarBounds, "scrollbar")
                .EndChildElements();

            // Fill the container
            GuiElementContainer scrollArea = SingleComposer.GetContainer("scroll-content");
            for (int i = 0; i < rowCount; i++)
            {
                scrollArea.Add(new SkiaToggleButtonGuiElement(capi, i, "enchant", enchantNames[i], customTypeface, OnSelectEnchant, containerRowBounds, true));
                containerRowBounds = containerRowBounds.BelowCopy();
            }
            SingleComposer.Compose();

            // After composing dialog, need to set the scrolling area heights to enable scroll behavior
            float scrollVisibleHeight = (float)clipBounds.fixedHeight;
            float scrollTotalHeight = rowHeight * rowCount;
            SingleComposer.GetScrollbar("scrollbar").SetHeights(scrollVisibleHeight, scrollTotalHeight);
        }
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
        public void OnSlotModified()
        {

        }
        private void OnTitleBarCloseClicked()
        {
            TryClose();
        }
        public override void Dispose()
        {
            customTypeface.Dispose();
            base.Dispose();
        }
    }
}