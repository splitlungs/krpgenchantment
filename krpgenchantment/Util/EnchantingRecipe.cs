using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.GameContent.Mechanics;

namespace KRPGLib.Enchantment
{
    #region Recipe
    public class EnchantingRecipe : IByteSerializable
    {
        /// <summary>
        /// Name of the recipe, optional
        /// </summary>
        public AssetLocation Name;
        /// <summary>
        /// Set by the recipe loader during json deserialization, if false the recipe will never be loaded.
        /// If loaded however, you can use this field to disable recipes during runtime.
        /// </summary>
        public bool Enabled { get; set; } = true;
        /// <summary>
        /// How many in-game hours to complete recipe.
        /// </summary>
        public double processingHours;    
        /// <summary>
        /// Info used by the handbook. Allows you to split grid recipe previews into multiple.
        /// </summary>
        public int RecipeGroup;
        /// <summary>
        /// Used by the handbook. If false, will not appear in the "Created by" section
        /// </summary>
        public bool ShowInCreatedBy = true;
        /// <summary>
        /// If set only players with given trait can use this recipe
        /// </summary>
        public string RequiresTrait;
        /// <summary>
        /// The recipes ingredients in any order
        /// </summary>
        public Dictionary<string, CraftingRecipeIngredient> Ingredients;
        /// <summary>
        /// Optional attribute data that you can attach any data to
        /// </summary>
        [JsonConverter(typeof(JsonAttributesConverter))]
        public JsonObject Attributes;
        /// <summary>
        /// If set, it will copy over the itemstack attributes from given ingredient code
        /// </summary>
        public string CopyAttributesFrom = null;
        /// <summary>
        /// Enchantments to write to the Output
        /// </summary>
        public Dictionary<string, int> Enchantments;

        public List<EnchantingRecipeIngredient> resolvedIngredients;

        IWorldAccessor world;

