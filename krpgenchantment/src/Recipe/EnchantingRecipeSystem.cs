using KRPGLib.Enchantment.API;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace KRPGLib.Enchantment
{
    public class EnchantingRecipeSystem : ModSystem
    {
        public static bool canRegister = true;
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
            // EnchantingRecipes = api.RegisterRecipeRegistry<RecipeRegistryGeneric<EnchantingRecipe>>("enchantingrecipes").Recipes;
            api.Logger.Notification("[KRPGEnchantment] Enchanting Recipe Registry started.");
        }
        /*
        /// <summary>
        /// List of all loaded enchanting recipes
        /// </summary>
        public List<EnchantingRecipe> EnchantingRecipes = new List<EnchantingRecipe>();
        /// <summary>
        /// Registers the provided EnchantingRecipe to the server.
        /// </summary>
        /// <param name="recipe"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void RegisterEnchantingRecipe(EnchantingRecipe recipe)
        {
            if (!canRegister)
            {
                throw new InvalidOperationException("[KRPGEnchantment] Coding error: Can no long register enchanting recipes. Register them during AssetsLoad/AssetsFinalize and with ExecuteOrder < 99999");
            }
            // If one of the Enchantments is included in the recipe's output, we disable it.
            foreach (var ench in recipe.Enchantments)
            {
                IEnchantment enchant = Api.EnchantAccessor().GetEnchantment(ench.Key);
                if (enchant?.Enabled != true) recipe.Enabled = false;
                if (EnchantingConfigLoader.Config.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] Enchanting Recipe {0} set Enabled to {1} because it contains Enchantment {2} with value of {3}.", recipe.Name, recipe.Enabled, enchant.Code, recipe.Enabled);
            }
            EnchantingRecipes.Add(recipe);
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
            if (EnchantingRecipes != null)
            {
                foreach (EnchantingRecipe rec in EnchantingRecipes)
                {
                    if (rec.Enabled && rec.Matches(Api, inSlot, rSlot))
                    {
                        recipes.Add(rec.Clone());
                    }
                }
                if (recipes.Count > 0)
                    return recipes;
                else
                    return null;
            }
            else
                Api.Logger.Error("[KRPGEnchantment] EnchantingRecipe Registry could not be found! Please report error to author.");
            return null;
        }
        */
    }
}