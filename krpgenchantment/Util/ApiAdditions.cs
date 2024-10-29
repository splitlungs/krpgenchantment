using Cairo;
using Cairo.Freetype;
using KRPGLib.Enchantment;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using static System.Net.Mime.MediaTypeNames;

namespace Vintagestory.GameContent
{
    public static class ApiAdditions
    {
        /// <summary>
        /// Returns all Enchantments on the ItemStack's Attributes in the ItemSlot provided. Will migrate 0.4.x enchants until 0.6.x
        /// </summary>
        /// <param name="inSlot"></param>
        /// <returns></returns>
        public static Dictionary<string, int> GetEnchantments(this ICoreAPI api, ItemSlot inSlot)
        {
            ITreeAttribute tree = inSlot.Itemstack.Attributes.GetOrAddTreeAttribute("enchantments");
            Dictionary<string, int> enchants = new Dictionary<string, int>();

            // Get Enchantments for Migration
            // Will be removed in 0.6.x
            foreach (var val in Enum.GetValues(typeof(EnumEnchantments)))
            {
                int ePower = inSlot.Itemstack.Attributes.GetInt(val.ToString(), 0);
                if (ePower > 0) { enchants.Add(val.ToString(), ePower); }
            }

            // Migrate old Enchantments if needed
            // Will be removed in 0.6.x
            if (enchants.Count > 0)
            {
                foreach (KeyValuePair<string, int> keyValuePair in enchants)
                {
                    tree.SetInt(keyValuePair.Key, keyValuePair.Value);
                    inSlot.Itemstack.Attributes.RemoveAttribute(keyValuePair.Key);
                }
            }
            else
            {
                // Get Enchantments
                foreach (var val in Enum.GetValues(typeof(EnumEnchantments)))
                {
                    int ePower = tree.GetInt(val.ToString(), 0);
                    if (ePower > 0) { enchants.Add(val.ToString(), ePower); }
                }
            }

            return enchants;
        }

        /// <summary>
        /// Returns if the ItemStack is Enchantable or not.
        /// </summary>
        /// <param name="inSlot"></param>
        /// <returns></returns>
        public static bool IsEnchantable(this ICoreAPI api, ItemSlot inSlot)
        {
            bool enchantable;

            ITreeAttribute enchantTree = inSlot.Itemstack.Attributes.GetTreeAttribute("enchantments");
            enchantable = enchantTree.GetBool("enchantable", false);
            if (enchantable == true)
                return true;

            EnchantmentBehavior eb = inSlot.Itemstack.Collectible.GetBehavior<EnchantmentBehavior>();
            if (eb != null)
                enchantable = eb.EnchantProps.Enchantable;
            if (enchantable != true)
                return false;

            return true;

        }

        /// <summary>
        /// List of all loaded Enchanting Recipes
        /// </summary>
        /// <param name="api"></param>
        /// <returns></returns>
        public static List<EnchantingRecipe> GetEnchantingRecipes(this ICoreAPI api)
        {
            return api.ModLoader.GetModSystem<EnchantingRecipeSystem>().EnchantingRecipes;
        }

        /// <summary>
        /// Register an Enchanting Recipe
        /// </summary>
        /// <param name="api"></param>
        /// <param name="recipe"></param>
        public static void RegisterEnchantingRecipe(this ICoreServerAPI api, EnchantingRecipe recipe)
        {
            api.ModLoader.GetModSystem<EnchantingRecipeSystem>().RegisterEnchantingRecipe(recipe);
        }

