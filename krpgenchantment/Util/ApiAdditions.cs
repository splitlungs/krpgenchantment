﻿using Cairo;
using Cairo.Freetype;
using KRPGLib.Enchantment;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
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
        /// Returns a List of Latent Enchantments pending for the contained Itemstack or null if there are none.
        /// </summary>
        /// <param name="api"></param>
        /// <param name="inSlot"></param>
        /// <returns></returns>
        public static List<string> GetLatentEnchants(this ICoreAPI api, ItemSlot inSlot)
        {
            List<string> enchants = new List<string>();

            if (!inSlot.Empty)
            {
                ITreeAttribute tree = inSlot.Itemstack?.Attributes.GetOrAddTreeAttribute("enchantments");
                if (tree != null)
                {
                    string lEnchant = tree.GetString("latentEnchants");
                    string[] lEnchants = null;
                    if (lEnchant != null)
                        lEnchants = lEnchant.Split(";", StringSplitOptions.RemoveEmptyEntries);
                    if (lEnchants != null)
                    {
                        foreach (string str in lEnchants)
                            enchants.Add(str);
                    }
                    return enchants;
                }
                else
                {
                    api.Logger.Error("Error when attempting to get Latent Enchants. Attribute tree not found.");
                    return null;
                }
            }

            return null;
        }
        /// <summary>
        /// Returns True if we successfully wrote new LatentEnchants to the item, or False if not.
        /// </summary>
        /// <param name="api"></param>
        /// <param name="inSlot"></param>
        /// <returns></returns>
        public static bool AssessItem(this ICoreAPI api, ItemSlot inSlot, ItemSlot rSlot)
        {
            // Sanity check
            if (inSlot.Empty || rSlot.Empty) return false;

            ITreeAttribute tree = inSlot.Itemstack.Attributes.GetOrAddTreeAttribute("enchantments");
            double latentStamp = tree.GetDouble("latentEnchantTime", 0);
            double timeStamp = api.World.Calendar.ElapsedDays;

            // Check if it's a valid Timestamp
            double ero = 7d;
            if (EnchantingRecipeLoader.Config?.EnchantResetOverride != ero)
                ero = EnchantingRecipeLoader.Config.EnchantResetOverride;
            if (latentStamp != 0 && timeStamp < latentStamp + ero)
                return false;

            // Check for override
            int mle = 3;
            if (EnchantingRecipeLoader.Config?.MaxLatentEnchants != mle)
                mle = EnchantingRecipeLoader.Config.MaxLatentEnchants;

            // Get the Valid Recipes
            List<EnchantingRecipe> recipes = api.GetValidRecipes(inSlot, rSlot);
            if (recipes == null) return false;

            // Create a string with a random selection of EnchantingRecipes
            string str = null;
            for (int i = 0; i < mle; i++)
            {
                int rNum = api.World.Rand.Next(recipes.Count);
                var er = recipes[rNum].Clone();
                if (er != null)
                    str += er.Name.ToShortString() + ";";
                else
                    api.World.Logger.Event("ValidRecipe element was null. Could not assign Latent Enchantment.");
            }

            // Write the assessment to attributes
            if (str != null)
            {
                tree.SetString("latentEnchants", str);
                tree.SetDouble("latentEnchantTime", timeStamp);
                inSlot.Itemstack.Attributes.MergeTree(tree);
                inSlot.MarkDirty();
            }
            else
                return false;

            return true;
        }
        /// <summary>
        /// Returns a List of EnchantingRecipes that match the provided slots, or null if something went wrong.
        /// </summary>
        /// <param name="api"></param>
        /// <param name="inSlot"></param>
        /// <param name="rSlot"></param>
        /// <returns></returns>
        public static List<EnchantingRecipe> GetValidRecipes(this ICoreAPI api, ItemSlot inSlot, ItemSlot rSlot)
        {
            if (inSlot.Empty || rSlot.Empty) return null;

            List<EnchantingRecipe> recipes = new List<EnchantingRecipe>();
            var enchantingRecipes = api.GetEnchantingRecipes();
            if (enchantingRecipes != null)
            {
                foreach (EnchantingRecipe rec in enchantingRecipes)
                    if (rec.Matches(api, inSlot, rSlot))
                        recipes.Add(rec.Clone());
                if (recipes.Count > 0)
                    return recipes;
                else
                    return null;
            }
            else
                api.Logger.Error("EnchantingRecipe Registry could not be found! Please report error to author.");
            return null;
        }
        /// <summary>
        /// Returns if the ItemStack is Enchantable or not.
        /// </summary>
        /// <param name="api"></param>
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
        /// Returns a request font file from ModData/krpgenchantment/fonts, downloads it if possible, or null if it doesn't exist
        /// </summary>
        /// <param name="api"></param>
        /// <param name="fName"></param>
        /// <returns></returns>
        public static SKTypeface LoadCustomFont(this ICoreClientAPI api, string fName)
        {
            // Path to the font file in the ModData folder
            string fontPath = System.IO.Path.Combine(api.GetOrCreateDataPath(System.IO.Path.Combine("ModData", "krpgenchantment", "fonts")), fName);

            // Download the file to the client's ModData if it doesn't exist
            if (!File.Exists(fontPath))
            {
                api.World.Logger.Error("Font file not found at path: {0}.", fontPath);
                api.World.Logger.Event("Copying font file to path: {0}.", fontPath);

                try
                {
                    using (var client = new HttpClient())
                    {
                        using (var s = client.GetStreamAsync("http://kronos-gaming.net/downloads/files/" + fName))
                        {
                            using (var fs = new FileStream(fontPath, FileMode.OpenOrCreate))
                            {
                                s.Result.CopyTo(fs);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    api.World.Logger.Error("Failed to download custom font: {0}", e.Message);
                    return null;
                }
            }
            // Check if the font file was created and bail if not
            if (!File.Exists(fontPath))
            {
                api.World.Logger.Error("Font file not found at path: {0}.", fontPath);
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
                        // api.World.Logger.Notification("Custom font successfully loaded from: " + fontPath);
                        return customTypeface;
                    }
                    else
                    {
                        api.World.Logger.Error("Failed to create SKTypeface from the font file.");
                        return null;
                    }
                }
            }
            catch (Exception e)
            {
                api.World.Logger.Error("Failed to load custom font: " + e.Message);
                return null;
            }
        }
    }
}