        public int IngredientQuantity(ItemSlot inSlot)
        {
            int qty = 1;
            // Get qty from Config
            string itemcode = inSlot.Itemstack.Collectible.Code.ToShortString();
            // Then check against the Config file
            if (EnchantingConfigLoader.Config.ValidReagents.ContainsKey(itemcode))
                qty = EnchantingConfigLoader.Config.ValidReagents[itemcode];
            // Override from Recipe. Note this overrides Config.
            foreach (KeyValuePair<string, CraftingRecipeIngredient> ing in Ingredients)
            {
                bool foundi = false;
                if (ing.Value.IsWildCard)
                {
                    inSlot.Itemstack.Collectible.WildCardMatch(ing.Value.Code);

                    bool foundw = false;
                    foundw =
                            ing.Value.Type == inSlot.Itemstack.Class &&
                            WildcardUtil.Match(ing.Value.Code, inSlot.Itemstack.Collectible.Code, ing.Value.AllowedVariants)
                        ;
                    if (foundw == true) foundi = true;
                }
                else if (ing.Value.ResolvedItemstack.Satisfies(inSlot.Itemstack)) foundi = true;
                
                if (foundi == true) qty = ing.Value.Quantity;
            }
            return qty;
        }
        /// <summary>
        /// Returns an Enchanted ItemStack
        /// </summary>
        /// <param name="api"></param>
        /// <param name="inSlot"></param>
        /// <param name="rSlot"></param>
        /// <returns></returns>
        public ItemStack OutStack(ICoreAPI api, ItemSlot inSlot, ItemSlot rSlot)
        {
            if (inSlot.Empty || rSlot.Empty) return null;

            // Setup a new ItemStack
            ItemStack outStack = inSlot.Itemstack.Clone();
            // Setup Quantity
            outStack.StackSize = IngredientQuantity(inSlot);

            if (EnchantingConfigLoader.Config?.Debug == true)
                api.Logger.VerboseDebug("[KRPGEnchantment] Setting OutStack {0} quantity to {1}", inSlot.Itemstack.GetName(), outStack.StackSize);

            // Setup Reagent Override
            bool rOverride = false;
            foreach (KeyValuePair<string, CraftingRecipeIngredient> keyValuePair in Ingredients)
            {
                if (keyValuePair.Key.ToLower() == "reagent") rOverride = true;
            }
            // Setup Enchant Power
            int power = 0;
            if (!rOverride)
            {
                // ITreeAttribute rTree = rSlot.Itemstack.Attributes?.GetOrAddTreeAttribute("enchantments");
                // int maxPot = rTree.GetInt("potential");
                string pot = rSlot.Itemstack.Attributes?.GetString("potential", "low").ToLower();
                int maxPot = EnchantingConfigLoader.Config.ReagentPotentialTiers[pot];
                power = api.World.Rand.Next(1, maxPot + 1);
                if (EnchantingConfigLoader.Config?.Debug == true)
                    api.Logger.Event("[KRPGEnchantment] Setting Power to {0} out of {1}, with Potential {2}.", power, maxPot, maxPot);
            }
            else if (EnchantingConfigLoader.Config?.Debug == true)
            {
                api.Logger.Warning("[KRPGEnchantment] Could not get Config override for reagent potential.");
            }
            // Dictionary<string, int> curEnchants = api.GetEnchantments(inSlot);
            ITreeAttribute tree = outStack.Attributes?.GetOrAddTreeAttribute("enchantments");
            // Apply Enchantments
            foreach (KeyValuePair<string, int> enchant in Enchantments)
            {
                // Overwrite Healing
                if (enchant.Key == EnumEnchantments.healing.ToString())
                {
                    tree.SetInt(EnumEnchantments.flaming.ToString(), 0);
                    tree.SetInt(EnumEnchantments.frost.ToString(), 0);
                    tree.SetInt(EnumEnchantments.harming.ToString(), 0);
                    tree.SetInt(EnumEnchantments.shocking.ToString(), 0);
                }
                // Overwrite Alternate Damage
                else if (enchant.Key == EnumEnchantments.flaming.ToString() || enchant.Key == EnumEnchantments.frost.ToString() 
                    || enchant.Key == EnumEnchantments.harming.ToString() || enchant.Key == EnumEnchantments.shocking.ToString()
                    )
                    tree.SetInt(EnumEnchantments.healing.ToString(), 0);
                // Re-roll if the recipe limits max power
                if (rOverride)
                    power = enchant.Value;
                // Write Enchant
                tree.SetInt(enchant.Key, power);
            }
            // Limit damage enchants.
            int maxDE = EnchantingConfigLoader.Config.MaxDamageEnchants;
            if (maxDE >= 0)
            {
                int numDmgEnchants = 0;
                if (tree.GetInt(EnumEnchantments.flaming.ToString(), 0) > 0) numDmgEnchants++;
                if (tree.GetInt(EnumEnchantments.frost.ToString(), 0) > 0) numDmgEnchants++;
                if (tree.GetInt(EnumEnchantments.harming.ToString(), 0) > 0) numDmgEnchants++;
                if (tree.GetInt(EnumEnchantments.shocking.ToString(), 0) > 0) numDmgEnchants++;
                if (numDmgEnchants > maxDE)
                {
                    int roll = api.World.Rand.Next(1, 5);
                    if (roll == 1) tree.SetInt(EnumEnchantments.flaming.ToString(), 0);
                    else if (roll == 2) tree.SetInt(EnumEnchantments.frost.ToString(), 0);
                    else if (roll == 3) tree.SetInt(EnumEnchantments.harming.ToString(), 0);
                    else if (roll == 4) tree.SetInt(EnumEnchantments.shocking.ToString(), 0);
                }
            }

            tree.RemoveAttribute("latentEnchantTime");
            tree.RemoveAttribute("latentEnchants");
            outStack.Attributes.MergeTree(tree);
            return outStack;
        }

