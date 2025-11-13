using Cairo;
using SkiaSharp;
using System;
using Vintagestory.API.Client;

namespace KRPGLib.Enchantment
{
    public class SkiaToggleButtonGuiElement : SkiaTextGuiElement
    {
        private Action<bool, int> handler;
        /// <summary>
        /// ID for tracking buttons
        /// </summary>
        public int ButtonID;
        /// <summary>
        /// Is this button toggleable?
        /// </summary>
        public bool Toggleable;
        /// <summary>
        /// Is this button on?
        /// </summary>
        public bool On;
        private LoadedTexture releasedTexture;
        private LoadedTexture pressedTexture;
        private LoadedTexture hoverTexture;
        private int unscaledDepth = 4;
        private string icon;
        private double pressedYOffset;
        private double nonPressedYOffset;
        /// <summary>
        /// Is this element capable of being in the focus?
        /// </summary>
        public override bool Focusable => true;

        /// <summary>
        /// Constructor for the button
        /// </summary>
        /// <param name="capi"></param>
        /// <param name="icon"></param>
        /// <param name="text"></param>
        /// <param name="typeface"></param>
        /// <param name="OnToggled"></param>
        /// <param name="bounds"></param>
        /// <param name="toggleable"></param>
        public SkiaToggleButtonGuiElement(ICoreClientAPI capi, int buttonID, string icon, string text, SKTypeface typeface , Action<bool, int> OnToggled, 
            ElementBounds bounds, bool toggleable = false)
            : base(capi, bounds, text, typeface)
        {
            releasedTexture = new LoadedTexture(capi);
            pressedTexture = new LoadedTexture(capi);
            hoverTexture = new LoadedTexture(capi);
            handler = OnToggled;
            Toggleable = toggleable;
            this.icon = icon;
            ButtonID = buttonID;
        }
        /// <summary>
        /// Composes the element in both the pressed, and released states.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="surface"></param>
        public override void ComposeElements(Context ctx, ImageSurface surface)
        {
            Bounds.CalcWorldBounds();
            ComposeReleasedButton();
            ComposePressedButton();
            GenerateTextTexture();
        }

        private void ComposeReleasedButton()
        {
            double num = GuiElement.scaled(unscaledDepth);
            ImageSurface imageSurface = new ImageSurface(Format.Argb32, (int)Bounds.OuterWidth, (int)Bounds.OuterHeight);
            Context context = genContext(imageSurface);
            context.SetSourceRGB(GuiStyle.DialogDefaultBgColor[0], GuiStyle.DialogDefaultBgColor[1], GuiStyle.DialogDefaultBgColor[2]);
            GuiElement.RoundRectangle(context, 0.0, 0.0, Bounds.OuterWidth, Bounds.OuterHeight, GuiStyle.ElementBGRadius);
            context.FillPreserve();
            context.SetSourceRGBA(1.0, 1.0, 1.0, 0.1);
            context.Fill();
            EmbossRoundRectangleElement(context, 0.0, 0.0, Bounds.OuterWidth, Bounds.OuterHeight, inverse: false, (int)num);
            // double multilineTextHeight = GetMultilineTextHeight();
            // nonPressedYOffset = (Bounds.InnerHeight - multilineTextHeight) / 2.0 - 1.0;
            // DrawMultilineTextAt(context, Bounds.absPaddingX, Bounds.absPaddingY + nonPressedYOffset, EnumTextOrientation.Center);
            if (icon != null && icon.Length > 0)
            {
                // api.Gui.Icons.DrawIcon(context, icon, Bounds.absPaddingX + GuiElement.scaled(4.0), Bounds.absPaddingY + GuiElement.scaled(4.0), Bounds.InnerWidth - GuiElement.scaled(9.0), Bounds.InnerHeight - GuiElement.scaled(9.0), Font.Color);
            }
            generateTexture(imageSurface, ref releasedTexture);
            context.Dispose();
            imageSurface.Dispose();
        }

