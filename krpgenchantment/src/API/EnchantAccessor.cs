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

namespace KRPGLib.Enchantment
{
    public class EnchantAccessor : IEnchantmentAPI
    {
        public ICoreAPI Api;
        public ICoreServerAPI sApi;
        public ICoreClientAPI cApi;
        /// <summary>
        /// Returns all Enchantments on the ItemStack's Attributes in the ItemSlot provided. Will migrate 0.4.x enchants until 0.6.x
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
            if (enchants.ContainsKey("resistelectric") && Api.Side == EnumAppSide.Server)
            {
                tree.SetInt("resistelectricity", enchants.GetValueOrDefault("resistelectric", 0));
                tree.SetInt("resistelectric", 0);
                itemStack.Attributes.MergeTree(tree);
            }
            return enchants;
        }
        /// <summary>
        /// Processes an Enchantment from the server. Returns false if it fails to run an Enchantment trigger.
        /// </summary>
        /// <param name="enchant"></param>
        /// <param name="slot"></param>
        /// <param name="damage"></param>
        /// <returns></returns>
        public bool DoEnchantment(EnchantmentSource enchant, ItemSlot slot, ref float damage)
        {
            if (!KRPGEnchantmentSystem.Enchantments.ContainsKey(enchant.Code))
                return false;

            if (KRPGEnchantmentSystem.Enchantments[enchant.Code]?.Enabled != true)
                return false;

            KRPGEnchantmentSystem.Enchantments[enchant.Code].OnTrigger(sApi, enchant, slot, ref damage);
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
                // api.Logger.Event("Attempting to check if {0} can read {1}.", api.World.PlayerByUid(player).PlayerName, enchant);
                string[] text = enchant.Split(":");
                if (!EnchantingConfigLoader.Config.LoreIDs.ContainsKey(text[1]))
                    return false;
                int id = EnchantingConfigLoader.Config.LoreIDs[text[1]];
                ModJournal journal = Api.ModLoader.GetModSystem<ModJournal>();
                if (journal == null)
                {
                    Api.Logger.Error("[KRPGEnchantment] Could not find ModJournal!");
                    return false;
                }
                bool canRead = journal.DidDiscoverLore(player, "enchantment", id);
                // api.Logger.Event("Can {0} read {1}? {2}", api.World.PlayerByUid(player).PlayerName, "lore-" + text[1], canRead);
                return canRead;
            }
            // api.Logger.Event("Player or recipe was null when attempting to read the runes!");
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
                // api.Logger.Event("Attempting to check if {0} can read {1}.", api.World.PlayerByUid(player).PlayerName, enchantName);
                string[] text = enchantName.Split(":");
                if (!EnchantingConfigLoader.Config.LoreIDs.ContainsKey(text[1]))
                    return false;
                int id = EnchantingConfigLoader.Config.LoreIDs[text[1]];
                ModJournal journal = Api.ModLoader.GetModSystem<ModJournal>();
                if (journal == null)
                {
                    Api.Logger.Warning("[KRPGEnchantment] Could not find LegacyModJournal!");
                    return false;
                }
                bool canRead = journal.DidDiscoverLore(player, "enchantment", id);
                // api.Logger.Event("Can the {0} read {1}? {2}", api.World.PlayerByUid(player).PlayerName, text[1], canRead);
                return canRead;
            }
            // api.Logger.Warning("Could not determine byPlayer or enchantName for CanReadEnchant api call.");
            return false;
        }
        /// <summary>
        /// This exists as a temporary replacement for a removed helper function in ModJournal.
        /// </summary>
        /// <param name="playerUid"></param>
        /// <param name="code"></param>
        /// <param name="chapterId"></param>
        /// <returns></returns>
        public bool DidDiscoverLore(string playerUid, string code, int chapterId)
        {
            Dictionary<string, Journal> journalsByPlayerUid = new Dictionary<string, Journal>();

            if (!journalsByPlayerUid.TryGetValue(playerUid, out var value))
            {
                return false;
            }

            for (int i = 0; i < value.Entries.Count; i++)
            {
                if (!(value.Entries[i].LoreCode == code))
                {
                    continue;
                }

                JournalEntry journalEntry = value.Entries[i];
                for (int j = 0; j < journalEntry.Chapters.Count; j++)
                {
                    if (journalEntry.Chapters[j].ChapterId == chapterId)
                    {
                        return true;
                    }
                }

                break;
            }

            return false;
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
                sApi.World.Logger.VerboseDebug("[KRPGEnchantment] Attempting to Assess {0}", inSlot.GetStackName());

            ITreeAttribute tree = inSlot.Itemstack.Attributes.GetOrAddTreeAttribute("enchantments");
            double latentStamp = tree.GetDouble("latentEnchantTime", 0);
            double timeStamp = sApi.World.Calendar.ElapsedDays;

            if (EnchantingConfigLoader.Config?.Debug == true)
                sApi.World.Logger.VerboseDebug("[KRPGEnchantment] LatentStamp: {0}, TimeStamp: {1}", latentStamp, timeStamp);

            // Check the timestamp
            // 0 or less means re-assess every time
            // Config default is 7 days
            double ero = 7d;
            if (EnchantingConfigLoader.Config?.LatentEnchantResetDays != null)
                ero = EnchantingConfigLoader.Config.LatentEnchantResetDays;
            if (latentStamp != 0 && timeStamp < latentStamp + ero)
                return false;

            if (EnchantingConfigLoader.Config?.Debug == true)
                sApi.World.Logger.VerboseDebug("[KRPGEnchantment] EnchantResetOverride set to {0}", ero);

            // Check for override
            int mle = 3;
            if (EnchantingConfigLoader.Config?.MaxLatentEnchants != mle)
                mle = EnchantingConfigLoader.Config.MaxLatentEnchants;

            if (EnchantingConfigLoader.Config?.Debug == true)
                sApi.World.Logger.VerboseDebug("[KRPGEnchantment] Max Latent Enchants set to {0}", mle);

            // Get the Valid Recipes
            IEnchantmentAPI eApi = sApi.EnchantAccessor();
            List<EnchantingRecipe> recipes = eApi.GetValidEnchantingRecipes(inSlot, rSlot);
            if (recipes == null) return false;

            if (EnchantingConfigLoader.Config?.Debug == true)
                sApi.World.Logger.VerboseDebug("[KRPGEnchantment] {0} valid recipes found.", recipes.Count);

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
                    sApi.World.Logger.VerboseDebug("[KRPGEnchantment] LatentEnchants string is {0}", str);

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
                    power = Api.World.Rand.Next(EnchantingConfigLoader.Config.MaxEnchantTier) + 1;
                }
                // Write back to Attributes
                tree.SetInt("potential", power);
                stack.Attributes?.MergeTree(tree);
                // Return for convenience
                return power;
            }
            return p;
        }
        /// <summary>
        /// Returns a List of EnchantingRecipes that match the provided slots, or null if something went wrong.
        /// </summary>
        /// <param name="inSlot"></param>
        /// <param name="rSlot"></param>
        /// <returns></returns>
        public List<EnchantingRecipe> GetValidEnchantingRecipes(ItemSlot inSlot, ItemSlot rSlot)
        {
            if (inSlot.Empty || rSlot.Empty) return null;

            List<EnchantingRecipe> recipes = new List<EnchantingRecipe>();
            var enchantingRecipes = Api.EnchantAccessor().GetEnchantingRecipes();
            if (enchantingRecipes != null)
            {
                foreach (EnchantingRecipe rec in enchantingRecipes)
                    if (rec.Matches(Api, inSlot, rSlot))
                        recipes.Add(rec.Clone());
                if (recipes.Count > 0)
                    return recipes;
                else
                    return null;
            }
            else
                Api.Logger.Error("[KRPGEnchantment] EnchantingRecipe Registry could not be found! Please report error to author.");
            return null;
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
                enchantable = eb.EnchantProps.Enchantable;
            if (enchantable != true)
                return false;

            return true;

        }

        /// <summary>
        /// List of all loaded Enchanting Recipes
        /// </summary>
        /// <returns></returns>
        public List<EnchantingRecipe> GetEnchantingRecipes()
        {
            return Api.ModLoader.GetModSystem<EnchantingRecipeSystem>().EnchantingRecipes;
        }

    }
}