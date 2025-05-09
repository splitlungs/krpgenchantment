using Cairo;
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
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using static System.Net.Mime.MediaTypeNames;
using KRPGLib.Enchantment.API;
using System.Collections;

namespace Vintagestory.GameContent
{
    public static class ApiAdditions
    {
        /// <summary>
        /// Interface for KRPG Enchantment mod system.
        /// </summary>
        /// <param name="api"></param>
        /// <returns></returns>
        // public static IEnchantAccessor EnchantAccessor(this ICoreServerAPI api)
        // {
        //     // { return api.ModLoader.GetModSystem<KRPGEnchantmentSystem>().EnchantmentAccessor; }
        //     { return (IEnchantAccessor)KRPGEnchantmentSystem.EnchantmentAccessor; }
        // }
        #region Assessments
        /// <summary>
        /// Returns all Enchantments in the ItemStack's Attributes or null if none are found.
        /// </summary>
        /// <param name="itemStack"></param>
        /// <returns></returns>
        public static Dictionary<string, int> GetEnchantments(this ICoreAPI api, ItemStack itemStack)
        {
            return api.ModLoader.GetModSystem<KRPGEnchantmentSystem>().GetEnchantments(itemStack);
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
                enchantable = eb.Enchantable;
            if (enchantable != true)
                return false;

            return true;

        }
        /// <summary>
        /// Returns a List of Latent Enchantments pending for the contained Itemstack or null if there are none.
        /// </summary>
        /// <param name="inSlot"></param>
        /// <returns></returns>
        public static List<string> GetLatentEnchants(this ICoreAPI api, ItemSlot inSlot, bool encrypt)
        {
            if (inSlot.Empty == true) return null;

            List<string> enchants = new List<string>();
            ITreeAttribute tree = inSlot.Itemstack?.Attributes.GetOrAddTreeAttribute("enchantments");
            if (tree != null && !encrypt)
            {
                string lEnchant = tree.GetString("latentEnchants");
                string[] lEnchants = null;
                if (lEnchant != null) lEnchants = lEnchant.Split(";", StringSplitOptions.RemoveEmptyEntries);
                if (lEnchants != null)
                {
                    foreach (string str in lEnchants)
                    {
                        // Temporary measure to resolve enchantment-resistelectric bug
                        if (str == "krpgenchantment:enchantment-resistelectric")
                        {
                            string s = str + "ity";
                        }
                        enchants.Add(str);
                    }
                }
                if (enchants?.Count < EnchantingConfigLoader.Config?.MaxLatentEnchants)
                    enchants = null;
                return enchants;
            }
            else if (tree != null && encrypt == true)
            {
                string lEnchant = tree.GetString("latentEnchantsEncrypted");
                string[] lEnchants = null;
                if (lEnchant != null) lEnchants = lEnchant.Split(";", StringSplitOptions.RemoveEmptyEntries);
                if (lEnchants != null)
                {
                    foreach (string str in lEnchants)
                        enchants.Add(str);
                }
                if (enchants?.Count < EnchantingConfigLoader.Config?.MaxLatentEnchants)
                    enchants = null;
                return enchants;
            }
            else
            {
                api.Logger.Error("[KRPGEnchantment] Error when attempting to get Latent Enchants. Attribute tree not found.");
                return null;
            }
        }
        /// <summary>
        /// Returns True if we successfully wrote new LatentEnchants to the item, or False if not.
        /// </summary>
        /// <param name="inSlot"></param>
        /// <returns></returns>
        public static bool AssessItem(this ICoreServerAPI api, ItemSlot inSlot, ItemSlot rSlot)
        {
            // Sanity check
            if (api.Side == EnumAppSide.Client || inSlot.Empty || rSlot.Empty) return false;

            if (EnchantingConfigLoader.Config?.Debug == true)
                api.World.Logger.Event("[KRPGEnchantment] Attempting to Assess {0}", inSlot.GetStackName());

            ITreeAttribute tree = inSlot.Itemstack.Attributes.GetOrAddTreeAttribute("enchantments");
            double latentStamp = tree.GetDouble("latentEnchantTime", 0);
            double timeStamp = api.World.Calendar.ElapsedDays;

            if (EnchantingConfigLoader.Config?.Debug == true)
                api.World.Logger.Event("[KRPGEnchantment] LatentStamp: {0}, TimeStamp: {1}", latentStamp, timeStamp);

            // Check the timestamp
            // 0 or less means re-assess every time
            // Config default is 7 days
            double ero = 7d;
            if (EnchantingConfigLoader.Config?.LatentEnchantResetDays != null)
                ero = EnchantingConfigLoader.Config.LatentEnchantResetDays;
            if (latentStamp != 0 && timeStamp < latentStamp + ero)
                return false;

            if (EnchantingConfigLoader.Config?.Debug == true)
                api.World.Logger.Event("[KRPGEnchantment] EnchantResetOverride set to {0}", ero);

            // Check for override
            int mle = 3;
            if (EnchantingConfigLoader.Config?.MaxLatentEnchants != mle)
                mle = EnchantingConfigLoader.Config.MaxLatentEnchants;

            if (EnchantingConfigLoader.Config?.Debug == true)
                api.World.Logger.Event("[KRPGEnchantment] Max Latent Enchants set to {0}", mle);

            // Get the Valid Recipes
            List<EnchantingRecipe> recipes = api.GetValidEnchantingRecipes(inSlot, rSlot);
            if (recipes == null) return false;

            if (EnchantingConfigLoader.Config?.Debug == true)
                api.World.Logger.Event("[KRPGEnchantment] {0} valid recipes found.", recipes.Count);

            // Create a string with a random selection of EnchantingRecipes
            string str = null;
            for (int i = 0; i < mle; i++)
            {
                int rNum = api.World.Rand.Next(recipes.Count);
                var er = recipes[rNum].Clone();
                if (er != null)
                    str += er.Name.ToShortString() + ";";
                else
                    api.World.Logger.Warning("[KRPGEnchantment] ValidRecipe element was null. Could not prep LatentEnchants string {0} to {1}.", i, inSlot.Itemstack.GetName());
            }

            // Write the assessment to attributes
            if (str != null)
            {
                string strEnc = "";
                for (int i = 0; i < mle; i++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        // ASCII characters 65 - 90
                        int k = api.World.Rand.Next(65, 90 + 1);
                        strEnc += ((char)k).ToString();
                    }
                    strEnc += ";";
                }

                if (EnchantingConfigLoader.Config?.Debug == true)
                    api.World.Logger.Event("[KRPGEnchantment] LatentEnchants string is {0}", str);

                tree.SetString("latentEnchants", str);
                tree.SetString("latentEnchantsEncrypted", strEnc);
                tree.SetDouble("latentEnchantTime", timeStamp);
                inSlot.Itemstack.Attributes.MergeTree(tree);
            }
            else
                return false;

