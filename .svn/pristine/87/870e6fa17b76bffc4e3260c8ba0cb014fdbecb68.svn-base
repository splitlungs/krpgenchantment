using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace KRPGLib.Enchantment
{ 
    public class EnchantingRecipeIngredient : IRecipeIngredient
    {
        /// <summary>
        /// Identifier for Ingredients
        /// </summary>
        public string PatternCode;

        //
        // Summary:
        //     Item or Block
        public EnumItemClass Type;

        //
        // Summary:
        //     How much input items are required
        public int Quantity = 1;

        //
        // Summary:
        //     What attributes this itemstack must have
        [JsonProperty]
        [JsonConverter(typeof(JsonAttributesConverter))]
        public JsonObject Attributes;

        //
        // Summary:
        //     Optional attribute data that you can attach any data to
        [JsonProperty]
        [JsonConverter(typeof(JsonAttributesConverter))]
        public JsonObject RecipeAttributes;

        //
        // Summary:
        //     Whether this crafting recipe ingredient should be regarded as a tool required
        //     to build this item. If true, the recipe will not consume the item but reduce
        //     its durability.
        public bool IsTool;

        //
        // Summary:
        //     If IsTool is set, this is the durability cost
        public int ToolDurabilityCost = 1;

        //
        // Summary:
        //     When using a wildcard in the item/block code, setting this field will limit the
        //     allowed variants
        public string[] AllowedVariants;

        //
        // Summary:
        //     When using a wildcard in the item/block code, setting this field will skip these
        //     variants
        public string[] SkipVariants;

        //
        // Summary:
        //     If set, the crafting recipe will give back the consumed stack to be player upon
        //     crafting
        public JsonItemStack ReturnedStack;

        //
        // Summary:
        //     The itemstack made from Code, Quantity and Attributes, populated by the engine
        public ItemStack ResolvedItemstack;

        //
        // Summary:
        //     Whether this recipe contains a wildcard, populated by the engine
        public bool IsWildCard;

        //
        // Summary:
        //     Code of the item or block
        public AssetLocation Code { get; set; }

        //
        // Summary:
        //     Name of the class, used for filling placeholders in the output stack
        public string Name { get; set; }

        //
        // Summary:
        //     Turns Type, Code and Attributes into an IItemStack
        //
        // Parameters:
        //   resolver:
        //
        //   sourceForErrorLogging:
        public bool Resolve(IWorldAccessor resolver, string sourceForErrorLogging)
        {
            if (ReturnedStack != null)
            {
                ReturnedStack.Resolve(resolver, sourceForErrorLogging + " recipe with output ", Code);
            }

            if (Code.Path.Contains('*'))
            {
                IsWildCard = true;
                return true;
            }

            if (Type == EnumItemClass.Block)
            {
                Block block = resolver.GetBlock(Code);
                if (block == null || block.IsMissing)
                {
                    resolver.Logger.Warning("Failed resolving crafting recipe ingredient with code {0} in {1}", Code, sourceForErrorLogging);
                    return false;
                }

                ResolvedItemstack = new ItemStack(block, Quantity);
            }
            else
            {
                Item item = resolver.GetItem(Code);
                if (item == null || item.IsMissing)
                {
                    resolver.Logger.Warning("Failed resolving crafting recipe ingredient with code {0} in {1}", Code, sourceForErrorLogging);
                    return false;
                }

                ResolvedItemstack = new ItemStack(item, Quantity);
            }

            if (Attributes != null)
            {
                IAttribute attribute = Attributes.ToAttribute();
                if (attribute is ITreeAttribute)
                {
                    ResolvedItemstack.Attributes = (ITreeAttribute)attribute;
                }
            }

            return true;
        }

        //
        // Summary:
        //     Checks whether or not the input satisfies as an ingredient for the recipe.
        //
        // Parameters:
        //   inputStack:
        //
        //   checkStacksize:
        public bool SatisfiesAsIngredient(ItemStack inputStack, bool checkStacksize = true)
        {
            if (inputStack == null)
            {
                return false;
            }

            if (IsWildCard)
            {
                if (Type != inputStack.Class)
                {
                    return false;
                }

                if (!WildcardUtil.Match(Code, inputStack.Collectible.Code, AllowedVariants))
                {
                    return false;
                }

                if (SkipVariants != null && WildcardUtil.Match(Code, inputStack.Collectible.Code, SkipVariants))
                {
                    return false;
                }

                if (checkStacksize && inputStack.StackSize < Quantity)
                {
                    return false;
                }
            }
            else
            {
                if (!ResolvedItemstack.Satisfies(inputStack))
                {
                    return false;
                }

                if (checkStacksize && inputStack.StackSize < ResolvedItemstack.StackSize)
                {
                    return false;
                }
            }

            return true;
        }

        public EnchantingRecipeIngredient Clone()
        {
            EnchantingRecipeIngredient recipe = new EnchantingRecipeIngredient();
            // recipe.PatternCode = PatternCode;
            recipe.Code = Code.Clone();
            recipe.Type = Type;
            recipe.Name = Name;
            recipe.Quantity = Quantity;
            recipe.IsWildCard = IsWildCard;
            recipe.IsTool = IsTool;
            recipe.ToolDurabilityCost = ToolDurabilityCost;
            recipe.AllowedVariants = ((AllowedVariants == null) ? null : ((string[])AllowedVariants.Clone()));
            recipe.SkipVariants = ((SkipVariants == null) ? null : ((string[])SkipVariants.Clone()));
            recipe.ResolvedItemstack = ResolvedItemstack?.Clone();
            recipe.ReturnedStack = ReturnedStack?.Clone();
            recipe.RecipeAttributes = RecipeAttributes?.Clone();
            
            if (Attributes != null)
            {
                recipe.Attributes = Attributes.Clone();
            }


            return recipe;
        }

        public override string ToString()
        {
            return Type.ToString() + " code " + Code;
        }

        //
        // Summary:
        //     Fills in the placeholder ingredients for the crafting recipe.
        //
        // Parameters:
        //   key:
        //
        //   value:
        public void FillPlaceHolder(string key, string value)
        {
            Code = Code.CopyWithPath(Code.Path.Replace("{" + key + "}", value));
            Attributes?.FillPlaceHolder(key, value);
            RecipeAttributes?.FillPlaceHolder(key, value);
        }

        public virtual void ToBytes(BinaryWriter writer)
        {
            writer.Write(IsWildCard);
            writer.Write((int)Type);
            writer.Write(Code.ToShortString());
            writer.Write(Quantity);
            if (!IsWildCard)
            {
                writer.Write(ResolvedItemstack != null);
                ResolvedItemstack?.ToBytes(writer);
            }

            writer.Write(IsTool);
            writer.Write(ToolDurabilityCost);
            writer.Write(AllowedVariants != null);
            if (AllowedVariants != null)
            {
                writer.Write(AllowedVariants.Length);
                for (int i = 0; i < AllowedVariants.Length; i++)
                {
                    writer.Write(AllowedVariants[i]);
                }
            }

            writer.Write(SkipVariants != null);
            if (SkipVariants != null)
            {
                writer.Write(SkipVariants.Length);
                for (int j = 0; j < SkipVariants.Length; j++)
                {
                    writer.Write(SkipVariants[j]);
                }
            }

            writer.Write(ReturnedStack?.ResolvedItemstack != null);
            if (ReturnedStack?.ResolvedItemstack != null)
            {
                ReturnedStack.ToBytes(writer);
            }

            if (RecipeAttributes != null)
            {
                writer.Write(value: true);
                writer.Write(RecipeAttributes.ToString());
            }
            else
            {
                writer.Write(value: false);
            }
            // writer.Write(PatternCode);
        }

        public virtual void FromBytes(BinaryReader reader, IWorldAccessor resolver)
        {
            IsWildCard = reader.ReadBoolean();
            Type = (EnumItemClass)reader.ReadInt32();
            Code = new AssetLocation(reader.ReadString());
            Quantity = reader.ReadInt32();
            if (!IsWildCard && reader.ReadBoolean())
            {
                ResolvedItemstack = new ItemStack(reader, resolver);
            }

            IsTool = reader.ReadBoolean();
            ToolDurabilityCost = reader.ReadInt32();
            if (reader.ReadBoolean())
            {
                AllowedVariants = new string[reader.ReadInt32()];
                for (int i = 0; i < AllowedVariants.Length; i++)
                {
                    AllowedVariants[i] = reader.ReadString();
                }
            }

            if (reader.ReadBoolean())
            {
                SkipVariants = new string[reader.ReadInt32()];
                for (int j = 0; j < SkipVariants.Length; j++)
                {
                    SkipVariants[j] = reader.ReadString();
                }
            }

            if (reader.ReadBoolean())
            {
                ReturnedStack = new JsonItemStack();
                ReturnedStack.FromBytes(reader, resolver.ClassRegistry);
                ReturnedStack.ResolvedItemstack.ResolveBlockOrItem(resolver);
            }

            if (reader.ReadBoolean())
            {
                RecipeAttributes = new JsonObject(JToken.Parse(reader.ReadString()));
            }

            // PatternCode = reader.ReadString();
        }
    }
}