        /// <summary>
        /// Turns Ingredients into IItemStacks
        /// </summary>
        /// <param name="world"></param>
        /// <returns>True on successful resolve</returns>
        public bool ResolveIngredients(IWorldAccessor world)
        {
            this.world = world;
            int eIndex = 0;
            resolvedIngredients = new List<EnchantingRecipeIngredient>();
            foreach (KeyValuePair<string, CraftingRecipeIngredient> pair in Ingredients)
            {
                if (!Ingredients.ContainsKey(pair.Key))
                {
                    world.Logger.Error("[KRPGEnchantment] Enchanting Recipe {0} contains an ingredient pattern code {1} but supplies no ingredient for it.", Name, pair.Key);
                    return false;
                }
                if (!Ingredients[pair.Key].Resolve(world, "Enchanting recipe"))
                {
                    world.Logger.Error("[KRPGEnchantment] Enchanting Recipe: {0} contains an ingredient that cannot be resolved: {1}", pair.Key, Ingredients[pair.Key]);
                    return false;
                }

                resolvedIngredients.Add(Ingredients[pair.Key].CloneTo<EnchantingRecipeIngredient>());
                resolvedIngredients[eIndex].PatternCode = pair.Key;
                eIndex++;
            }

            return true;
        }

        /// <summary>
        /// Resolves Wildcards in the ingredients
        /// </summary>
        /// <param name="world"></param>
        /// <returns></returns>
        public Dictionary<string, string[]> GetNameToCodeMapping(IWorldAccessor world)
        {
            Dictionary<string, string[]> mappings = new Dictionary<string, string[]>();

            foreach (var val in Ingredients)
            {
                if (val.Value.Name == null || val.Value.Name.Length == 0) continue;
                if (!val.Value.Code.Path.Contains('*')) continue;
                int wildcardStartLen = val.Value.Code.Path.IndexOf('*');
                int wildcardEndLen = val.Value.Code.Path.Length - wildcardStartLen - 1;

                List<string> codes = new List<string>();

                if (val.Value.Type == EnumItemClass.Block)
                {
                    for (int i = 0; i < world.Blocks.Count; i++)
                    {
                        var block = world.Blocks[i];
                        if (block?.Code == null || block.IsMissing) continue;
                        if (val.Value.SkipVariants != null && WildcardUtil.MatchesVariants(val.Value.Code, block.Code, val.Value.SkipVariants)) continue;

                        if (WildcardUtil.Match(val.Value.Code, block.Code, val.Value.AllowedVariants))
                        {
                            string code = block.Code.Path.Substring(wildcardStartLen);
                            string codepart = code.Substring(0, code.Length - wildcardEndLen);
                            codes.Add(codepart);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < world.Items.Count; i++)
                    {
                        var item = world.Items[i];
                        if (item?.Code == null || item.IsMissing) continue;
                        if (val.Value.SkipVariants != null && WildcardUtil.MatchesVariants(val.Value.Code, item.Code, val.Value.SkipVariants)) continue;

                        if (WildcardUtil.Match(val.Value.Code, item.Code, val.Value.AllowedVariants))
                        {
                            string code = item.Code.Path.Substring(wildcardStartLen);
                            string codepart = code.Substring(0, code.Length - wildcardEndLen);
                            codes.Add(codepart);
                        }
                    }
                }

                mappings[val.Value.Name] = codes.ToArray();
            }
            return mappings;
        }
        /// <summary>
        /// Puts the crafted itemstack into the output slot and 
        /// consumes the required items from the input slots
        /// </summary>
        /// <param name="byPlayer"></param>
        /// <param name="inputSlots"></param>
        /// <returns></returns>
        public bool ConsumeInput(IPlayer byPlayer, ItemSlot[] inputSlots)
        {
            List<EnchantingRecipeIngredient> exactMatchIngredients = new List<EnchantingRecipeIngredient>();
            List<EnchantingRecipeIngredient> wildcardIngredients = new List<EnchantingRecipeIngredient>();

            for (int i = 0; i < resolvedIngredients.Count; i++)
            {
                EnchantingRecipeIngredient ingredient = resolvedIngredients[i];
                if (ingredient == null) continue;

                if (ingredient.IsWildCard || ingredient.IsTool)
                {
                    wildcardIngredients.Add(ingredient.CloneTo<EnchantingRecipeIngredient>());
                    continue;
                }

                ItemStack stack = ingredient.ResolvedItemstack;

                bool found = false;
                for (int j = 0; j < exactMatchIngredients.Count; j++)
                {
                    if (exactMatchIngredients[j].ResolvedItemstack.Satisfies(stack))
                    {
                        exactMatchIngredients[j].ResolvedItemstack.StackSize += stack.StackSize;
                        found = true;
                        break;
                    }
                }
                if (!found) exactMatchIngredients.Add(ingredient.CloneTo<EnchantingRecipeIngredient>());
            }

            for (int i = 0; i < inputSlots.Length; i++)
            {
                ItemStack inStack = inputSlots[i].Itemstack;
                if (inStack == null) continue;

                for (int j = 0; j < exactMatchIngredients.Count; j++)
                {
                    if (exactMatchIngredients[j].ResolvedItemstack.Satisfies(inStack))
                    {
                        int quantity = Math.Min(exactMatchIngredients[j].ResolvedItemstack.StackSize, inStack.StackSize);

                        // inStack.Collectible.OnConsumedByCrafting(inputSlots, inputSlots[i], this, exactMatchIngredients[j], byPlayer, quantity);

                        exactMatchIngredients[j].ResolvedItemstack.StackSize -= quantity;

                        if (exactMatchIngredients[j].ResolvedItemstack.StackSize <= 0)
                        {
                            exactMatchIngredients.RemoveAt(j);
                        }

                        break;
                    }
                }

                for (int j = 0; j < wildcardIngredients.Count; j++)
                {
                    EnchantingRecipeIngredient ingredient = wildcardIngredients[j];

                    if (
                        ingredient.Type == inStack.Class &&
                        WildcardUtil.Match(ingredient.Code, inStack.Collectible.Code, ingredient.AllowedVariants)
                    )
                    {
                        int quantity = Math.Min(ingredient.Quantity, inStack.StackSize);

                        // inStack.Collectible.OnConsumedByCrafting(inputSlots, inputSlots[i], this, ingredient, byPlayer, quantity);

                        if (ingredient.IsTool)
                        {
                            wildcardIngredients.RemoveAt(j);
                        }
                        else
                        {
                            ingredient.Quantity -= quantity;

                            if (ingredient.Quantity <= 0)
                            {
                                wildcardIngredients.RemoveAt(j);
                            }
                        }

                        break;
                    }
                }
            }

            return exactMatchIngredients.Count == 0;
        }
        /// <summary>
        /// Check if this recipe matches given ingredients
        /// </summary>
        /// <param name="inputSlot"></param>
        /// <param name="reagentSlot"></param>
        /// <returns></returns>
        public bool Matches(ICoreAPI api, ItemSlot inputSlot, ItemSlot reagentSlot)
        {
            // Null Check
            if (inputSlot.Empty || reagentSlot.Empty) return false;
            
            // Check Targets
            bool foundt = false;
            // bool flag2 = false;
            // foreach (EnchantingRecipeIngredient ing in resolvedIngredients)
            foreach (KeyValuePair<string, CraftingRecipeIngredient> ing in Ingredients)
            {
                // api.Logger.Event("Echanting Recipe: {0}. Checking Resolved Ingredient {1}. Quantity of {2}", Name, ing.Value.Name, ing.Value.Quantity);
                if (ing.Value.IsWildCard)
                {
                    // api.Logger.Event("Ingredient is a Wildcard!");

                    inputSlot.Itemstack.Collectible.WildCardMatch(ing.Value.Code);

                    bool foundw = false;
                    foundw =
                            ing.Value.Type == inputSlot.Itemstack.Class &&
                            WildcardUtil.Match(ing.Value.Code, inputSlot.Itemstack.Collectible.Code, ing.Value.AllowedVariants)
                        ;
                    if (foundw == true) foundt = true;
                }
                else if (ing.Value.ResolvedItemstack.Satisfies(inputSlot.Itemstack)) foundt = true;

                if (inputSlot.Itemstack.StackSize < ing.Value.Quantity) foundt = false;
            }

            if (EnchantingConfigLoader.Config?.Debug == true)
                api.Logger.Event("Enchanting Recipe: {0}. Found target in Matches? {1}", Name, foundt);

            // Cancel if no Target
            if (!foundt) return false;

            // Check Reagents
            bool foundr = false;
            // Override from recipe first
            foreach (EnchantingRecipeIngredient ing in resolvedIngredients)
            {
                if (ing.PatternCode == "reagent")
                {
                    if (ing.IsWildCard)
                    {
                        bool foundw = false;
                        foundw =
                                ing.Type == reagentSlot.Itemstack.Class &&
                                WildcardUtil.Match(ing.Code, reagentSlot.Itemstack.Collectible.Code, ing.AllowedVariants)
                            ;
                        if (foundw) foundr = true;
                    }
                    else if (ing.ResolvedItemstack.Satisfies(reagentSlot.Itemstack)) foundr = true;

                    if (reagentSlot.Itemstack.StackSize < ing.Quantity) foundr = false;
                }
            }
            // Then check against the Config file
            if (!foundr && EnchantingConfigLoader.Config.ValidReagents != null)
            {
                foreach (KeyValuePair<string, int> reagent in EnchantingConfigLoader.Config.ValidReagents)
                {
                    // Last chance to prove True, so we check qty simultaneously
                    if (reagentSlot.Itemstack.Collectible.Code.ToString() == reagent.Key && reagentSlot.Itemstack.StackSize >= reagent.Value)
                        foundr = true;
                }
            }

            if (EnchantingConfigLoader.Config?.Debug == true)
                api.Logger.Event("Enchanting Recipe: {0}. Found Reagent in Matches? {1}", Name, foundr);

            // Cancel if no Rreagent is found
            if (!foundr) return false;

            return true;
        }

        /// <summary>
        /// Returns only the first matching itemstack, there may be multiple
        /// </summary>
        /// <param name="patternCode"></param>
        /// <param name="inputSlots"></param>
        /// <returns></returns>
        private ItemStack GetInputStackForPatternCode(string patternCode, ItemSlot[] inputSlots)
        {
            var ingredient = resolvedIngredients.FirstOrDefault(ig => ig?.PatternCode == patternCode);
            if (ingredient == null) return null;

            foreach (var slot in inputSlots)
            {
                if (slot.Empty) continue;
                var inputStack = slot.Itemstack;
                if (inputStack == null) continue;
                if (ingredient.SatisfiesAsIngredient(inputStack)) return inputStack;
            }

            return null;
        }
        // We probably don't need this
        // public bool TryCraftNow(ICoreAPI api, double nowProcessedHours, ItemSlot[] inputslots)
        // {
        //     if (nowProcessedHours < processingHours) return false;
        // 
        //     inputslots[1].Itemstack = OutStack(inputslots[0].Itemstack).Clone();
        //     return true;
        // }
        /// <summary>
        /// Serialized the recipe
        /// </summary>
        /// <param name="writer"></param>
        public void ToBytes(BinaryWriter writer)
        {
            writer.Write(processingHours);

            writer.Write(resolvedIngredients.Count);
            for (int i = 0; i < resolvedIngredients.Count; i++)
            {
                if (resolvedIngredients[i] == null)
                {
                    writer.Write(value: true);
                    continue;
                }

                writer.Write(value: false);
                resolvedIngredients[i].ToBytes(writer);
            }

            writer.Write(Name.ToShortString());
            writer.Write(Attributes == null);
            if (Attributes != null)
            {
                writer.Write(Attributes.Token.ToString());
            }

            writer.Write(RequiresTrait != null);
            if (RequiresTrait != null)
            {
                writer.Write(RequiresTrait);
            }

            writer.Write(RecipeGroup);
            writer.Write(CopyAttributesFrom != null);
            if (CopyAttributesFrom != null)
            {
                writer.Write(CopyAttributesFrom);
            }

            writer.Write(ShowInCreatedBy);
            writer.Write(Ingredients.Count);
            foreach (KeyValuePair<string, CraftingRecipeIngredient> ingredient in Ingredients)
            {
                writer.Write(ingredient.Key);
                ingredient.Value.ToBytes(writer);
            }
            writer.Write(Enchantments.Count);
            foreach (KeyValuePair<string, int> enchant in Enchantments)
            {
                writer.Write(enchant.Key);
                writer.Write((int)enchant.Value);
            }
        }

        /// <summary>
        /// Deserializes the recipe
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="resolver"></param>
        public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
        {
            processingHours = reader.ReadDouble();

            int num = reader.ReadInt32();
            resolvedIngredients = new List<EnchantingRecipeIngredient>();
            for (int i = 0; i < num; i++)
            {
                if (!reader.ReadBoolean())
                {
                    EnchantingRecipeIngredient ing = new EnchantingRecipeIngredient();
                    ing.FromBytes(reader, resolver);
                    resolvedIngredients.Add(ing.CloneTo<EnchantingRecipeIngredient>());
                }
            }

            Name = new AssetLocation(reader.ReadString());
            if (!reader.ReadBoolean())
            {
                string json = reader.ReadString();
                Attributes = new JsonObject(JToken.Parse(json));
            }

            if (reader.ReadBoolean())
            {
                RequiresTrait = reader.ReadString();
            }

            RecipeGroup = reader.ReadInt32();
            if (reader.ReadBoolean())
            {
                CopyAttributesFrom = reader.ReadString();
            }

            ShowInCreatedBy = reader.ReadBoolean();
            int num2 = reader.ReadInt32();
            Ingredients = new Dictionary<string, CraftingRecipeIngredient>();
            for (int j = 0; j < num2; j++)
            {
                string key = reader.ReadString();
                CraftingRecipeIngredient craftingRecipeIngredient = new CraftingRecipeIngredient();
                craftingRecipeIngredient.FromBytes(reader, resolver);
                Ingredients[key] = craftingRecipeIngredient;
            }

            int num3 = reader.ReadInt32();
            Enchantments = new Dictionary<string, int>();
            for (int k = 0; k < num3; k++)
            {
                string key = reader.ReadString();
                Enchantments[key] = reader.ReadInt32();
            }
        }

        /// <summary>
        /// Creates a deep copy
        /// </summary>
        /// <returns></returns>
        public EnchantingRecipe Clone()
        {
            EnchantingRecipe recipe = new EnchantingRecipe();

            recipe.Ingredients = new Dictionary<string, CraftingRecipeIngredient>();
            if (Ingredients != null)
            {
                foreach (var val in Ingredients)
                {
                    recipe.Ingredients[val.Key] = val.Value.Clone();
                }
            }
            if (resolvedIngredients != null)
            {
                recipe.resolvedIngredients = new List<EnchantingRecipeIngredient>();
                foreach(EnchantingRecipeIngredient ing in resolvedIngredients)
                    recipe.resolvedIngredients.Add(ing.CloneTo<EnchantingRecipeIngredient>());
            }
            recipe.Enchantments = new Dictionary<string, int>();
            if (Enchantments != null)
            {
                foreach (var val in Enchantments)
                {
                    recipe.Enchantments[val.Key] = (int)val.Value;
                }
            }

            recipe.Name = Name;
            recipe.Attributes = Attributes?.Clone();
            recipe.RequiresTrait = RequiresTrait;
            recipe.RecipeGroup = RecipeGroup;
            recipe.CopyAttributesFrom = CopyAttributesFrom;
            recipe.processingHours = processingHours;
            return recipe;
        }
    }
    #endregion
    #region Ingredient
    public class EnchantingRecipeIngredient : CraftingRecipeIngredient
    {
        /// <summary>
        /// Identifier for Ingredients
        /// </summary>
        public string PatternCode;

        public override void ToBytes(BinaryWriter writer)
        {
            base.ToBytes(writer);
            writer.Write(PatternCode);
        }
        public override void FromBytes(BinaryReader reader, IWorldAccessor resolver)
        {
            base.FromBytes(reader, resolver);
            PatternCode = reader.ReadString();
        }
    }
    #endregion
}