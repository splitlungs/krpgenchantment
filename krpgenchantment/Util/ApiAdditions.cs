using KRPGLib.Enchantment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.ServerMods;

namespace Vintagestory.GameContent
{
    public static class ApiAdditions
    {
        /// <summary>
        /// List of all loaded Enchanting Recipes
        /// </summary>
        /// <param name="api"></param>
        /// <returns></returns>
        public static List<EnchantingRecipe> GetEnchantingRecipes(this ICoreAPI api)
        {
            return api.ModLoader.GetModSystem<EnchantingRecipeSystem>().EnchantingRecipes;
        }

        /// <summary>
        /// Register an Enchanting Recipe
        /// </summary>
        /// <param name="api"></param>
        /// <param name="recipe"></param>
        public static void RegisterEnchantingRecipe(this ICoreServerAPI api, EnchantingRecipe recipe)
        {
            api.ModLoader.GetModSystem<EnchantingRecipeSystem>().RegisterEnchantingRecipe(recipe);
        }
    }
}