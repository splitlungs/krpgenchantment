using Cairo;
using SkiaSharp;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace KRPGLib.Enchantment
{
    public class SkiaTextGuiElement : GuiElement
    {
        public LoadedTexture textTexture;
        public SKPaint textPaint;
        public string textToRender;

        private int xOffset = 8;
        private int yOffset = 32;

        public SkiaTextGuiElement(ICoreClientAPI capi, ElementBounds bounds, string text, SKTypeface typeface) : base(capi, bounds)
        {
            textTexture = new LoadedTexture(capi);
            textToRender = text;

            // Initialize Skia Paint
            textPaint = new SKPaint
            {
                Color = SKColors.RoyalBlue,
                TextSize = 28,
                IsAntialias = true,
                Typeface = typeface
            };
        }
        public override void ComposeElements(Context ctx, ImageSurface surface)
        {
            GenerateTextTexture();
        }
        /// <summary>
        /// Converts the text in Skia to a LoadedTexture for Rendering
        /// </summary>
        public void GenerateTextTexture()
        {
            // Create a SkiaSharp surface and canvas from the current Cairo context
            int width = (int)Bounds.InnerWidth;
            int height = (int)Bounds.InnerHeight;
            SKImageInfo info = new SKImageInfo(width, height);
            // Null check to prevent crash
            string text = "";
            if (textToRender != null) text = textToRender;

            using (var skiaSurface = SKSurface.Create(info))
            {
                SKCanvas canvas = skiaSurface.Canvas;
                canvas.Clear(SKColors.Transparent);

                // Draw text on the Skia canvas with offset positioning
                canvas.DrawText(text, xOffset, yOffset, textPaint);
                canvas.Flush();

                // Now we need to transfer Skia's drawing back into Cairo for display
                var skImage = skiaSurface.Snapshot();
                var pixmap = skImage.PeekPixels();
                IntPtr dataPtr = pixmap.GetPixels();
                int rowBytes = pixmap.RowBytes;

                // Render the Skia image onto the Cairo context
                ImageSurface imageSurface = new ImageSurface(dataPtr, Format.Argb32, info.Width, info.Height, pixmap.RowBytes);
                Context context = new Context(imageSurface);
                context.Antialias = Antialias.Best;
                context.SetSourceSurface(imageSurface, (int)Bounds.absFixedX, (int)Bounds.absFixedY);
                // Then save it as a texture
                generateTexture(imageSurface, ref textTexture);
                context.Dispose();
                imageSurface.Dispose();
            };
        }
        /// <summary>
        /// Renders the button.
        /// </summary>
        /// <param name="deltaTime"></param>
        public override void RenderInteractiveElements(float deltaTime)
        {
            api.Render.Render2DTexturePremultipliedAlpha(textTexture.TextureId, Bounds);
        }
        // Old Compose method.
        // Do we still need this?
        /*
        internal void ComposeText(Context ctx)
        {
            // Create a SkiaSharp surface and canvas from the current Cairo context
            int width = (int)Bounds.InnerWidth;
            int height = (int)Bounds.InnerHeight;
            SKImageInfo info = new SKImageInfo(width, height);
        
            using (var skiaSurface = SKSurface.Create(info))
            {
                SKCanvas canvas = skiaSurface.Canvas;
        
                // Set a transpaarent layer to write on
                canvas.Clear(SKColors.Transparent);
        
                // Draw text on the Skia canvas with offset positioning
                canvas.DrawText(textToRender, 0, textPaint.TextSize, textPaint);
                canvas.Flush();
        
                // Now we need to transfer Skia's drawing back into Cairo for display
                using (var skImage = skiaSurface.Snapshot())
                using (var pixmap = skImage.PeekPixels())
                {
                    IntPtr dataPtr = pixmap.GetPixels();
                    int rowBytes = pixmap.RowBytes;
        
                    // Render the Skia image onto the Cairo context used by Vintage Story
                    using (var imageSurface = new ImageSurface(dataPtr, Format.Argb32, info.Width, info.Height, pixmap.RowBytes))
                    {
                        ctx.Save();
                        ctx.SetSourceSurface(imageSurface, (int)Bounds.fixedX , (int)Bounds.fixedY);
                        ctx.Paint();
                        ctx.Restore();
                    }
                }
            }
        }
        */
        // Clean up any resources used by the custom element
        public override void Dispose()
        {
            base.Dispose();
            textPaint.Dispose();
            textTexture.Dispose();
        }
    }
}
