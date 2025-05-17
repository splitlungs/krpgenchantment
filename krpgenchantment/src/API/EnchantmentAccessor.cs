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
using Vintagestory.API.Util;

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
        #region Recipes
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
        /// <summary>
        /// Registers the provided EnchantingRecipe to the server.
        /// </summary>
        /// <param name="recipe"></param>
        public void RegisterEnchantingRecipe(EnchantingRecipe recipe)
        {
            sApi.ModLoader.GetModSystem<EnchantingRecipeSystem>().RegisterEnchantingRecipe(recipe);
        }
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

                string toolType = inSlot.Itemstack.Collectible.Tool.Value.ToString().ToLower();
                
                // if (inSlot.Itemstack.Class.GetType() == typeof(ItemWearable))
            }

            return enchants;
        }
        /// <summary>
        /// Returns an enchanted ItemStack. Provide int greater than 0 to override reagent potential.
        /// </summary>
        /// <param name="inSlot"></param>
        /// <param name="rSlot"></param>
        /// <param name="enchantments"></param>
        /// <returns></returns>
        public ItemStack EnchantItem(ItemSlot inSlot, ItemSlot rSlot, Dictionary<string, int> enchantments)
        {
            if (inSlot.Empty || rSlot.Empty) return null;
            if (EnchantingConfigLoader.Config?.Debug == true)
                sApi.Logger.Event("[KRPGEnchantment] Attempting to Enchant an {0} with {1}.", inSlot.Itemstack.GetName(), rSlot.Itemstack.GetName());
            
            // Check Reagent Quantity
            int rQty = 0;
            foreach (KeyValuePair<string, int> pair in EnchantingConfigLoader.Config.ValidReagents)
            {
                int qty = EnchantingConfigLoader.Config.ValidReagents.TryGetValue(rSlot.Itemstack.Collectible.Code.ToString().ToLower());
                if (qty > 0) rQty = qty;
            }
            if (rQty < 0) return null;
            // Get Reagent Potential
            ITreeAttribute tree = rSlot.Itemstack.Attributes.GetOrAddTreeAttribute("enchantments");
            string rPot = tree.GetString("potoential");
            if (rPot == null) return null;
            int maxPot = EnchantingConfigLoader.Config.ReagentPotentialTiers.TryGetValue(rPot);
            if (maxPot <= 0) return null;
            if (EnchantingConfigLoader.Config?.Debug == true)
                sApi.Logger.Event("[KRPGEnchantment] Setting Max Potential to {0}.", maxPot);
            
            // Get Input Type
            var toolType = inSlot.Itemstack.Collectible.Tool;
            if (toolType == null) return null;

            // Setup a new ItemStack
            ItemStack outStack = inSlot.Itemstack.Clone();
            // Setup Quantity
            outStack.StackSize = inSlot.StackSize;
            if (EnchantingConfigLoader.Config?.Debug == true)
                sApi.Logger.Event("[KRPGEnchantment] Setting OutStack {0} quantity to {1}", inSlot.Itemstack.GetName(), outStack.StackSize);

            // Try to write the Enchantments
            foreach (KeyValuePair<string, int> enchant in enchantments)
            {
                // Get the Enchantment first & check if it's Enabled before we do anything
                IEnchantment ench = sApi.EnchantAccessor().GetEnchantment(enchant.Key);
                if (ench == null || ench?.Enabled != true) continue;
                // Use provided Power or roll with reagent.
                int power = enchant.Value;
                if (power > 0) power = Api.World.Rand.Next(1, maxPot + 1);
                if (EnchantingConfigLoader.Config?.Debug == true)
                    sApi.Logger.Event("[KRPGEnchantment] Attempting to write {0}: {1} to item.", enchant.Key, enchant.Value);
                // Try to Enchant the item
                bool didEnchant = ench.TryEnchantItem(ref outStack, power);
                if (EnchantingConfigLoader.Config?.Debug == true)
                    sApi.Logger.Event("[KRPGEnchantment] Write completed with status: {0}.", didEnchant);
            }

            return outStack;
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
        /// Returns all EnchantmentRegistry keys with an Enchantment containing the provided category. Returns null if none are found.
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public List<string> GetEnchantmentsInCategory(string category)
        {
            List<string> s = new List<string>();
            foreach (KeyValuePair<string, Enchantment> pair in EnchantmentRegistry)
            {
                IEnchantment enchant = Api.EnchantAccessor().GetEnchantment(pair.Key);
                if (enchant == null) continue;
                if (enchant.Category.ToLower() == category)
                {
                    s.Add(enchant.Code);
                }
            }
            if (s.Count <= 0) return null;
            return s;
        }
        /// <summary>
        /// Returns all Enchantments in the ItemStack's Attributes or null if none are found.
        /// </summary>
        /// <param name="itemStack"></param>
        /// <returns></returns>
        public Dictionary<string, int> GetEnchantments(ItemStack itemStack)
        {
            // if (EnchantingConfigLoader.Config.Debug == true)
            //     Api.Logger.Event("[KRPGEnchantment] Attempting to GetEnchantments on {0}", itemStack.GetName());
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
                if (EnchantingConfigLoader.Config.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] Found Enchantment {0} with Power of {1} on {2}.", aa[0], aa[1], itemStack.GetName());
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
            if (Api.Side == EnumAppSide.Client || inSlot.Empty || rSlot.Empty) return false;

            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.World.Logger.Event("[KRPGEnchantment] Attempting to Assess {0}", inSlot.GetStackName());

            ITreeAttribute tree = inSlot.Itemstack.Attributes.GetOrAddTreeAttribute("enchantments");
            double latentStamp = tree.GetDouble("latentEnchantTime", 0);
            double timeStamp = Api.World.Calendar.ElapsedDays;

            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.World.Logger.Event("[KRPGEnchantment] LatentStamp: {0}, TimeStamp: {1}", latentStamp, timeStamp);

            // Check the timestamp
            // 0 or less means re-assess every time
            // Config default is 7 days
            double ero = 7d;
            if (EnchantingConfigLoader.Config?.LatentEnchantResetDays != null)
                ero = EnchantingConfigLoader.Config.LatentEnchantResetDays;
            if (latentStamp != 0 && timeStamp < latentStamp + ero)
                return false;

            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.World.Logger.Event("[KRPGEnchantment] EnchantResetOverride set to {0}", ero);

            // Check for override
            int mle = 3;
            if (EnchantingConfigLoader.Config?.MaxLatentEnchants != mle)
                mle = EnchantingConfigLoader.Config.MaxLatentEnchants;

            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.World.Logger.Event("[KRPGEnchantment] Max Latent Enchants set to {0}", mle);

            // Get the Valid Recipes
            List<EnchantingRecipe> recipes = GetValidEnchantingRecipes(inSlot, rSlot);
            if (recipes == null) return false;

            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.World.Logger.Event("[KRPGEnchantment] {0} valid recipes found.", recipes.Count);

            // Create a string with a random selection of EnchantingRecipes
            string str = null;
            for (int i = 0; i < mle; i++)
            {
                int rNum = Api.World.Rand.Next(recipes.Count);
                var er = recipes[rNum].Clone();
                if (er != null)
                    str += er.Name.ToShortString() + ";";
                else
                    Api.World.Logger.Warning("[KRPGEnchantment] ValidRecipe element was null. Could not prep LatentEnchants string {0} to {1}.", i, inSlot.Itemstack.GetName());
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
                        int k = Api.World.Rand.Next(65, 90 + 1);
                        strEnc += ((char)k).ToString();
                    }
                    strEnc += ";";
                }

                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.World.Logger.Event("[KRPGEnchantment] LatentEnchants string is {0}", str);

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
        #endregion
        #region Lore
        /// <summary>
        /// Returns true if the given player can decrypt the enchant.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="recipe"></param>
        /// <returns></returns>
        public bool CanReadEnchant(string player, EnchantingRecipe recipe)
        {
            if (player != null && recipe != null)
            {
                string enchant = recipe.Name.ToShortString();
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] Attempting to check if {0} can read {1}.", Api.World.PlayerByUid(player).PlayerName, enchant);

                string[] text = enchant.Split(":");
                string enchantCode = text[1].Replace("enchantment-", "");

                IEnchantment enchantment = GetEnchantment(enchantCode);

                if (enchantment.Enabled != true)
                    return false;
                int id = enchantment.LoreChapterID;
                ModJournal journal = Api.ModLoader.GetModSystem<ModJournal>();
                if (journal == null)
                {
                    Api.Logger.Error("[KRPGEnchantment] Could not find ModJournal!");
                    return false;
                }
                bool canRead = journal.DidDiscoverLore(player, "enchantment", id);
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] Can {0} read {1}? {2}", Api.World.PlayerByUid(player).PlayerName, "lore-" + text[1], canRead);
                return canRead;
            }

            Api.Logger.Error("[KRPGEnchantment] Could not determine player or enchantName for CanReadEnchant api call.");
            return false;
        }
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

                string[] text = enchantName.Split(":");
                string enchantCode = text[1].Replace("enchantment-", "");

                IEnchantment enchantment = GetEnchantment(enchantCode);

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
                    Api.Logger.Event("[KRPGEnchantment] Can the {0} read {1}? {2}", Api.World.PlayerByUid(player).PlayerName, text[1], canRead);
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
        /// <param name="parameters"></param>
        /// <returns></returns>
        public bool TryEnchantments(ItemSlot slot, string trigger, Entity byEntity, Entity targetEntity)
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] TryEnchantments has been called.");

            Dictionary<string, int> enchants = GetEnchantments(slot.Itemstack);
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
        public bool TryEnchantments(ItemStack stack, string trigger, Entity byEntity, Entity targetEntity)
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] TryEnchantments has been called.");

            Dictionary<string, int> enchants = GetEnchantments(stack);
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

            Dictionary<string, int> enchants = GetEnchantments(slot.Itemstack);
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

            Dictionary<string, int> enchants = GetEnchantments(stack);
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