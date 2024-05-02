﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.ServerMods;
using Vintagestory.GameContent;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace KRPGLib.Enchantment
{
    public class EnchantingRecipeSystem : ModSystem
    {
        public static bool canRegister = true;
        public static List<EnchantingRecipe> EnchantingRecipes { get; private set; }
        ICoreAPI Api;
        ICoreServerAPI sApi;
        bool classExclusiveRecipes = true;

        public override double ExecuteOrder()
        {
            return 0.6;
        }
        public override void StartPre(ICoreAPI api)
        {
            canRegister = true;
        }
        public override bool ShouldLoad(EnumAppSide side)
        {
            return side == EnumAppSide.Server;
        }
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            Api = api;
        }
        public override void StartServerSide(ICoreServerAPI api)
        {
            Api.Logger.Event("KRPG Enchanting Recipe System loaded.");
        }

        public override void Dispose()
        {
            base.Dispose();
        }
        // TODO: Change classExclusiveRecipes to our own config
        public override void AssetsLoaded(ICoreAPI api)
        {
            if (!(api is ICoreServerAPI sapi)) return;

            this.sApi = sapi;

            // TODO: Change classExclusiveRecipes to our own config
            classExclusiveRecipes = sapi.World.Config.GetBool("classExclusiveRecipes", true);

            EnchantingRecipes = new List<EnchantingRecipe>();
            LoadEnchantingRecipes();
        }
        // Load from files
        public void LoadEnchantingRecipes()
        {
            Dictionary<AssetLocation, JToken> files = sApi.Assets.GetMany<JToken>(sApi.Server.Logger, "recipes/enchanting-table");
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

            sApi.World.Logger.Event("{0} enchanting recipes loaded from {1} files", recipeQuantity, files.Count);
            sApi.World.Logger.StoryEvent(Lang.Get("Enchanting..."));
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

                        foreach (EnchantingRecipeIngredient ingred in rec.Ingredients.Values)
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

                        rec.Output.FillPlaceHolder(variantCode, variants[i % variants.Length]);
                    }

                    first = false;
                }

                foreach (EnchantingRecipe subRecipe in subRecipes)
                {
                    if (!subRecipe.ResolveIngredients(sApi.World)) continue;
                    RegisterEnchantingRecipe(subRecipe);
                }

            }
            else
            {
                if (!recipe.ResolveIngredients(sApi.World)) return;
                RegisterEnchantingRecipe(recipe);
            }
        }

        // Register to EnchantingRecipeSystem
        public void RegisterEnchantingRecipe(EnchantingRecipe recipe)
        {
            if (!canRegister) throw new InvalidOperationException("Coding error: Can no long register enchanting recipes. Register them during AssetsLoad/AssetsFinalize and with ExecuteOrder < 99999");
            EnchantingRecipes.Add(recipe);
        }
    }
}