        /// <summary>
        /// Returns a font from ModData/krpgenchantment/fonts or null if it doesn't exist
        /// </summary>
        /// <param name="api"></param>
        /// <param name="fName"></param>
        /// <returns></returns>
        public static SKTypeface LoadCustomFont(this ICoreClientAPI api, string fName)
        {
            // Path to the font file in the ModData folder
            string fontPath = System.IO.Path.Combine(api.GetOrCreateDataPath(System.IO.Path.Combine("ModData", "krpgenchantment", "fonts")), fName);

            // Check if the font file exists
            if (!File.Exists(fontPath))
            {
                api.World.Logger.Error("Font file not found at path: " + fontPath);
                return null;
            }

            try
            {
                // Load the custom font using SkiaSharp
                using (var fontStream = File.OpenRead(fontPath))
                {
                    SKTypeface customTypeface = SKTypeface.FromStream(fontStream);
                    if (customTypeface != null)
                    {
                        api.World.Logger.Notification("Custom font successfully loaded from: " + fontPath);
                        return customTypeface;
                    }
                    else
                    {
                        api.World.Logger.Error("Failed to create SKTypeface from the font file.");
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                api.World.Logger.Error("Failed to load custom font: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Converts Skia Text to a Cairo ImageSurface.
        /// </summary>
        /// <param name="api"></param>
        /// <param name="text"></param>
        /// <param name="font"></param>
        /// <param name="Bounds"></param>
        /// <returns></returns>
        public static ImageSurface SkiaTextToImageSurface(this ICoreClientAPI api, string text, SKTypeface font, ElementBounds Bounds) 
        {
            // Initialize Skia Paint
            SKPaint textPaint = new SKPaint
            {
                Color = SKColors.RoyalBlue,
                TextSize = 32,
                IsAntialias = true,
                Typeface = font
            };

            // Create a SkiaSharp surface and canvas from the current Cairo context
            int width = (int)Bounds.InnerWidth;
            int height = (int)Bounds.InnerHeight;
            SKImageInfo info = new SKImageInfo(width, height);

            using (var skiaSurface = SKSurface.Create(info))
            {
                SKCanvas canvas = skiaSurface.Canvas;

                // Clear canvas
                canvas.Clear(SKColors.Transparent);

                // Use the Bounds.fixedX and Bounds.fixedY to adjust the position of each GuiElementSkiaText
                float xOffset = (float)Bounds.fixedX;
                float yOffset = (float)Bounds.fixedY;

                // Draw text
                canvas.DrawText(text, xOffset, yOffset, textPaint);

                // Flush canvas to ensure rendering is complete
                canvas.Flush();

                // Now we need to transfer Skia's drawing back into Cairo for display
                using (var skImage = skiaSurface.Snapshot())
                using (var pixmap = skImage.PeekPixels())
                {
                    IntPtr dataPtr = pixmap.GetPixels();
                    ImageSurface surface = new ImageSurface(dataPtr, Format.Argb32, info.Width, info.Height, pixmap.RowBytes);
                    canvas.Dispose();
                    return surface;
                }
            }
        }

        /// <summary>
        /// Converts Skia text into a Cairo ImageSurface
        /// </summary>
        /// <param name="text"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static ImageSurface RenderTextToCairoImage(this ICoreClientAPI api, SKTypeface customTypeface, string text, int width, int height)
        {
            // Create a SkiaSharp bitmap to render the text
            var info = new SKImageInfo(width, height);
            var bitmap = new SKBitmap(info);

            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(SKColors.Transparent);

                // Create a paint object with the custom typeface
                var paint = new SKPaint
                {
                    Typeface = customTypeface,
                    TextSize = 32,
                    IsAntialias = true,
                    Color = SKColors.Blue,
                    TextAlign = SKTextAlign.Center
                };

                // Render the text
                canvas.DrawText(text, width / 2, height / 2 + paint.TextSize / 2, paint);
                canvas.Flush();
            }

            // Convert the SKBitmap to a Cairo ImageSurface
            ImageSurface cairoImage = new ImageSurface(Format.Argb32, width, height);
            using (var cairoCtx = new Context(cairoImage))
            {
                // Lock the Skia bitmap for access to its pixel data
                using (var pixmap = bitmap.PeekPixels())
                {
                    // Copy pixel data from Skia bitmap to Cairo surface
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            var color = pixmap.GetPixelColor(x, y);
                            cairoCtx.SetSourceRGBA(color.Red / 255.0, color.Green / 255.0, color.Blue / 255.0, color.Alpha / 255.0);
                            cairoCtx.Rectangle(x, y, 1, 1);
                            cairoCtx.Fill();
                        }
                    }
                }
            }

            return cairoImage;
        }
    }
}