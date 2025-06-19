using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using Vintagestory.API.Util;
using CombatOverhaul.Armor;
using System.Collections;

namespace KRPGLib.Enchantment
{
    /// <summary>
    /// Primary controller for KRPG Enchantments. Should be managed through the API and almost never addressed directly.
    /// </summary>
    public class EnchantmentAccessor : IEnchantmentAccessor, IEnchantmentClientAccessor, IEnchantmentServerAccessor
    {
        public ICoreAPI Api;
        public ICoreServerAPI sApi;
        public ICoreClientAPI cApi;
        /// <summary>
        /// All Enchantments are processed and stored here. Must use RegisterEnchantmentClass to handle adding Enchantments.
        /// </summary>
        public Dictionary<string, Enchantment> EnchantmentRegistry = new Dictionary<string, Enchantment>();
        /// <summary>
        /// Used in CreateEnchantment(), as configured by RegisterEnchantmentClass().
        /// </summary>
        private Dictionary<string, Type> EnchantCodeToTypeMapping = new Dictionary<string, Type>();

        #region Registration
        /// <summary>
        /// Register an Enchantment to the EnchantmentRegistry. All Enchantments must be registered here. Returns false if it fails to register.
        /// </summary>
        /// <param name="enchantClass"></param>
        /// <param name="configLocation"></param>
        /// <param name="t"></param>
        public bool RegisterEnchantmentClass(string enchantClass, string configLocation, Type t)
        {
            if (EnchantingConfigLoader.Config.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] Attempting to RegisterEnchantmentClass.");
            if (enchantClass == null || configLocation == null || t.BaseType != typeof(Enchantment))
            {
                Api.Logger.Error("[KRPGEnchantment] Attempted to register an Enchantment with an invalid or missing registration information.");
                return false;
            }
            try
            {
                // Register the Enchantment Class
                this.EnchantCodeToTypeMapping[enchantClass] = t;
                // Create a new instance & assign registered class name
                var enchant = CreateEnchantment(enchantClass);
                // Setup the Config
                EnchantmentProperties props = Api.LoadModConfig<EnchantmentProperties>("KRPGEnchantment/Enchantments/" + configLocation);
                if (props == null)
                {
                    props = new EnchantmentProperties()
                    {
                        Enabled = enchant.Enabled,
                        Code = enchant.Code,
                        Category = enchant.Category,
                        LoreCode = enchant.LoreCode,
                        LoreChapterID = enchant.LoreChapterID,
                        MaxTier = enchant.MaxTier,
                        ValidToolTypes = enchant.ValidToolTypes,
                        Modifiers = enchant.Modifiers
                    };

                    Api.StoreModConfig(props, "KRPGEnchantment/Enchantments/" + configLocation);
                }
                enchant.Initialize(props);
                // Add to the Registry
                EnchantmentRegistry.Add(enchant.Code, enchant);

                if (EnchantingConfigLoader.Config.Debug == true)
                    Api.World.Logger.Event("[KRPGEnchantment] Enchantment {0} registered to the Enchantment Registry.", enchantClass);

                return true;
            }
            catch (Exception e)
            {
                Api.Logger.Error("[KRPGEnchantment] Error loading Enchantment Class: {0}", e);
                return false;
            }
        }
        private Type GetEnchantmentClass(string enchantClass)
        {
            Type val = null;
            this.EnchantCodeToTypeMapping.TryGetValue(enchantClass, out val);
            return val;
        }
        private Enchantment CreateEnchantment(string enchantClass)
        {
            Type enchantType;
            if (enchantClass == null || !this.EnchantCodeToTypeMapping.TryGetValue(enchantClass, out enchantType))
            {
                throw new Exception("[KRPGEnchantment] Don't know how to instantiate enchantment of class '" + enchantClass + "' did you forget to register a mapping?");
            }
            Enchantment result;
            try
            {
                result = (Enchantment)Activator.CreateInstance(enchantType, new object[1] { Api });
                result.ClassName = enchantClass;
            }
            catch (Exception exception)
            {
                DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(39, 2);
                defaultInterpolatedStringHandler.AppendLiteral("[KRPGEnchantment] Error on instantiating enchantment class '");
                defaultInterpolatedStringHandler.AppendFormatted(enchantClass);
                defaultInterpolatedStringHandler.AppendLiteral("':\n");
                defaultInterpolatedStringHandler.AppendFormatted<Exception>(exception);
                throw new Exception(defaultInterpolatedStringHandler.ToStringAndClear(), exception);
            }

            return result;
        }
        #endregion
        #region Enchanting
        /*
        [Obsolete]
        /// <summary>
        /// Returns a List of EnchantingRecipes that match the provided slots, or null if something went wrong.
        /// </summary>
        /// <param name="inSlot"></param>
        /// <param name="rSlot"></param>
        /// <returns></returns>
        public List<EnchantingRecipe> GetValidEnchantingRecipes(ItemSlot inSlot, ItemSlot rSlot)
        {
            return sApi.ModLoader.GetModSystem<EnchantingRecipeSystem>().GetValidEnchantingRecipes(inSlot, rSlot);
        }
        [Obsolete]
        /// <summary>
        /// Registers the provided EnchantingRecipe to the server.
        /// </summary>
        /// <param name="recipe"></param>
        public void RegisterEnchantingRecipe(EnchantingRecipe recipe)
        {
            sApi.ModLoader.GetModSystem<EnchantingRecipeSystem>().RegisterEnchantingRecipe(recipe);
        }
        */
        /// <summary>
        /// Returns a List of Enchantments that can be written to the ItemStack, or null if something went wrong.
        /// </summary>
        /// <param name="inSlot"></param>
        /// <returns></returns>
        public List<string> GetValidEnchantments(ItemSlot inSlot)
        {
            if (inSlot.Empty) return null;
            List<string> enchants = new List<string>();
            foreach (KeyValuePair<string, Enchantment> pair in EnchantmentRegistry)
            {
                IEnchantment ench = GetEnchantment(pair.Key);
                if (ench?.Enabled != true) continue;
                // Check the item's type vs the Enchantment's type
                string toolType = GetToolType(inSlot.Itemstack);
                if (!ench.ValidToolTypes.Contains(toolType, StringComparer.OrdinalIgnoreCase)) continue;
                // Write to the List if it passed
                enchants.Add(pair.Key);
            }

            return enchants;
        }
        /// <summary>
        /// Returns true if the provided Input and Reagent can accept the provided Enchantment code.
        /// </summary>
        /// <param name="inStack"></param>
        /// <param name="rStack"></param>
        /// <param name="enchant"></param>
        /// <returns></returns>
        public bool CanEnchant(ItemStack inStack, ItemStack rStack, string enchant)
        {
            if (inStack == null || rStack == null || enchant == null) return false;

            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] Attempting to check if {0} can be Enchanted with {1}.", inStack.GetName(), rStack.GetName());

