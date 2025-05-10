using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using KRPGLib.Enchantment.API;
using Vintagestory.API.Datastructures;

namespace KRPGLib.Enchantment
{
    public class EnchantingRecipeLoader : ModSystem
    {
        ICoreServerAPI sApi;

        public override double ExecuteOrder()
        {
            return 1;
        }

        public override bool ShouldLoad(EnumAppSide side)
        {
            return side == EnumAppSide.Server;
        }

        bool classExclusiveRecipes = true;

        public override void AssetsLoaded(ICoreAPI api)
        {
            if (!(api is ICoreServerAPI sapi)) return;
            this.sApi = sapi;

            LoadEnchantingRecipes();
        }

        public void ReloadEnchantingRecipes()
        {
            
            sApi.World.Logger.Warning("[KRPGEnchantment] Reloading KRPG Enchantment Recipes!");
        }

        public void LoadEnchantingRecipes()
        {
            Dictionary<AssetLocation, JToken> files = sApi.Assets.GetMany<JToken>(sApi.Server.Logger, "recipes/enchanting-table", "krpgenchantment");
            if (EnchantingConfigLoader.Config.CustomPatches.Count > 0)
            {
                foreach (KeyValuePair<string, bool> keyValuePair in EnchantingConfigLoader.Config.CustomPatches)
                {
                    if (keyValuePair.Value != true)
                        continue;

                    if (sApi.ModLoader.IsModEnabled(keyValuePair.Key.ToLower()))
                        files.AddRange(sApi.Assets.GetMany<JToken>(sApi.Server.Logger, "compatibility/recipes/enchanting-table/" + keyValuePair.Key.ToLower(), "krpgenchantment"));

                    if (sApi.ModLoader.IsModEnabled(keyValuePair.Key.ToLower()))
                        files.AddRange(sApi.Assets.GetMany<JToken>(sApi.Server.Logger, "recipes/enchanting-table" + keyValuePair.Key.ToLower()));
                }
            }

            int fileQuantity = 0;

            foreach (var val in files)
            {
                if (val.Value is JObject)
                {
                    LoadRecipe(val.Key, val.Value.ToObject<EnchantingRecipe>(val.Key.Domain));
                    fileQuantity++;
                }
                if (val.Value is JArray)
                {
                    foreach (var token in (val.Value as JArray))
                    {
                        LoadRecipe(val.Key, token.ToObject<EnchantingRecipe>(val.Key.Domain));
                        fileQuantity++;
                    }
                }
            }
            sApi.World.Logger.Notification("[KRPGEnchantment] {0} enchanting recipes loaded from {1} files.", fileQuantity, files.Count);
            sApi.World.Logger.StoryEvent(Lang.Get("Enchanting..."));

            // try
            // {
            // }
            // catch (Exception e)
            // {
            //     throw new Exception("[KRPGEnchantment] Error loading Enchantment Recipe: " + e);
            //     // sApi.Logger.Error("[KRPGEnchantment] Error loading Enchantment Recipe: {0}", e);
            //     // return;
            // }
        }

        public void LoadRecipe(AssetLocation loc, EnchantingRecipe recipe)
        {
            if (!recipe.Enabled) return;

            if (!classExclusiveRecipes) recipe.RequiresTrait = null;

            if (recipe.Name == null) recipe.Name = loc;

            Dictionary<string, string[]> nameToCodeMapping = recipe.GetNameToCodeMapping(sApi.World);

            if (nameToCodeMapping.Count > 0)
            {
                List<EnchantingRecipe> subRecipes = new List<EnchantingRecipe>();

                int qCombs = 0;
                bool first = true;
                foreach (var val2 in nameToCodeMapping)
                {
                    if (first) qCombs = val2.Value.Length;
                    else qCombs *= val2.Value.Length;
                    first = false;
                }

                first = true;
                foreach (var val2 in nameToCodeMapping)
                {
                    string variantCode = val2.Key;
                    string[] variants = val2.Value;

                    for (int i = 0; i < qCombs; i++)
                    {
                        EnchantingRecipe rec;

                        if (first) subRecipes.Add(rec = recipe.Clone());
                        else rec = subRecipes[i];

                        foreach (CraftingRecipeIngredient ingred in rec.Ingredients.Values)
                        {
                            if (ingred.Name == variantCode)
                            {
                                ingred.Code.Path = ingred.Code.Path.Replace("*", variants[i % variants.Length]);
                            }

                            if (ingred.ReturnedStack?.Code != null)
                            {
                                ingred.ReturnedStack.Code.Path.Replace("{" + variantCode + "}", variants[i % variants.Length]);
                            }
                        }
                    }

                    first = false;
                }

                foreach (EnchantingRecipe subRecipe in subRecipes)
                {
                    if (!subRecipe.ResolveIngredients(sApi.World)) continue;
                    sApi.EnchantAccessor().RegisterEnchantingRecipe(subRecipe);
                }

            }
            else
            {
                if (!recipe.ResolveIngredients(sApi.World)) return;
                sApi.EnchantAccessor().RegisterEnchantingRecipe(recipe);
            }

        }
    }
}
