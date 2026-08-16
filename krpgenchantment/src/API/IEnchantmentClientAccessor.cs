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
    public interface IEnchantmentClientAccessor
    {
        #region Registration
        /// <summary>
        /// Register an Enchantment to the EnchantmentRegistry. All Enchantments must be registered here. Returns false if it fails to register.
        /// </summary>
        /// <param name="enchantClass"></param>
        /// <param name="props"></param>
        /// <param name="t"></param>
        internal bool RegisterEnchantmentClass(string enchantClass, EnchantmentProperties props, Type t);
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
        #endregion
        #region GUI
        /// <summary>
        /// Returns a request font file from ModData/krpgenchantment/fonts, downloads it if possible, or null if it doesn't exist
        /// </summary>
        /// <param name="fName"></param>
        /// <returns></returns>
        SKTypeface LoadCustomFont(string fName);
        #endregion
    }
}