            return true;
        }
        /// <summary>
        /// Call to assign a EnchantPotential attribute to an item. Returns 0 if it is not valid.
        /// </summary>
        /// <param name="stack"></param>
        /// <returns></returns>
        public static int AssessReagent(this ICoreServerAPI api, ItemStack stack)
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                api.World.Logger.Event("[KRPGEnchantment] Attempting to Assess a {0}.", stack.GetName());
            // Check if we can actually access Attributes
            ITreeAttribute tree = stack?.Attributes?.GetOrAddTreeAttribute("enchantments");
            if (tree == null)
                return 0;
            // Return an existing Potential or roll a new one
            int p = tree.GetInt("potential");
            if (p != 0)
            {
                int power = 0;
                // Attempt to roll a random Potential based on Config
                if (EnchantingConfigLoader.Config.ValidReagents.ContainsKey(stack.Collectible.Code))
                {
                    power = api.World.Rand.Next(6);
                }
                // Write back to Attributes
                tree.SetInt("potential", power);
                stack.Attributes?.MergeTree(tree);
                // Return for convenience
                return power;
            }
            return p;
        }
        #endregion
        #region Triggers
        /// <summary>
        /// Bulk convenience processor for Enchantments. Returns false if it fails to run an Enchantment trigger.
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="trigger"></param>
        /// <param name="byEntity"></param>
        /// <param name="targetEntity"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static bool TryEnchantments(this ICoreAPI api, ItemSlot slot, string trigger, Entity byEntity, Entity targetEntity, ref Dictionary<string, object> parameters)
        {
            return api.ModLoader.GetModSystem<KRPGEnchantmentSystem>().TryEnchantments(slot, trigger, byEntity, targetEntity, ref parameters);
        }
        /// <summary>
        /// Bulk convenience processor for Enchantments. Returns false if it fails to run an Enchantment trigger.
        /// </summary>
        /// <param name="stack"></param>
        /// <param name="trigger"></param>
        /// <param name="byEntity"></param>
        /// <param name="targetEntity"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static bool TryEnchantments(this ICoreAPI api, ItemStack stack, string trigger, Entity byEntity, Entity targetEntity, ref Dictionary<string, object> parameters)
        {
            return api.ModLoader.GetModSystem<KRPGEnchantmentSystem>().TryEnchantments(stack, trigger, byEntity, targetEntity, ref parameters);
        }
        /// <summary>
        /// Generic convenience processor for Enchantments. Requires a pre-formed EnchantmentSource Returns false if it fails to run an Enchantment trigger.
        /// </summary>
        /// <param name="enchant"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static bool TryEnchantment(this ICoreAPI api, EnchantmentSource enchant, ref Dictionary<string, object> parameters)
        {
            return api.ModLoader.GetModSystem<KRPGEnchantmentSystem>().TryEnchantment(enchant, ref parameters);
        }
        #endregion
        #region Lore
        /// <summary>
        /// Returns true if the given player can decrypt the enchant.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="recipe"></param>
        /// <returns></returns>
        public static bool CanReadEnchant(this ICoreAPI api, string player, EnchantingRecipe recipe)
        {
            if (player != null && recipe != null)
            {
                string enchant = recipe.Name.ToShortString();
                if (EnchantingConfigLoader.Config?.Debug == true)
                    api.Logger.Event("[KRPGEnchantment] Attempting to check if {0} can read {1}.", api.World.PlayerByUid(player).PlayerName, enchant);
                
                string[] text = enchant.Split(":");
                string enchantCode = text[1].Replace("enchantment-", "");

                ICoreServerAPI sapi = (ICoreServerAPI)api;
                IEnchantment enchantment = sapi.GetEnchantment(enchantCode);

                if (enchantment.Enabled != true)
                    return false;
                int id = enchantment.LoreChapterID;
                ModJournal journal = api.ModLoader.GetModSystem<ModJournal>();
                if (journal == null)
                {
                    api.Logger.Error("[KRPGEnchantment] Could not find ModJournal!");
                    return false;
                }
                bool canRead = journal.DidDiscoverLore(player, "enchantment", id);
                if (EnchantingConfigLoader.Config?.Debug == true)
                    api.Logger.Event("[KRPGEnchantment] Can {0} read {1}? {2}", api.World.PlayerByUid(player).PlayerName, "lore-" + text[1], canRead);
                return canRead;
            }

            api.Logger.Error("[KRPGEnchantment] Could not determine player or enchantName for CanReadEnchant api call.");
            return false;
        }
        /// <summary>
        /// Returns true if the given player can decrypt the enchant. enchantName must be in the format of an AssetLocation.Name.ToShortString() (Ex: "domain:enchant-name")
        /// </summary>
        /// <param name="player"></param>
        /// <param name="enchantName"></param>
        /// <returns></returns>
        public static bool CanReadEnchant(this ICoreAPI api, string player, string enchantName)
        {
            if (player != null && enchantName != null)
            {
                if (EnchantingConfigLoader.Config?.Debug == true)
                    api.Logger.Event("[KRPGEnchantment] Attempting to check if {0} can read {1}.", api.World.PlayerByUid(player).PlayerName, enchantName);

                string[] text = enchantName.Split(":");
                string enchantCode = text[1].Replace("enchantment-", "");

                ICoreServerAPI sapi = (ICoreServerAPI)api;
                IEnchantment enchantment = sapi.GetEnchantment(enchantCode);

                if (enchantment.Enabled != true)
                    return false;
                int id = enchantment.LoreChapterID;
                ModJournal journal = api.ModLoader.GetModSystem<ModJournal>();
                if (journal == null)
                {
                    api.Logger.Warning("[KRPGEnchantment] Could not find ModJournal!");
                    return false;
                }
                bool canRead = journal.DidDiscoverLore(player, "enchantment", id);
                if (EnchantingConfigLoader.Config?.Debug == true)
                    api.Logger.Event("[KRPGEnchantment] Can the {0} read {1}? {2}", api.World.PlayerByUid(player).PlayerName, text[1], canRead);
                return canRead;
            }

            api.Logger.Error("[KRPGEnchantment] Could not determine player or enchantName for CanReadEnchant api call.");
            return false;
        }
        #endregion

        // public static bool RegisterEnchantmentClass(this ICoreServerAPI api, string enchantClass, string configLocation, Type t)
        // {
        //     { return api.ModLoader.GetModSystem<EnchantingRecipeSystem>().RegisterEnchantmentClass(enchantClass, configLocation, t); }
        // }
        /// <summary>
        /// Interface for KRPG Enchantment mod system.
        /// </summary>
        /// <param name="api"></param>
        /// <returns></returns>
        // public static IEnchantAccessor EnchantAccessor(this ICoreServerAPI api)
        // {
        //     { return api.ModLoader.GetModSystem<KRPGEnchantmentSystem>().EnchantAccessor; }
        // }
        /// <summary>
        /// Register an Enchantment to the EnchantmentRegistry. All Enchantments must be registered here. Returns false if it fails to register.
        /// </summary>
        /// <param name="enchantClass"></param>
        /// <param name="configLocation"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static bool RegisterEnchantmentClass(this ICoreServerAPI api, string enchantClass, string configLocation, Type t)
        {
            return api.ModLoader.GetModSystem<KRPGEnchantmentSystem>().RegisterEnchantmentClass(enchantClass, configLocation, t);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="enchantCode"></param>
        /// <returns></returns>
        public static IEnchantment GetEnchantment(this ICoreAPI api, string enchantCode)
        {
            return api.ModLoader.GetModSystem<KRPGEnchantmentSystem>().GetEnchantment(enchantCode);
        }
        /// <summary>
        /// Register an Enchanting Recipe
        /// </summary>
        /// <param name="recipe"></param>
        public static void RegisterEnchantingRecipe(this ICoreServerAPI api, EnchantingRecipe recipe)
        {
            api.ModLoader.GetModSystem<EnchantingRecipeSystem>().RegisterEnchantingRecipe(recipe);
        }
        /// <summary>
        /// List of all loaded Enchanting Recipes
        /// </summary>
        /// <returns></returns>
        public static List<EnchantingRecipe> GetEnchantingRecipes(this ICoreAPI api)
        {
            return api.ModLoader.GetModSystem<EnchantingRecipeSystem>().EnchantingRecipes;
        }
        public static List<EnchantingRecipe> GetValidEnchantingRecipes(this ICoreAPI api, ItemSlot inSlot, ItemSlot rSlot)
        {
            return api.ModLoader.GetModSystem<EnchantingRecipeSystem>().GetValidEnchantingRecipes(inSlot, rSlot);
        }
        /// <summary>
        /// Returns a request font file from ModData/krpgenchantment/fonts, downloads it if possible, or null if it doesn't exist
        /// </summary>
        /// <param name="fName"></param>
        /// <returns></returns>
        public static SKTypeface LoadCustomFont(this ICoreClientAPI api, string fName)
        {
            return api.ModLoader.GetModSystem<KRPGEnchantmentSystem>().LoadCustomFont(fName);
        }
    }
}