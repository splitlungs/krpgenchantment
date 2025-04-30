using KRPGLib.Enchantment.API;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace KRPGLib.Enchantment
{
    public class EnchantingRecipeSystem : ModSystem
    {
        public static bool canRegister = true;
        /// <summary>
        /// List of all loaded enchanting recipes
        /// </summary>
        public List<EnchantingRecipe> EnchantingRecipes = new List<EnchantingRecipe>();
        ICoreAPI Api;

        public override double ExecuteOrder()
        {
            return 0;
        }
        public override void StartPre(ICoreAPI api)
        {
            canRegister = true;
        }
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            Api = api;
            EnchantingRecipes = api.RegisterRecipeRegistry<RecipeRegistryGeneric<EnchantingRecipe>>("enchantingrecipes").Recipes;
        }
        public void RegisterEnchantingRecipe(EnchantingRecipe recipe)
        {
            if (!canRegister)
            {
                throw new InvalidOperationException("[KRPGEnchantment] Coding error: Can no long register enchanting recipes. Register them during AssetsLoad/AssetsFinalize and with ExecuteOrder < 99999");
            }
            foreach (var ench in recipe.Enchantments)
            {
                IEnchantment enchant = Api.EnchantAccessor().EnchantmentRegistry.GetValueOrDefault(ench.Key, null);
                if (enchant?.Enabled != true) recipe.Enabled = false;
            }
            EnchantingRecipes.Add(recipe);
        }
    }
}