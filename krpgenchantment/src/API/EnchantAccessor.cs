using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.API;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;
using HarmonyLib;
using KRPGLib.Enchantment.API;
using Newtonsoft.Json;
using Vintagestory.API.Config;
using static System.Net.Mime.MediaTypeNames;
using Vintagestory.API.Common.Entities;

namespace KRPGLib.Enchantment
{
    /// <summary>
    /// Primary controller for KRPG Enchantments. Should be managed through the API and almost never addressed directly.
    /// </summary>
    public class EnchantAccessor : IEnchantAccessor
    {
        public ICoreAPI Api;
        public ICoreServerAPI sApi;
        public ICoreClientAPI cApi;
        public Dictionary<long, EnchantTick> TickRegistry { get; set; }
        #region Assessments
        /*
        /// <summary>
        /// Returns all Enchantments on the ItemStack's Attributes in the ItemSlot provided.
        /// </summary>
        /// <param name="itemStack"></param>
        /// <returns></returns>
        public Dictionary<string, int> GetEnchantments(ItemStack itemStack)
        {
            ITreeAttribute tree = itemStack?.Attributes?.GetTreeAttribute("enchantments");
            if (tree == null)
                return null;

            Dictionary<string, int> enchants = new Dictionary<string, int>();
            // Get Enchantments
            foreach (var val in Enum.GetValues(typeof(EnumEnchantments)))
            {
                int ePower = tree.GetInt(val.ToString(), 0);
                if (ePower > 0) { enchants.Add(val.ToString(), ePower); }
            }
            // Temporary converter for resistelectric bug
            // if (enchants.ContainsKey("resistelectric") && Api.Side == EnumAppSide.Server)
            // {
            //     tree.SetInt("resistelectricity", enchants.GetValueOrDefault("resistelectric", 0));
            //     tree.SetInt("resistelectric", 0);
            //     itemStack.Attributes.MergeTree(tree);
            // }
            return enchants;
        }
        /// <summary>
        /// Returns if the ItemStack is Enchantable or not.
        /// </summary>
        /// <param name="inSlot"></param>
        /// <returns></returns>
        public bool IsEnchantable(ItemSlot inSlot)
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
        public List<string> GetLatentEnchants(ItemSlot inSlot, bool encrypt)
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
                Api.Logger.Error("[KRPGEnchantment] Error when attempting to get Latent Enchants. Attribute tree not found.");
                return null;
            }
        }
        /// <summary>
        /// Returns True if we successfully wrote new LatentEnchants to the item, or False if not.
        /// </summary>
        /// <param name="inSlot"></param>
        /// <returns></returns>
        public bool AssessItem(ItemSlot inSlot, ItemSlot rSlot)
        {
            // Sanity check
            if (sApi.Side == EnumAppSide.Client || inSlot.Empty || rSlot.Empty) return false;

            if (EnchantingConfigLoader.Config?.Debug == true)
                sApi.World.Logger.Event("[KRPGEnchantment] Attempting to Assess {0}", inSlot.GetStackName());

            ITreeAttribute tree = inSlot.Itemstack.Attributes.GetOrAddTreeAttribute("enchantments");
            double latentStamp = tree.GetDouble("latentEnchantTime", 0);
            double timeStamp = sApi.World.Calendar.ElapsedDays;

            if (EnchantingConfigLoader.Config?.Debug == true)
                sApi.World.Logger.Event("[KRPGEnchantment] LatentStamp: {0}, TimeStamp: {1}", latentStamp, timeStamp);

            // Check the timestamp
            // 0 or less means re-assess every time
            // Config default is 7 days
            double ero = 7d;
            if (EnchantingConfigLoader.Config?.LatentEnchantResetDays != null)
                ero = EnchantingConfigLoader.Config.LatentEnchantResetDays;
            if (latentStamp != 0 && timeStamp < latentStamp + ero)
                return false;

            if (EnchantingConfigLoader.Config?.Debug == true)
                sApi.World.Logger.Event("[KRPGEnchantment] EnchantResetOverride set to {0}", ero);

            // Check for override
            int mle = 3;
            if (EnchantingConfigLoader.Config?.MaxLatentEnchants != mle)
                mle = EnchantingConfigLoader.Config.MaxLatentEnchants;

            if (EnchantingConfigLoader.Config?.Debug == true)
                sApi.World.Logger.Event("[KRPGEnchantment] Max Latent Enchants set to {0}", mle);

            // Get the Valid Recipes
            List<EnchantingRecipe> recipes = sApi.EnchantAccessor().GetValidEnchantingRecipes(Api, inSlot, rSlot);
            if (recipes == null) return false;

            if (EnchantingConfigLoader.Config?.Debug == true)
                sApi.World.Logger.Event("[KRPGEnchantment] {0} valid recipes found.", recipes.Count);

            // Create a string with a random selection of EnchantingRecipes
            string str = null;
            for (int i = 0; i < mle; i++)
            {
                int rNum = sApi.World.Rand.Next(recipes.Count);
                var er = recipes[rNum].Clone();
                if (er != null)
                    str += er.Name.ToShortString() + ";";
                else
                    sApi.World.Logger.Warning("[KRPGEnchantment] ValidRecipe element was null. Could not prep LatentEnchants string {0} to {1}.", i, inSlot.Itemstack.GetName());
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
                        int k = sApi.World.Rand.Next(65, 90 + 1);
                        strEnc += ((char)k).ToString();
                    }
                    strEnc += ";";
                }

                if (EnchantingConfigLoader.Config?.Debug == true)
                    sApi.World.Logger.Event("[KRPGEnchantment] LatentEnchants string is {0}", str);

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
        public int AssessReagent(ItemStack stack)
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.World.Logger.Event("[KRPGEnchantment] Attempting to Assess a {0}.", stack.GetName());
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
                    power = Api.World.Rand.Next(6);
                }
                // Write back to Attributes
                tree.SetInt("potential", power);
                stack.Attributes?.MergeTree(tree);
                // Return for convenience
                return power;
            }
            return p;
        }
        */
        #endregion
        #region Triggers
        /*
        /// <summary>
        /// Bulk convenience processor for Enchantments. Returns false if it fails to run an Enchantment trigger.
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="trigger"></param>
        /// <param name="byEntity"></param>
        /// <param name="targetEntity"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public bool TryEnchantments(ICoreAPI api, ItemSlot slot, string trigger, Entity byEntity, Entity targetEntity, ref Dictionary<string, object> parameters)
        {
            return api.ModLoader.GetModSystem<KRPGEnchantmentSystem>().TryEnchantments(slot, trigger, byEntity, targetEntity, ref parameters);
        }
        /// <summary>
        /// Bulk convenience processor for Enchantments. Returns false if it fails to run an Enchantment trigger.
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="trigger"></param>
        /// <param name="byEntity"></param>
        /// <param name="targetEntity"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public bool TryEnchantments(ICoreAPI api, ItemStack stack, string trigger, Entity byEntity, Entity targetEntity, ref Dictionary<string, object> parameters)
        {
            return api.ModLoader.GetModSystem<KRPGEnchantmentSystem>().TryEnchantments(stack, trigger, byEntity, targetEntity, ref parameters);
        }
        /// <summary>
        /// Generic convenience processor for Enchantments. Requires a pre-formed EnchantmentSource Returns false if it fails to run an Enchantment trigger.
        /// </summary>
        /// <param name="enchant"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public bool TryEnchantment(ICoreAPI api, EnchantmentSource enchant, ref Dictionary<string, object> parameters)
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
        public bool CanReadEnchant(ICoreAPI api, string player, EnchantingRecipe recipe)
        {
            return api.ModLoader.GetModSystem<KRPGEnchantmentSystem>().CanReadEnchant(player, recipe);
        }
        /// <summary>
        /// Returns true if the given player can decrypt the enchant. enchantName must be in the format of an AssetLocation.Name.ToShortString() (Ex: "domain:enchant-name")
        /// </summary>
        /// <param name="player"></param>
        /// <param name="enchantName"></param>
        /// <returns></returns>
        public bool CanReadEnchant(ICoreAPI api, string player, string enchantName)
        {
            return api.ModLoader.GetModSystem<KRPGEnchantmentSystem>().CanReadEnchant(player, enchantName);
        }
        */
        #endregion
        #region Registration and Loading        
        /// <summary>
        /// Returns a List of EnchantingRecipes that match the provided slots, or null if something went wrong.
        /// </summary>
        /// <param name="api"></param>
        /// <param name="inSlot"></param>
        /// <param name="rSlot"></param>
        /// <returns></returns>
        // public List<EnchantingRecipe> GetValidEnchantingRecipes(ICoreAPI api, ItemSlot inSlot, ItemSlot rSlot)
        // {
        //     return api.ModLoader.GetModSystem<EnchantingRecipeSystem>().GetValidEnchantingRecipes(inSlot, rSlot);
        // }
        /// <summary>
        /// List of all loaded Enchanting Recipes
        /// </summary>
        /// <returns></returns>
        public List<EnchantingRecipe> GetEnchantingRecipes()
        {
            return Api.ModLoader.GetModSystem<EnchantingRecipeSystem>().EnchantingRecipes;
        }
        /// <summary>
        /// All Enchantments are processed and stored here. Must use RegisterEnchantmentClass to handle adding Enchantments.
        /// </summary>
        public Dictionary<string, Enchantment> GetEnchantmentRegistry()
        { 
            return Api.ModLoader.GetModSystem<KRPGEnchantmentSystem>().EnchantmentRegistry;
        }
        /// <summary>
        /// Register an Enchantment to the EnchantmentRegistry. All Enchantments must be registered here. Returns false if it fails to register.
        /// </summary>
        /// <param name="api"></param>
        /// <param name="enchantClass"></param>
        /// <param name="configLocation"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public bool RegisterEnchantmentClass(ICoreServerAPI api, string enchantClass, string configLocation, Type t)
        {
            return api.ModLoader.GetModSystem<KRPGEnchantmentSystem>().RegisterEnchantmentClass(enchantClass, configLocation, t);
        }
        #endregion
    }
}