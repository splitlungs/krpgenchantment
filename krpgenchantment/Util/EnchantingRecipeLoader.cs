﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace KRPGLib.Enchantment
{
    public class EnchantingRecipeLoader : ModSystem
    {
        private const double ConfigVersion = 0.4d;
        public const string ConfigFile = "KRPGEnchantment_Recipe_Config.json";
        public static KRPGEnchantRecipeConfig Config { get; set; } = null!;

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

            try
            {
                Config = sapi.LoadModConfig<KRPGEnchantRecipeConfig>(ConfigFile);
                if (Config == null)
                {
                    Config = new KRPGEnchantRecipeConfig();
                    Config.Version = ConfigVersion;
                    sapi.StoreModConfig(Config, ConfigFile);

                    sapi.Logger.Event("KRPGEnchantRecipeConfig file not found. A new one has been created.");
                }
                else if (Config.Version < ConfigVersion)
                {
                    KRPGEnchantRecipeConfig tempConfig = new KRPGEnchantRecipeConfig();
                    if (Config.EnchantTimeOverride >= 0) tempConfig.EnchantTimeOverride = Config.EnchantTimeOverride;
                    if (Config.EnchantResetOverride >= 0) tempConfig.EnchantResetOverride = Config.EnchantResetOverride;
                    if (Config.MaxLatentEnchants != 3) tempConfig.MaxLatentEnchants = Config.MaxLatentEnchants;
                    if (Config.ValidReagents?.Count > 0) tempConfig.ValidReagents = Config.ValidReagents;
                    // These will be deprecated in favor of CustomPatches at next release
                    if (Config.EnableAncientArmory) tempConfig.CustomPatches["AncientArmory"] = true;
                    else tempConfig.CustomPatches["AncientArmory"] = false;
                    if (Config.EnableKRPGWands) tempConfig.CustomPatches["KRPGWands"] = true;
                    else tempConfig.CustomPatches["KRPGWands"] = false;
                    if (Config.EnablePaxel) tempConfig.CustomPatches["Paxel"] = true;
                    else tempConfig.CustomPatches["Paxel"] = false;
                    if (Config.EnableRustboundMagic) tempConfig.CustomPatches["RustboundMagic"] = true;
                    else tempConfig.CustomPatches["EnableRustboundMagic"] = false;
                    if (Config.EnableSpearExpantion) tempConfig.CustomPatches["SpearExpantion"] = true;
                    else tempConfig.CustomPatches["SpearExpantion"] = false;
                    if (Config.EnableSwordz) tempConfig.CustomPatches["Swordz"] = true;
                    else tempConfig.CustomPatches["Swordz"] = false;
                    if (Config.CustomPatches?.Count > 0) tempConfig.CustomPatches = Config.CustomPatches;
                    
                    tempConfig.Version = ConfigVersion;
                    Config = tempConfig;
                    sapi.StoreModConfig(Config, ConfigFile);

                    sapi.Logger.Event("KRPGEnchantRecipeConfig file is outdated. Migrated to version {0} successfully.", ConfigVersion);
                }
                else
                    sapi.Logger.Event("KRPGEnchantRecipeConfig file found. Loaded successfully.");
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
            Dictionary<AssetLocation, JToken> files = sApi.Assets.GetMany<JToken>(sApi.Server.Logger, "recipes/enchanting-table", "krpgenchantment");
            if (Config.CustomPatches.Count > 0)
            {
                foreach (KeyValuePair<string, bool> keyValuePair in Config.CustomPatches)
                {
                    if (keyValuePair.Value == true)
                        files.AddRange(sApi.Assets.GetMany<JToken>(sApi.Server.Logger, "recipes/enchanting-table", keyValuePair.Key.ToLower()));
                }
            }

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
                    sApi.RegisterEnchantingRecipe(subRecipe);
                }

            }
            else
            {
                if (!recipe.ResolveIngredients(sApi.World)) return;
                sApi.RegisterEnchantingRecipe(recipe);
            }
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            ICoreServerAPI sApi = api as ICoreServerAPI;

            sApi.ChatCommands.GetOrCreate("krpg")
            .WithDescription(Lang.Get("krpgenchantment:dsc-cmd-krpg"))
            .RequiresPrivilege(Privilege.controlserver)
            .BeginSubCommand("enchantment")
            .WithDescription(Lang.Get("krpgenchantment:dsc-cmd-enchantment"))
            .RequiresPrivilege(Privilege.controlserver)
            .BeginSubCommand("reload")
            .WithDescription(Lang.Get("krpgenchantment:dsc-cmd-reload-config"))
            .RequiresPrivilege(Privilege.controlserver)
            .HandleWith(_ =>
            {
                if (ReloadConfig())
                {
                    return TextCommandResult.Success(Lang.Get("krpgenchantment:cmd-reloadcfg-msg"));
                }

                return TextCommandResult.Error(Lang.Get("krpgenchantment:cmd-reloadcfg-fail"));
            })
            .EndSubCommand()
            .EndSubCommand()
            .Validate();
        }
        private bool ReloadConfig()
        {
            try
            {
                var configTemp = sApi.LoadModConfig<KRPGEnchantRecipeConfig>(ConfigFile);
                Config.Reload(configTemp);
            }
            catch (Exception e)
            {
                sApi.Logger.Error("Error reloading KRPGEnchantment Recipe Config: ", e.ToString());
                return false;
            }

            return true;
        }

    }
}
