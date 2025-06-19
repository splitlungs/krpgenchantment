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
using Newtonsoft.Json.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace KRPGLib.Enchantment.API
{
    /// <summary>
    /// Primary controller for KRPG Enchantments.
    /// </summary>
    public interface IEnchantmentAccessor
    {
        #region Getters
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
        #endregion
    }
}