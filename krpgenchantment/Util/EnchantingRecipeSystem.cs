using Newtonsoft.Json.Linq;
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
using Vintagestory.API.Client;

namespace KRPGLib.Enchantment
{
    // TODO: Deprecate
    /*
    public class EnchantingRecipeRegistry : RecipeRegistryGeneric<EnchantingRecipe>
    {   
        // private EnchantingRecipeRegistry()
        // {
        //     //do our intialisation stuff, only once!
        // }
        // 
        // private static readonly EnchantingRecipeRegistry registry = new EnchantingRecipeRegistry();
        // public static EnchantingRecipeRegistry Registry
        // {
        //     get
        //     {
        //         return registry;
        //     }
        // }
        // 
        // private List<EnchantingRecipe> enchantingRecipes = new List<EnchantingRecipe>();
        // /// <summary>
        // /// List of all loaded Enchanting Recipes
        // /// </summary>
        // public List<EnchantingRecipe> EnchantingRecipes
        // {
        //     get
        //     {
        //         return enchantingRecipes;
        //     }
        //     set
        //     {
        //         enchantingRecipes = value;
        //     }
        // }
        public List<EnchantingRecipe> EnchantingRecipes;

        public EnchantingRecipeRegistry() 
        { 
            EnchantingRecipes = new List<EnchantingRecipe>();
        }
        public EnchantingRecipeRegistry(List<EnchantingRecipe> recipes)
        {
            EnchantingRecipes = recipes;
        }
        public override void FromBytes(IWorldAccessor resolver, int quantity, byte[] data)
        {
            using MemoryStream input = new MemoryStream(data);
            BinaryReader reader = new BinaryReader(input);
            for (int i = 0; i < quantity; i++)
            {
                EnchantingRecipe item = new EnchantingRecipe();
                item.FromBytes(reader, resolver);
                EnchantingRecipes.Add(item);
            }
        }

        public override void ToBytes(IWorldAccessor resolver, out byte[] data, out int quantity)
        {
            quantity = EnchantingRecipes.Count;
            using MemoryStream memoryStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(memoryStream);
            foreach (EnchantingRecipe recipe in EnchantingRecipes)
            {
                recipe.ToBytes(writer);
            }

            data = memoryStream.ToArray();
        }
    }*/
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
            return 0.6;
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
        /*
        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            sApi = api;
            Api.Logger.Event("KRPG Enchanting Recipe System loaded.");
        }
        public override void Dispose()
        {
            base.Dispose();
        }
        public override void AssetsLoaded(ICoreAPI api)
        {
            // Only load on Server side
            if (!(api is ICoreServerAPI sapi)) return;
            this.sApi = sapi;

            // TODO: Change classExclusiveRecipes to our own config
            //classExclusiveRecipes = sapi.World.Config.GetBool("classExclusiveRecipes", true);

            // LoadEnchantingRecipes();
        }
        public override void AssetsFinalize(ICoreAPI api)
        {
            base.AssetsFinalize(api);
        }
        // Load from files
        void LoadEnchantingRecipes()
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
        }*/
        public void RegisterEnchantingRecipe(EnchantingRecipe recipe)
        {
            if (!canRegister)
            {
                throw new InvalidOperationException("Coding error: Can no long register enchanting recipes. Register them during AssetsLoad/AssetsFinalize and with ExecuteOrder < 99999");
            }

            EnchantingRecipes.Add(recipe);
        }
    }
}