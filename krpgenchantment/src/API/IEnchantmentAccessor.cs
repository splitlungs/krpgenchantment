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
    public interface IEnchantmentAccessor
    {
        #region Assessments
        /// <summary>
        /// Returns the Enchantment Interface from the EnchantmentRegistry.
        /// </summary>
        /// <param name="enchantCode"></param>
        /// <returns></returns>
        IEnchantment GetEnchantment(string enchantCode);
        /// <summary>
        /// Returns the number of Enchantments in the EnchantmentRegistry that match the provided category.
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        int GetEnchantCategoryCount(string category);
        /// <summary>
        /// Returns all EnchantmentRegistry keys with an Enchantment containing the provided category. Returns null if none are found.
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        List<string> GetEnchantmentsInCategory(string category);
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
        bool CanReadEnchant(string player, EnchantingRecipe recipe);
        /// <summary>
        /// Returns true if the given player can decrypt the enchant. enchantName must be in the format of an AssetLocation.Name.ToShortString() (Ex: "domain:enchant-name")
        /// </summary>
        /// <param name="player"></param>
        /// <param name="enchantName"></param>
        /// <returns></returns>
        bool CanReadEnchant(string player, string enchantName);
        #endregion
        #region Actions
        /// <summary>
        /// Bulk convenience processor for Enchantments. Returns false if it fails to run an Enchantment trigger.
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="trigger"></param>
        /// <param name="byEntity"></param>
        /// <param name="targetEntity"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        bool TryEnchantments(ItemSlot slot, string trigger, Entity byEntity, Entity targetEntity, ref Dictionary<string, object> parameters);
        /// <summary>
        /// Bulk convenience processor for Enchantments. Returns false if it fails to run an Enchantment trigger.
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="trigger"></param>
        /// <param name="byEntity"></param>
        /// <param name="targetEntity"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        bool TryEnchantments(ItemStack stack, string trigger, Entity byEntity, Entity targetEntity, ref Dictionary<string, object> parameters);
        /// <summary>
        /// Generic convenience processor for Enchantments. Requires a pre-formed EnchantmentSource Returns false if it fails to run an Enchantment trigger.
        /// </summary>
        /// <param name="enchant"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        bool TryEnchantment(EnchantmentSource enchant, ref Dictionary<string, object> parameters);
        #endregion
    }
}