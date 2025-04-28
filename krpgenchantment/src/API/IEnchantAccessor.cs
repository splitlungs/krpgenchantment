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

namespace KRPGLib.Enchantment.API
{
    public interface IEnchantAccessor
    {
        /// <summary>
        /// Returns all Enchantments on the ItemStack's Attributes in the ItemSlot provided. Will migrate 0.4.x enchants until 0.6.x
        /// </summary>
        /// <param name="itemStack"></param>
        /// <returns></returns>
        public Dictionary<string, int> GetEnchantments(ItemStack itemStack);
        /// <summary>
        /// Processes an Enchantment from the server. Returns false if it fails to run an Enchantment trigger.
        /// </summary>
        /// <param name="enchant"></param>
        /// <param name="slot"></param>
        /// <param name="damage"></param>
        /// <returns></returns>
        bool DoEnchantment(EnchantmentSource enchant, ItemSlot slot, ref object[] parameters);
        /// <summary>
        /// Returns a List of Latent Enchantments pending for the contained Itemstack or null if there are none.
        /// </summary>
        /// <param name="inSlot"></param>
        /// <returns></returns>
        List<string> GetLatentEnchants(ItemSlot inSlot, bool encrypt);
        /// <summary>
        /// Returns true if the given player can decrypt the enchant.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="recipe"></param>
        /// <returns></returns>
        bool CanReadEnchant(string player, EnchantingRecipe recipe);
        /// <summary>
        /// Returns true if the given player can decrypt the enchant. enchantName must be in the format of an AssetLocation.Name.ToShortString() (Ex: "domain:enchant-name")
        /// </summary>
        /// <param name="player"></param>
        /// <param name="enchantName"></param>
        /// <returns></returns>
        bool CanReadEnchant(string player, string enchantName);
        /// <summary>
        /// This exists as a temporary replacement for a removed helper function in ModJournal.
        /// </summary>
        /// <param name="playerUid"></param>
        /// <param name="code"></param>
        /// <param name="chapterId"></param>
        /// <returns></returns>
        bool DidDiscoverLore(string playerUid, string code, int chapterId);
        /// <summary>
        /// Returns True if we successfully wrote new LatentEnchants to the item, or False if not.
        /// </summary>
        /// <param name="inSlot"></param>
        /// <returns></returns>
        bool AssessItem(ItemSlot inSlot, ItemSlot rSlot);
        /// <summary>
        /// Call to assign a EnchantPotential attribute to an item. Returns 0 if it is not valid.
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        int AssessReagent(ItemStack stack);
        /// <summary>
        /// Returns a List of EnchantingRecipes that match the provided slots, or null if something went wrong.
        /// </summary>
        /// <param name="inSlot"></param>
        /// <param name="rSlot"></param>
        /// <returns></returns>
        List<EnchantingRecipe> GetValidEnchantingRecipes(ItemSlot inSlot, ItemSlot rSlot);
        /// <summary>
        /// Returns if the ItemStack is Enchantable or not.
        /// </summary>
        /// <param name="inSlot"></param>
        /// <returns></returns>
        bool IsEnchantable(ItemSlot inSlot);
        /// <summary>
        /// List of all loaded Enchanting Recipes
        /// </summary>
        /// <returns></returns>
        List<EnchantingRecipe> GetEnchantingRecipes();
        /// <summary>
        /// All Enchantments are processed and stored here. Must use RegisterEnchantmentClass to handle adding Enchantments.
        /// </summary>
        // Dictionary<string, Enchantment> EnchantmentRegistry();
        /// <summary>
        /// Register an Enchantment to the EnchantmentRegistry. All Enchantments must be registered here.
        /// </summary>
        /// <param name="enchantClass"></param>
        /// <param name="t"></param>
        void RegisterEnchantmentClass(string enchantClass, Type t);
    }
}