            // Check against Max Enchantments Config option
            int maxEnchants = EnchantingConfigLoader.Config.MaxEnchantsPerItem;
            Dictionary<string, int> enchantments = sApi.EnchantAccessor().GetActiveEnchantments(inStack);
            if (enchantments != null && maxEnchants >= 0 && enchantments.Count >= maxEnchants)
            {
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] {0} has too may Enchantments: {1}.", inStack.GetName(), enchantments.Count);
                return false;
            }
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] {0} has {1} enchantments out of {2}.", inStack.GetName(), enchantments?.Count, maxEnchants);

            // Check Reagent Quantity
            int rQty = 0;
            foreach (KeyValuePair<string, int> pair in EnchantingConfigLoader.Config.ValidReagents)
            {
                if (pair.Key.ToLower() != rStack.Collectible.Code.ToString().ToLower()) continue;
                rQty = pair.Value;
            }
            if (rQty < 0) return false;

            // Get Reagent Potential
            int maxPot = GetReagentChargeOrPotential(rStack);
            if (maxPot <= 0) return false;

            // Get Input Type
            string toolType = GetToolType(inStack);
            if (toolType == null) return false;
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] Input type is {0}.", toolType);
            // Check against Enchantment
            IEnchantment ench = sApi.EnchantAccessor().GetEnchantment(enchant);
            if (ench == null || ench?.Enabled != true) return false;
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] Enchantment {0} is Enabled.", enchant);
            // Check the item's type vs the Enchantment's type
            if (!ench.ValidToolTypes.Contains(toolType, StringComparer.OrdinalIgnoreCase)) return false;
            
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] Enchant Check passed.", enchant);
            return true;
        }
        /// <summary>
        /// Returns the maximum tier enchantment a Reagent can power. Checks for Charge first, then Potential. Returns 0 if none is found.
        /// </summary>
        /// <param name="reagent"></param>
        /// <returns></returns>
        public int GetReagentChargeOrPotential(ItemStack reagent)
        {
            ITreeAttribute tree = reagent.Attributes.GetOrAddTreeAttribute("enchantments");
            int rPot = tree.GetInt("charge", 0);
            string rPotOG = reagent.Attributes.GetString("potential");
            if (rPot == 0 && rPotOG == null)
            {
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] Reagent {0} does not have any Charge or Potential value.", reagent.GetName());
                return 0;
            }
            // Fall back to default VS Potential values if enabled in config
            if (EnchantingConfigLoader.Config?.LegacyReagentPotential == true && rPotOG != null)
            {
                switch (rPotOG.ToLower())
                {
                    case "low": { return 2; }
                    case "medium": { return 3; }
                    case "high": { return 5; }
                    default:
                        {
                            Api.Logger.Error("[KRPGEnchantment] Reagent Potential {0} is not valid! Disable 'LegacyReagentPotential' in the config if you aren't sure why you're seeing this.", rPotOG);
                            return 0;
                        }
                }
            }
            // 1.0.x style Charge values
            if (rPot != 0)
            {
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] Reagent Max Charge is {0}", rPot);
                return rPot;
            }
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] {0} does not have a reagent charge or potential value.", reagent.GetName());
            return 0;
        }
        /// <summary>
        /// Returns the maximum tier enchantment a Reagent can power. Returns 0 if not found.
        /// </summary>
        /// <param name="reagent"></param>
        /// <returns></returns>
        public int GetReagentCharge(ItemStack reagent)
        {
            int maxPot = 0;
            ITreeAttribute tree = reagent.Attributes.GetOrAddTreeAttribute("enchantments");
            int rPot = tree.GetInt("charge");
            if (rPot == 0)
            {
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] Reagent {0} does not have any Charge value.", reagent.GetName());
                return 0;
            }
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] Reagent Max Charge is {0}", maxPot);

            return maxPot;
        }
        /// <summary>
        /// Returns an enchanted ItemStack. Provide int greater than 0 to override reagent potential.
        /// </summary>
        /// <param name="inSlot"></param>
        /// <param name="rSlot"></param>
        /// <param name="enchantments"></param>
        /// <returns></returns>
        public ItemStack EnchantItem(ICoreServerAPI api, ItemSlot inSlot, ItemSlot rSlot, Dictionary<string, int> enchantments)
        {
            if (inSlot.Empty || rSlot.Empty) return null;
            if (EnchantingConfigLoader.Config?.Debug == true)
                api.Logger.Event("[KRPGEnchantment] Attempting to Enchant an {0} with {1}.", inSlot.Itemstack.GetName(), rSlot.Itemstack.GetName());

            // Check Reagent Quantity - Obsolete
            // int rQty = GetReagentQuantity(rSlot.Itemstack);

            // Get Reagent Potential
            int maxPot = GetReagentChargeOrPotential(rSlot.Itemstack);
            if (EnchantingConfigLoader.Config?.Debug == true)
                api.Logger.Event("[KRPGEnchantment] Setting Max Potential to {0}.", maxPot);
            
            // Get Input Type
            var toolType = GetToolType(inSlot.Itemstack);
            if (toolType == null) return null;

            // Setup a new ItemStack
            ItemStack outStack = inSlot.Itemstack.Clone();
            // Setup Quantity
            outStack.StackSize = inSlot.StackSize;
            if (EnchantingConfigLoader.Config?.Debug == true)
                api.Logger.Event("[KRPGEnchantment] Setting OutStack {0} quantity to {1}", inSlot.Itemstack.GetName(), outStack.StackSize);

            // Try to write the Enchantments
            foreach (KeyValuePair<string, int> enchant in enchantments)
            {
                // Get the Enchantment first & check if it's Enabled before we do anything
                IEnchantment ench = this.GetEnchantment(enchant.Key);
                if (ench == null || ench?.Enabled != true) continue;
                // Check the item's type vs the Enchantment's type
                if (!ench.ValidToolTypes.Contains(toolType, StringComparer.OrdinalIgnoreCase)) continue;
                // Use provided Power or roll with reagent.
                int power = enchant.Value;
                if (power <= 0) power = api.World.Rand.Next(1, maxPot + 1);
                if (EnchantingConfigLoader.Config?.Debug == true)
                    api.Logger.Event("[KRPGEnchantment] Attempting to write {0}: {1} to item.", enchant.Key, enchant.Value);
                // Try to Enchant the item
                bool didEnchant = ench.TryEnchantItem(ref outStack, power, api);
                if (EnchantingConfigLoader.Config?.Debug == true)
                    api.Logger.Event("[KRPGEnchantment] Write completed with status: {0}.", didEnchant);
            }

            return outStack;
        }
        /// <summary>
        /// Returns the quantity of a provided Reagent, as set in the Config under ValidReagents. Set to 0 to disable Reagent consumption. Returns -1 if nothing is found.
        /// </summary>
        /// <param name="stack"></param>
        /// <returns></returns>
        public int GetReagentQuantity(ItemStack stack)
        {
            // Check Reagent Quantity
            int rQty = 0;
            foreach (KeyValuePair<string, int> pair in EnchantingConfigLoader.Config.ValidReagents)
            {
                if (pair.Key.ToLower() != stack.Collectible.Code.ToString().ToLower()) continue;
                rQty = pair.Value;
            }
            if (rQty < 0) return -1;
            return rQty;
        }
        /// <summary>
        /// Attempts to get base EnumTool type from an item, or interperited ID for a non-tool, then converts to string. This should match your ValidToolTypes in the Enchantment. Returns null if none can be found.
        /// </summary>
        /// <param name="stack"></param>
        /// <returns></returns>
        public string GetToolType(ItemStack stack)
        {
            // Block
            if (stack.Class == EnumItemClass.Block) return "block";
            // Tool or Weapon
            string s = null;
            s = stack.Collectible.Tool?.ToString().ToLower();
            if (s != null) return s;
            // Wearables
            s = stack.Attributes.GetString("clothescategory");
            if (s != null) return s.ToLower();
            // Class fallback - Some items just don't have tool times. idk, it's kinda inconsistent
            s = stack.Collectible?.Code;
            if (s != null)
            {
                if (s.Contains("cleaver")) return "cleaver";
            }
            return null;
        }
        #endregion
        #region Assessments
        /// <summary>
        /// Returns the Enchantment Interface from the EnchantmentRegistry.
        /// </summary>
        /// <param name="enchantCode"></param>
        /// <returns></returns>
        public IEnchantment GetEnchantment(string enchantCode)
        {
            return EnchantmentRegistry.GetValueOrDefault(enchantCode, null);
        }
        /// <summary>
        /// Returns the number of Enchantments in the EnchantmentRegistry that match the provided category.
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public int GetEnchantCategoryCount(string category)
        {
            int count = 0;
            foreach (KeyValuePair<string, Enchantment> enchant in EnchantmentRegistry)
            {
                if (GetEnchantment(enchant.Key)?.Category?.ToLower() == category)
                    count++;
            }
            return count;
        }
        /// <summary>
        /// Returns all EnchantmentRegistry keys with an Enchantment containing the provided category. Returns null if none are found or if run from client.
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public List<string> GetEnchantmentsInCategory(string category)
        {
            if (Api.Side != EnumAppSide.Server) return null;

            List<string> encahnts = new List<string>();
            foreach (KeyValuePair<string, Enchantment> pair in EnchantmentRegistry)
            {
                IEnchantment enchant = sApi.EnchantAccessor().GetEnchantment(pair.Key);
                if (enchant == null) continue;
                if (enchant.Category.ToLower() == category.ToLower())
                {
                    encahnts.Add(enchant.Code);
                }
            }
            if (encahnts.Count <= 0) return null;
            return encahnts;
        }
        /// <summary>
        /// Returns all Enchantments in the ItemStack's Attributes or null if none are found.
        /// </summary>
        /// <param name="itemStack"></param>
        /// <returns></returns>
        public Dictionary<string, int> GetActiveEnchantments(ItemStack itemStack)
        {
            // if (EnchantingConfigLoader.Config.Debug == true)
            //     Api.Logger.Event("[KRPGEnchantment] Attempting to GetActiveEnchantments on {0}", itemStack.GetName());
            // Get Attributes
            ITreeAttribute tree = itemStack?.Attributes?.GetTreeAttribute("enchantments");
            if (tree == null)
                return null;
            // Convert 0.6.x enchantments to 0.7.x "active" string. This will be removed in a later release.
            string oldActive = null;
            foreach (var val in Enum.GetValues(typeof(EnumEnchantments)))
            {
                int power = tree.GetInt(val.ToString());
                if (power > 0)
                {
                    oldActive += val.ToString() + ":" + power + ";";
                    tree.SetInt(val.ToString(), 0);
                }
            }
            if (oldActive != null) tree.SetString("active", oldActive);
            // Get Active Enchantments string
            string active = tree.GetString("active", null);
            if (active == null) return null;
            // Convert Active Enchantments string to Dictionary
            string[] activeStrings = active.Split(";",StringSplitOptions.RemoveEmptyEntries);
            Dictionary<string, int> enchants = new Dictionary<string, int>();
            foreach (string s in activeStrings)
            {
                string[] aa = s.Split(":");
                // if (EnchantingConfigLoader.Config.Debug == true)
                //     Api.Logger.Event("[KRPGEnchantment] Found Enchantment {0} with Power of {1} on {2}.", aa[0], aa[1], itemStack.GetName());
                enchants.Add(aa[0], Convert.ToInt32(aa[1]));
            }
            // Throw null if we failed to get anything
            if (enchants.Count <= 0) return null;
            return enchants;
        }
        /// <summary>
        /// Returns a List of Latent Enchantments pending for the contained Itemstack or null if there are none.
        /// </summary>
        /// <param name="inSlot"></param>
        /// <param name="encrypt"></param>
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
                        enchants.Add(str);
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
        /// Returns True if we successfully wrote new LatentEnchants to the item, or False if not.
        /// </summary>
        /// <param name="inSlot"></param>
        /// <returns></returns>
        public bool AssessItem(ItemSlot inSlot, ItemSlot rSlot)
        {
            // Sanity check
            if (sApi.Side != EnumAppSide.Server || inSlot.Empty || rSlot.Empty) return false;

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
            List<string> enchants = sApi.EnchantAccessor().GetValidEnchantments(inSlot);
            if (enchants == null) return false;
            if (enchants.Count <= 0) return false;
            // List<EnchantingRecipe> recipes = GetValidEnchantingRecipes(inSlot, rSlot);
            // if (recipes == null) return false;
            int eCount = enchants.Count;
            if (EnchantingConfigLoader.Config?.Debug == true)
                sApi.World.Logger.Event("[KRPGEnchantment] {0} valid enchantments found.", eCount);

            // Create a string with a random selection of Enchants
            string str = null;
            for (int i = 0; i < mle; i++)
            {
                int rNum = sApi.World.Rand.Next(eCount);
                if (EnchantingConfigLoader.Config?.Debug == true)
                    sApi.Logger.Event("rNum is {0} and eCount is {1}", rNum, eCount);
                string er = enchants[rNum];
                if (er != null)
                    str += er + ";";
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
        /// Call to assign a reagent charge attribute to an item. Returns the value assigned or 0 if it is not valid.
        /// </summary>
        /// <param name="stack"></param>
        /// <returns></returns>
        public int SetReagentCharge(ref ItemStack inStack, int numGears)
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.World.Logger.Event("[KRPGEnchantment] Attempting to set a charge to {0}.", inStack.GetName());
            // Check if we can actually access Attributes
            ITreeAttribute tree = inStack?.Attributes?.GetOrAddTreeAttribute("enchantments");
            if (tree == null)
                return 0;
            // Return an existing Potential or roll a new one
            int power = 0;
            // Set Charge attribute, based on config
            string s = inStack.Collectible.Code;
            if (EnchantingConfigLoader.Config.ValidReagents.ContainsKey(s) == true)
            {
                float mul = EnchantingConfigLoader.Config.ChargePerGear;
                int maxPower = EnchantingConfigLoader.Config.MaxReagentCharge;
                int p = (int)MathF.Floor(numGears * mul);
                power = Math.Min(p, maxPower);

                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.World.Logger.Event("[KRPGEnchantment] {0} is a ValidReagent and is being assigned a Charge of {1}.", inStack.GetName(), power);
            }
            // Write back to Attributes
            tree.SetInt("charge", power);
            inStack.Attributes?.MergeTree(tree);
            // Return for convenience
            return power;
        }
        #endregion
        #region Lore
        /// <summary>
        /// Returns true if the given player can decrypt the enchant. enchantName must be in the format of an AssetLocation.Name.ToShortString() (Ex: "domain:enchant-name")
        /// </summary>
        /// <param name="player"></param>
        /// <param name="enchantName"></param>
        /// <returns></returns>
        public bool CanReadEnchant(string player, string enchantName)
        {
            if (player != null && enchantName != null)
            {
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] Attempting to check if {0} can read {1}.", Api.World.PlayerByUid(player).PlayerName, enchantName);

                // string[] text = enchantName.Split(":");
                // string enchantCode = text[1].Replace("enchantment-", "");

                IEnchantment enchantment = GetEnchantment(enchantName);

                if (enchantment.Enabled != true)
                    return false;
                int id = enchantment.LoreChapterID;
                ModJournal journal = Api.ModLoader.GetModSystem<ModJournal>();
                if (journal == null)
                {
                    Api.Logger.Warning("[KRPGEnchantment] Could not find ModJournal!");
                    return false;
                }
                bool canRead = journal.DidDiscoverLore(player, "enchantment", id);
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] Can the {0} read {1}? {2}", Api.World.PlayerByUid(player).PlayerName, enchantName, canRead);
                return canRead;
            }

            Api.Logger.Error("[KRPGEnchantment] Could not determine player or enchantName for CanReadEnchant api call.");
            return false;
        }
        #endregion
        #region Actions
        /// <summary>
        /// Bulk convenience processor for Enchantments. Returns false if it fails to run an Enchantment trigger.
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="trigger"></param>
        /// <param name="byEntity"></param>
        /// <param name="targetEntity"></param>
        /// <returns></returns>
        public bool TryEnchantments(ItemSlot slot, string trigger, Entity byEntity, Entity targetEntity)
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] TryEnchantments has been called.");

            Dictionary<string, int> enchants = GetActiveEnchantments(slot.Itemstack);
            if (enchants != null)
            {
                foreach (KeyValuePair<string, int> pair in enchants)
                {
                    IEnchantment enc = GetEnchantment(pair.Key);
                    if (enc?.Enabled != true)
                    {
                        if (EnchantingConfigLoader.Config?.Debug == true)
                            Api.Logger.Event("[KRPGEnchantment] Tried Enchantment {0}, but it was either Disabled or not get-able.", pair.Key);
                        continue;
                    }

                    EnchantmentSource enchant = new EnchantmentSource()
                    {
                        SourceStack = slot.Itemstack,
                        Trigger = trigger,
                        Code = pair.Key,
                        Power = pair.Value,
                        SourceEntity = byEntity,
                        CauseEntity = byEntity,
                        TargetEntity = targetEntity,
                        DamageTier = pair.Value
                    };
                    enc.OnTrigger(enchant);
                }
                return true;
            }
            return false;
        }
        /// <summary>
        /// Bulk convenience processor for Enchantments. Returns false if it fails to run an Enchantment trigger.
        /// </summary>
        /// <param name="stack"></param>
        /// <param name="trigger"></param>
        /// <param name="byEntity"></param>
        /// <param name="targetEntity"></param>
        /// <returns></returns>
        public bool TryEnchantments(ItemStack stack, string trigger, Entity byEntity, Entity targetEntity)
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] TryEnchantments has been called.");

            Dictionary<string, int> enchants = GetActiveEnchantments(stack);
            if (enchants != null)
            {
                foreach (KeyValuePair<string, int> pair in enchants)
                {
                    IEnchantment enc = GetEnchantment(pair.Key);
                    if (enc?.Enabled != true)
                    {
                        if (EnchantingConfigLoader.Config?.Debug == true)
                            Api.Logger.Event("[KRPGEnchantment] Tried Enchantment {0}, but it was either Disabled or not get-able.", pair.Key);
                        continue;
                    }

                    EnchantmentSource enchant = new EnchantmentSource()
                    {
                        SourceStack = stack,
                        Trigger = trigger,
                        Code = pair.Key,
                        Power = pair.Value,
                        SourceEntity = byEntity,
                        CauseEntity = byEntity,
                        TargetEntity = targetEntity,
                        DamageTier = pair.Value
                    };

                    enc.OnTrigger(enchant);
                }
                return true;
            }
            return false;
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
        public bool TryEnchantments(ItemSlot slot, string trigger, Entity byEntity, Entity targetEntity, ref EnchantModifiers parameters)
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] TryEnchantments has been called.");

            Dictionary<string, int> enchants = GetActiveEnchantments(slot.Itemstack);
            if (enchants != null)
            {
                foreach (KeyValuePair<string, int> pair in enchants)
                {
                    IEnchantment enc = GetEnchantment(pair.Key);
                    if (enc?.Enabled != true)
                    {
                        if (EnchantingConfigLoader.Config?.Debug == true)
                            Api.Logger.Event("[KRPGEnchantment] Tried Enchantment {0}, but it was either Disabled or not get-able.", pair.Key);
                        continue;
                    }

                    EnchantmentSource enchant = new EnchantmentSource()
                    {
                        SourceStack = slot.Itemstack,
                        Trigger = trigger,
                        Code = pair.Key,
                        Power = pair.Value,
                        SourceEntity = byEntity,
                        CauseEntity = byEntity,
                        TargetEntity = targetEntity
                    };
                    if (parameters != null)
                        enc.OnTrigger(enchant, ref parameters);
                    else
                        enc.OnTrigger(enchant);
                }
                return true;
            }
            return false;
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
        public bool TryEnchantments(ItemStack stack, string trigger, Entity byEntity, Entity targetEntity, ref EnchantModifiers parameters)
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] TryEnchantments has been called.");

            Dictionary<string, int> enchants = GetActiveEnchantments(stack);
            if (enchants != null)
            {
                foreach (KeyValuePair<string, int> pair in enchants)
                {
                    IEnchantment enc = GetEnchantment(pair.Key);
                    if (enc?.Enabled != true)
                    {
                        if (EnchantingConfigLoader.Config?.Debug == true)
                            Api.Logger.Event("[KRPGEnchantment] Tried Enchantment {0}, but it was either Disabled or not get-able.", pair.Key);
                        continue;
                    }

                    EnchantmentSource enchant = new EnchantmentSource()
                    {
                        SourceStack = stack,
                        Trigger = trigger,
                        Code = pair.Key,
                        Power = pair.Value,
                        SourceEntity = byEntity,
                        CauseEntity = byEntity,
                        TargetEntity = targetEntity
                    };

                    if (parameters != null)
                        enc.OnTrigger(enchant, ref parameters);
                    else
                        enc.OnTrigger(enchant);
                }
                return true;
            }
            return false;
        }
        /// <summary>
        /// Generic convenience processor for Enchantments. Requires a pre-formed EnchantmentSource Returns false if it fails to run an Enchantment trigger.
        /// </summary>
        /// <param name="enchant"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public bool TryEnchantment(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            if (GetEnchantment(enchant.Code)?.Enabled != true)
                return false;

            GetEnchantment(enchant.Code).OnTrigger(enchant, ref parameters);

            return true;
        }
        #endregion
        #region GUI
        /// <summary>
        /// Returns a request font file from ModData/krpgenchantment/fonts, downloads it if possible, or null if it doesn't exist
        /// </summary>
        /// <param name="fName"></param>
        /// <returns></returns>
        public SKTypeface LoadCustomFont(string fName)
        {
            // Path to the font file in the ModData folder
            string fontPath = System.IO.Path.Combine(cApi.GetOrCreateDataPath(System.IO.Path.Combine("ModData", "krpgenchantment", "fonts")), fName);

            // Download the file to the client's ModData if it doesn't exist
            if (!File.Exists(fontPath))
            {
                cApi.World.Logger.Warning("[KRPGEnchantment] Font file not found at path: {0}.", fontPath);
                cApi.World.Logger.Event("[KRPGEnchantment] Copying font file to path: {0}.", fontPath);

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
                    cApi.World.Logger.Error("[KRPGEnchantment] Failed to download custom font: {0}", e.Message);
                    return null;
                }
            }
            // Check if the font file was created and bail if not
            if (!File.Exists(fontPath))
            {
                cApi.World.Logger.Error("[KRPGEnchantment] Font file not found at path: {0}.", fontPath);
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
                        cApi.World.Logger.Error("[KRPGEnchantment] Failed to create SKTypeface from the font file.");
                        return null;
                    }
                }
            }
            catch (Exception e)
            {
                cApi.World.Logger.Error("[KRPGEnchantment] Failed to load custom font: " + e.Message);
                return null;
            }
        }
        #endregion
    }
}