using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace KRPGLib.Enchantment
{
    /*
    [HarmonyPatch(typeof(RecipeRegistrySystem))]
    internal class RecipeRegistrySystem_Patch
    {
        static List<EnchantingRecipe> EnchantingRecipes;

        [HarmonyPatch("Start")]
        public static void Postfix(RecipeRegistrySystem __instance, ICoreAPI api)
        {

            EnchantingRecipes = api.RegisterRecipeRegistry<RecipeRegistryGeneric<EnchantingRecipe>>("enchantingrecipes").Recipes;
        }
    }*/
}
