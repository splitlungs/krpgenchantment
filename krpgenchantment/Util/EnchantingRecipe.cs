using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
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

        public EnchantingRecipeIngredient[] resolvedIngredients;

        IWorldAccessor world;

        /// <summary>
        /// Returns an Enchanted ItemStack
        /// </summary>
        /// <param name="inStack"></param>
        /// <returns></returns>
        public ItemStack OutStack(ItemStack inStack)
        {
            if (!(inStack != null)) return null;

            ItemStack outStack = inStack.Clone();

            // Set Qty
            outStack.StackSize = resolvedIngredients[1].Quantity;

            // Apply Enchantments
            foreach (KeyValuePair<string, int> enchant in Enchantments)
            {
                // Overwrite Healing
                if (enchant.Key == EnumEnchantments.healing.ToString())
                {
                    outStack.Attributes.SetInt(EnumEnchantments.flaming.ToString(), 0);
                    outStack.Attributes.SetInt(EnumEnchantments.frost.ToString(), 0);
                    outStack.Attributes.SetInt(EnumEnchantments.harming.ToString(), 0);
                    outStack.Attributes.SetInt(EnumEnchantments.shocking.ToString(), 0);
                }
                // Overwrite Alternate Damage
                else if (enchant.Key == EnumEnchantments.flaming.ToString() || enchant.Key == EnumEnchantments.frost.ToString() 
                    || enchant.Key == EnumEnchantments.harming.ToString() || enchant.Key == EnumEnchantments.shocking.ToString()
                    )
                    outStack.Attributes.SetInt(EnumEnchantments.healing.ToString(), 0);

                // Write Enchant
                outStack.Attributes.SetInt(enchant.Key, enchant.Value);
            }

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
            // HARDCODED TO 2 INGREDIENTS CURRENTLY
            string pCode = "";
            resolvedIngredients = new EnchantingRecipeIngredient[2];
            for (int i = 0; i < 2; i++)
            {
                if (i == 0) pCode = "reagent";
                if (i == 1) pCode = "target";

                if (!Ingredients.ContainsKey(pCode))
                {
                    world.Logger.Error("Enchanting Recipe {0} contains an ingredient pattern code {1} but supplies no ingredient for it.", Name, pCode);
                    return false;
                }

                if (!Ingredients[pCode].Resolve(world, "Enchanting recipe"))
                {
                    world.Logger.Error("Enchanting {0} contains an ingredient that cannot be resolved: {1}", pCode, Ingredients[pCode]);
                    return false;
                }

                resolvedIngredients[i] = Ingredients[pCode].CloneTo<EnchantingRecipeIngredient>();
                resolvedIngredients[i].PatternCode = pCode;
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

            for (int i = 0; i < resolvedIngredients.Length; i++)
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
        public bool Matches(ItemSlot inputSlot, ItemSlot reagentSlot)
        {
            // Null Check
            if (inputSlot?.Itemstack == null || reagentSlot?.Itemstack == null) return false;

            // Check Reagent
            if (reagentSlot?.Itemstack != null)
            {
                if (resolvedIngredients[0].IsWildCard)
                {
                    bool foundw = false;
                    foundw =
                            resolvedIngredients[0].Type == reagentSlot.Itemstack.Class &&
                            WildcardUtil.Match(resolvedIngredients[0].Code, reagentSlot.Itemstack.Collectible.Code, resolvedIngredients[0].AllowedVariants)
                        ;
                    if (!foundw) return false;
                }
                else if (!resolvedIngredients[0].ResolvedItemstack.Satisfies(reagentSlot.Itemstack)) return false;

                if (reagentSlot.Itemstack.StackSize != resolvedIngredients[0].Quantity) return false;
            }
            // Check Input/Target
            if (inputSlot?.Itemstack != null)
            {
                if (resolvedIngredients[1].IsWildCard)
                {
                    inputSlot.Itemstack.Collectible.WildCardMatch(resolvedIngredients[1].Code);

                    bool foundw = false;
                    foundw =
                            resolvedIngredients[1].Type == inputSlot.Itemstack.Class &&
                            WildcardUtil.Match(resolvedIngredients[1].Code, inputSlot.Itemstack.Collectible.Code, resolvedIngredients[1].AllowedVariants)
                        ;
                    if (!foundw) return false;
                }
                else if (!resolvedIngredients[1].ResolvedItemstack.Satisfies(inputSlot.Itemstack)) return false;
                
                if (inputSlot.Itemstack.StackSize < resolvedIngredients[1].Quantity) return false;
            }
            
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
        public bool TryCraftNow(ICoreAPI api, double nowProcessedHours, ItemSlot[] inputslots)
        {
            if (nowProcessedHours < processingHours) return false;

            inputslots[1].Itemstack = OutStack(inputslots[0].Itemstack).Clone();
            return true;
        }
        /// <summary>
        /// Serialized the recipe
        /// </summary>
        /// <param name="writer"></param>
        public void ToBytes(BinaryWriter writer)
        {
            writer.Write(processingHours);
            for (int i = 0; i < resolvedIngredients.Length; i++)
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
            foreach(KeyValuePair<string, int> enchant in Enchantments)
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
            resolvedIngredients = new EnchantingRecipeIngredient[2];
            for (int i = 0; i < resolvedIngredients.Length; i++)
            {
                if (!reader.ReadBoolean())
                {
                    resolvedIngredients[i] = new EnchantingRecipeIngredient();
                    resolvedIngredients[i].FromBytes(reader, resolver);
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
            int num = reader.ReadInt32();
            Ingredients = new Dictionary<string, CraftingRecipeIngredient>();
            for (int j = 0; j < num; j++)
            {
                string key = reader.ReadString();
                CraftingRecipeIngredient craftingRecipeIngredient = new CraftingRecipeIngredient();
                craftingRecipeIngredient.FromBytes(reader, resolver);
                Ingredients[key] = craftingRecipeIngredient;
            }

            int ench = reader.ReadInt32();
            Enchantments = new Dictionary<string, int>();
            for (int k = 0; k < ench; k++)
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
                recipe.resolvedIngredients = new EnchantingRecipeIngredient[resolvedIngredients.Length];
                for (int i = 0; i < resolvedIngredients.Length; i++)
                {
                    recipe.resolvedIngredients[i] = resolvedIngredients[i]?.CloneTo<EnchantingRecipeIngredient>();
                }
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