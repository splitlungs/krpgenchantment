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
        #region Enchanting
        /// <summary>
        /// Removes the provided enchantment from an item. Returns false if it fails for any reason.
        /// </summary>
        /// <param name="eName"></param>
        /// <param name="inSlot"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        bool RemoveEnchantFromItem(string eName, ItemSlot inSlot, Entity entity);
        /// <summary>
        /// Removes the provided enchantment from an item. Returns false if it fails for any reason.
        /// </summary>
        /// <param name="inSlot"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        bool RemoveAllEnchantsFromItem(ItemSlot inSlot, Entity entity);
        /// <summary>
        /// Resets all Latent Enchant attributes to null on the ItemStack in the slot provided.
        /// </summary>
        /// <param name="inSlot"></param>
        /// <returns></returns>
        bool ResetLatentEnchantsOnItem(ItemSlot inSlot);
        #endregion
        #region Actions
        /// <summary>
        /// Bulk convenience processor for Enchantments. Returns false if it fails to run an Enchantment trigger.
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="trigger"></param>
        /// <param name="byEntity"></param>
        /// <param name="targetEntity"></param>
        /// <returns></returns>
        bool TryEnchantments(ItemSlot slot, string trigger, Entity byEntity, Entity targetEntity);
        /// <summary>
        /// Bulk convenience processor for Enchantments. Returns false if it fails to run an Enchantment trigger.
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="trigger"></param>
        /// <param name="byEntity"></param>
        /// <param name="targetEntity"></param>
        /// <param name="enchants"></param>
        /// <returns></returns>
        bool TryEnchantments(ItemSlot slot, string trigger, Entity byEntity, Entity targetEntity, Dictionary<string, int> enchants);
        /// <summary>
        /// Bulk convenience processor for Enchantments. Returns false if it fails to run an Enchantment trigger.
        /// </summary>
        /// <param name="stack"></param>
        /// <param name="trigger"></param>
        /// <param name="byEntity"></param>
        /// <param name="targetEntity"></param>
        /// <returns></returns>
        bool TryEnchantments(ItemStack stack, string trigger, Entity byEntity, Entity targetEntity);
        /// <summary>
        /// Bulk convenience processor for Enchantments. Returns false if it fails to run an Enchantment trigger.
        /// </summary>
        /// <param name="stack"></param>
        /// <param name="trigger"></param>
        /// <param name="byEntity"></param>
        /// <param name="targetEntity"></param>
        /// <param name="enchants"></param>
        /// <returns></returns>
        bool TryEnchantments(ItemStack stack, string trigger, Entity byEntity, Entity targetEntity, Dictionary<string, int> enchants);
        /// <summary>
        /// Bulk convenience processor for Enchantments. Returns false if it fails to run an Enchantment trigger.
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="trigger"></param>
        /// <param name="byEntity"></param>
        /// <param name="targetEntity"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        bool TryEnchantments(ItemSlot slot, string trigger, Entity byEntity, Entity targetEntity, ref EnchantModifiers parameters);
        /// <summary>
        /// Bulk convenience processor for Enchantments. Returns false if it fails to run an Enchantment trigger.
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="trigger"></param>
        /// <param name="byEntity"></param>
        /// <param name="targetEntity"></param>
        /// <param name="parameters"></param>
        /// <param name="enchants"></param>
        /// <returns></returns>
        bool TryEnchantments(ItemSlot slot, string trigger, Entity byEntity, Entity targetEntity, Dictionary<string, int> enchants, ref EnchantModifiers parameters);
        /// <summary>
        /// Bulk convenience processor for Enchantments. Returns false if it fails to run an Enchantment trigger.
        /// </summary>
        /// <param name="stack"></param>
        /// <param name="trigger"></param>
        /// <param name="byEntity"></param>
        /// <param name="targetEntity"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        bool TryEnchantments(ItemStack stack, string trigger, Entity byEntity, Entity targetEntity, ref EnchantModifiers parameters);
        /// <summary>
        /// Bulk convenience processor for Enchantments. Returns false if it fails to run an Enchantment trigger.
        /// </summary>
        /// <param name="stack"></param>
        /// <param name="trigger"></param>
        /// <param name="byEntity"></param>
        /// <param name="targetEntity"></param>
        /// <param name="parameters"></param>
        /// <param name="enchants"></param>
        /// <returns></returns>
        bool TryEnchantments(ItemStack stack, string trigger, Entity byEntity, Entity targetEntity, Dictionary<string, int> enchants, ref EnchantModifiers parameters);
        /// <summary>
        /// Generic convenience processor for Enchantments. Requires a pre-formed EnchantmentSource Returns false if it fails to run an Enchantment trigger.
        /// </summary>
        /// <param name="enchant"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        bool TryEnchantment(EnchantmentSource enchant, ref EnchantModifiers parameters);
        #endregion
        #region Getters
        /// <summary>
        /// Returns the Enchantment Interface from the EnchantmentRegistry. Returns null if not found.
        /// </summary>
        /// <param name="enchantCode"></param>
        /// <returns></returns>
        IEnchantment GetEnchantment(string enchantCode);
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
        ItemStack EnchantItem(ICoreServerAPI api, ItemSlot inSlot, ItemSlot rSlot, Dictionary<string, int> enchantments);
        /// <summary>
        /// Attempts to get base EnumTool type from an item, or interperited ID for a non-tool, then converts to string. This should match your ValidToolTypes in the Enchantment. Returns null if none can be found.
        /// </summary>
        /// <param name="stack"></param>
        /// <returns></returns>
        string GetToolType(ItemStack stack);
        #endregion
        #region Assessments
        /// <summary>
        /// Returns all Enchantments in the ItemStack's Attributes or null if none are found.
        /// </summary>
        /// <param name="itemStack"></param>
        /// <returns></returns>
        Dictionary<string, int> GetActiveEnchantments(ItemStack itemStack);
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
        /// Call to assign a reagent charge attribute to an item. Returns the value assigned or 0 if it is not valid.
        /// </summary>
        /// <param name="stack"></param>
        /// <returns></returns>
        int SetReagentCharge(ref ItemStack stack, int numGears);
        /// <summary>
        /// Returns all EnchantmentRegistry keys with an Enchantment containing the provided category. Returns null if none are found.
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        List<string> GetEnchantmentsInCategory(string category);
        /// <summary>
        /// Returns a list of all Enchantment Categories among all registered Enchantments.
        /// </summary>
        /// <returns></returns>
        List<string> GetEnchantmentCategories();
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
        /// <param name="api"></param>
        /// <returns></returns>
        bool CanReadEnchant(string player, string enchantName, ICoreServerAPI api);
        #endregion
    }
}