        private void ComposePressedButton()
        {
            double num = GuiElement.scaled(unscaledDepth);
            ImageSurface imageSurface = new ImageSurface(Format.Argb32, (int)Bounds.OuterWidth, (int)Bounds.OuterHeight);
            Context context = genContext(imageSurface);
            context.SetSourceRGB(GuiStyle.DialogDefaultBgColor[0], GuiStyle.DialogDefaultBgColor[1], GuiStyle.DialogDefaultBgColor[2]);
            GuiElement.RoundRectangle(context, 0.0, 0.0, Bounds.OuterWidth, Bounds.OuterHeight, GuiStyle.ElementBGRadius);
            context.FillPreserve();
            context.SetSourceRGBA(0.0, 0.0, 0.0, 0.1);
            context.Fill();
            EmbossRoundRectangleElement(context, 0.0, 0.0, Bounds.OuterWidth, Bounds.OuterHeight, inverse: true, (int)num);

            // ComposeText(context);

            // double multilineTextHeight = GetMultilineTextHeight();
            // pressedYOffset = (Bounds.InnerHeight - multilineTextHeight) / 2.0 + num / 2.0 - 1.0;
            // DrawMultilineTextAt(context, Bounds.absPaddingX, Bounds.absPaddingY + pressedYOffset, EnumTextOrientation.Center);
            if (icon != null && icon.Length > 0)
            {
                context.SetSourceRGBA(GuiStyle.DialogDefaultTextColor);
                api.Gui.Icons.DrawIcon(context, icon, Bounds.absPaddingX + GuiElement.scaled(4.0), Bounds.absPaddingY + GuiElement.scaled(4.0), Bounds.InnerWidth - GuiElement.scaled(8.0), Bounds.InnerHeight - GuiElement.scaled(8.0), GuiStyle.DialogDefaultTextColor);
            }

            generateTexture(imageSurface, ref pressedTexture);
            context.Dispose();
            imageSurface.Dispose();
            imageSurface = new ImageSurface(Format.Argb32, (int)Bounds.OuterWidth, (int)Bounds.OuterHeight);
            context = genContext(imageSurface);
            context.SetSourceRGBA(0.0, 0.0, 0.0, 0.0);
            context.Fill();

            // ComposeText(context);

            // double[] color = Font.Color;
            // Font.Color = GuiStyle.ActiveButtonTextColor;
            // DrawMultilineTextAt(context, Bounds.absPaddingX, 0.0, EnumTextOrientation.Center);
            if (icon != null && icon.Length > 0)
            {
                context.SetSourceRGBA(GuiStyle.DialogDefaultTextColor);
                api.Gui.Icons.DrawIcon(context, icon, Bounds.absPaddingX + GuiElement.scaled(4.0), Bounds.absPaddingY + GuiElement.scaled(4.0), Bounds.InnerWidth - GuiElement.scaled(8.0), Bounds.InnerHeight - GuiElement.scaled(8.0), GuiStyle.DialogDefaultTextColor);
            }

            // Font.Color = color;
            generateTexture(imageSurface, ref hoverTexture);
            context.Dispose();
            imageSurface.Dispose();
        }
        /// <summary>
        /// Renders the button.
        /// </summary>
        /// <param name="deltaTime"></param>
        public override void RenderInteractiveElements(float deltaTime)
        {
            api.Render.Render2DTexturePremultipliedAlpha(On ? pressedTexture.TextureId : releasedTexture.TextureId, Bounds);
            if (icon == null && Bounds.PointInside(api.Input.MouseX, api.Input.MouseY))
            {
                api.Render.Render2DTexturePremultipliedAlpha(hoverTexture.TextureId, Bounds.renderX, Bounds.renderY + (On ? pressedYOffset : nonPressedYOffset), Bounds.OuterWidthInt, Bounds.OuterHeightInt);
            }
            api.Render.Render2DTexturePremultipliedAlpha(textTexture.TextureId, Bounds);
        }
        /// <summary>
        /// Handles the mouse button press while the mouse is on this button.
        /// </summary>
        /// <param name="api"></param>
        /// <param name="args"></param>
        public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
        {
            base.OnMouseDownOnElement(api, args);
            On = !On;
            handler?.Invoke(On, this.ButtonID);
            api.Gui.PlaySound("toggleswitch");
        }
        /// <summary>
        /// Handles the mouse button release while the mouse is on this button.
        /// </summary>
        /// <param name="api"></param>
        /// <param name="args"></param>
        public override void OnMouseUpOnElement(ICoreClientAPI api, MouseEvent args)
        {
            if (!Toggleable)
            {
                On = false;
            }
        }
        /// <summary>
        /// Handles the event fired when the mouse is released.
        /// </summary>
        /// <param name="api"></param>
        /// <param name="args"></param>
        public override void OnMouseUp(ICoreClientAPI api, MouseEvent args)
        {
            if (!Toggleable)
            {
                On = false;
            }

            base.OnMouseUp(api, args);
        }

        public override void OnKeyDown(ICoreClientAPI api, KeyEvent args)
        {
            if (base.HasFocus && args.KeyCode == 49)
            {
                args.Handled = true;
                On = !On;
                handler?.Invoke(On, this.ButtonID);
                api.Gui.PlaySound("toggleswitch");
            }
        }
        public override void OnFocusGained()
        {
            base.OnFocusGained();
        }
        public override void OnFocusLost()
        {
            base.OnFocusLost();
        }
        /// <summary>
        /// Is it On or off?
        /// </summary>
        /// <param name="on"></param>
        public void SetValue(bool on)
        {
            On = on;
        }
        /// <summary>
        /// Disposes the button
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
            releasedTexture.Dispose();
            pressedTexture.Dispose();
            hoverTexture.Dispose();
        }
    }
}