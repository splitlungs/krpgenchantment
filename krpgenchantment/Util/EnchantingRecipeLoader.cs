using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace KRPGLib.Enchantment
{
    public class EnchantingRecipeLoader : ModSystem
    {
        public const string ConfigFile = "KRPGEnchantRecipeConfig.json";
        public KRPGEnchantRecipeConfig Config { get; set; }

        ICoreServerAPI api;

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
            this.api = sapi;

            try
            {
                Config = sapi.LoadModConfig<KRPGEnchantRecipeConfig>(ConfigFile);
                if (Config == null)
                {
                    Config = new KRPGEnchantRecipeConfig();
                    sapi.StoreModConfig(Config, ConfigFile);

                    sapi.Logger.Event("Loaded KRPGEnchantRecipeConfig from file.");
                }
            }
            catch (Exception e)
            {
                sapi.Logger.Error("Error loading KRPGEnchantRecipeConfig: {0}", e);
                return;
            }
            classExclusiveRecipes = sapi.World.Config.GetBool("classExclusiveRecipes", true);
            
            LoadEnchantingRecipes();
        }

        public void LoadEnchantingRecipes()
        {
            Dictionary<AssetLocation, JToken> files = api.Assets.GetMany<JToken>(api.Server.Logger, "recipes/enchanting-table", "krpgenchantment");

            if (Config.EnableKRPGWands)
                files.AddRange(api.Assets.GetMany<JToken>(api.Server.Logger, "recipes/enchanting-table", "krpgwands"));
            if (Config.EnablePaxel)
                files.AddRange(api.Assets.GetMany<JToken>(api.Server.Logger, "recipes/enchanting-table", "paxel"));
            if (Config.EnableSwordz)
                files.AddRange(api.Assets.GetMany<JToken>(api.Server.Logger, "recipes/enchanting-table", "swordz"));

            int recipeQuantity = 0;

            foreach (var val in files)
            {
                if (val.Value is JObject)
                {
                    LoadRecipe(val.Key, val.Value.ToObject<EnchantingRecipe>(val.Key.Domain));
                    recipeQuantity++;
                }
                if (val.Value is JArray)
                {
                    foreach (var token in (val.Value as JArray))
                    {
                        LoadRecipe(val.Key, token.ToObject<EnchantingRecipe>(val.Key.Domain));
                        recipeQuantity++;
                    }
                }
            }

            api.World.Logger.Event("{0} enchanting recipes loaded from {1} files", recipeQuantity, files.Count);
            api.World.Logger.StoryEvent(Lang.Get("Enchanting..."));
        }

        public void LoadRecipe(AssetLocation loc, EnchantingRecipe recipe)
        {
            if (!recipe.Enabled) return;
            if (!classExclusiveRecipes) recipe.RequiresTrait = null;

            if (recipe.Name == null) recipe.Name = loc;

            Dictionary<string, string[]> nameToCodeMapping = recipe.GetNameToCodeMapping(api.World);

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
                    if (!subRecipe.ResolveIngredients(api.World)) continue;
                    api.RegisterEnchantingRecipe(subRecipe);
                }

            }
            else
            {
                if (!recipe.ResolveIngredients(api.World)) return;
                api.RegisterEnchantingRecipe(recipe);
            }
        }
    }
}
