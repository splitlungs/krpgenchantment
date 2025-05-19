using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using KRPGLib.Enchantment;
using Vintagestory.GameContent;
using Vintagestory.API.Common.Entities;
using System.Runtime.CompilerServices;

namespace KRPGLib.Enchantment.API
{
    /// <summary>
    /// Primary controller for KRPG Enchantments.
    /// </summary>
    public interface IEnchantmentServerAccessor
    {
        #region Registration
        /// <summary>
        /// Register an Enchantment to the EnchantmentRegistry. All Enchantments must be registered here. Returns false if it fails to register.
        /// </summary>
        /// <param name="enchantClass"></param>
        /// <param name="configLocation"></param>
        /// <param name="t"></param>
        bool RegisterEnchantmentClass(string enchantClass, string configLocation, Type t);
        #endregion
        #region Recipes
        /*
        /// <summary>
        /// Returns a List of EnchantingRecipes that match the provided slots, or null if something went wrong.
        /// </summary>
        /// <param name="inSlot"></param>
        /// <param name="rSlot"></param>
        /// <returns></returns>
        List<EnchantingRecipe> GetValidEnchantingRecipes(ItemSlot inSlot, ItemSlot rSlot);
        /// <summary>
        /// Registers the provided EnchantingRecipe to the server.
        /// </summary>
        /// <param name="recipe"></param>
        void RegisterEnchantingRecipe(EnchantingRecipe recipe);
        */
        /// <summary>
        /// Returns a List of Enchantments that can be written to the ItemStack, or null if something went wrong.
        /// </summary>
        /// <param name="inSlot"></param>
        /// <returns></returns>
        List<string> GetValidEnchantments(ItemSlot inSlot);
        /// <summary>
        /// Returns true if the provided Input and Reagent can accept the provided Enchantment code.
        /// </summary>
        /// <param name="inStack"></param>
        /// <param name="rStack"></param>
        /// <param name="enchant"></param>
        /// <returns></returns>
        bool CanEnchant(ItemStack inStack, ItemStack rStack, string enchant);
        /// <summary>
        /// Returns the quantity of a provided Reagent, as set in the Config under ValidReagents. Set to 0 to disable Reagent consumption. Returns -1 if nothing is found.
        /// </summary>
        /// <param name="stack"></param>
        /// <returns></returns>
        int GetReagentQuantity(ItemStack stack);
        /// <summary>
        /// Returns an enchanted ItemStack. Provide int greater than 0 to override reagent potential.
        /// </summary>
        /// <param name="inSlot"></param>
        /// <param name="rSlot"></param>
        /// <param name="enchantments"></param>
        /// <returns></returns>
        ItemStack EnchantItem(ItemSlot inSlot, ItemSlot rSlot, Dictionary<string, int> enchantments);
        /// <summary>
        /// Attempts to get base EnumTool type from an item, or interperited ID for a non-tool, then converts to string. This should match your ValidToolTypes in the Enchantment. Returns null if none can be found.
        /// </summary>
        /// <param name="stack"></param>
        /// <returns></returns>
        string GetToolType(ItemStack stack);
        #endregion
        #region Assessments
        IEnchantment GetEnchantment(string enchantCode);
        /// <summary>
        /// Returns all Enchantments in the ItemStack's Attributes or null if none are found.
        /// </summary>
        /// <param name="itemStack"></param>
        /// <returns></returns>
        Dictionary<string, int> GetEnchantments(ItemStack itemStack);
        /// <summary>
        /// Returns a List of Latent Enchantments pending for the contained Itemstack or null if there are none.
        /// </summary>
        /// <param name="inSlot"></param>
        /// <param name="encrypt"></param>
        /// <returns></returns>
        List<string> GetLatentEnchants(ItemSlot inSlot, bool encrypt);
        /// <summary>
        /// Returns if the ItemStack is Enchantable or not.
        /// </summary>
        /// <param name="inSlot"></param>
        /// <returns></returns>
        bool IsEnchantable(ItemSlot inSlot);
        /// <summary>
        /// Returns True if we successfully wrote new LatentEnchants to the item, or False if not.
        /// </summary>
        /// <param name="inSlot"></param>
        /// <returns></returns>
        bool AssessItem(ItemSlot inSlot, ItemSlot rSlot);
        /// <summary>
        /// Call to assign a EnchantPotential attribute to an item. Returns 0 if it is not valid.
        /// </summary>
        /// <param name="stack"></param>
        /// <returns></returns>
        int AssessReagent(ItemStack stack);
        #endregion
        #region Lore
        /// <summary>
        /// Returns true if the given player can decrypt the enchant.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="recipe"></param>
        /// <returns></returns>
        // bool CanReadEnchant(string player, EnchantingRecipe recipe);
        /// <summary>
        /// Returns true if the given player can decrypt the enchant. enchantName must be in the format of an AssetLocation.Name.ToShortString() (Ex: "domain:enchant-name")
        /// </summary>
        /// <param name="player"></param>
        /// <param name="enchantName"></param>
        /// <returns></returns>
        bool CanReadEnchant(string player, string enchantName);
        #endregion
    }
}