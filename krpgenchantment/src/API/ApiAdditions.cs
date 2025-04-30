using Cairo;
using Cairo.Freetype;
using KRPGLib.Enchantment;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using static System.Net.Mime.MediaTypeNames;
using KRPGLib.Enchantment.API;

namespace Vintagestory.GameContent
{
    public static class ApiAdditions
    {
        /// <summary>
        /// Interface for KRPG Enchantment mod system.
        /// </summary>
        /// <param name="api"></param>
        /// <returns></returns>
        public static IEnchantAccessor EnchantAccessor(this ICoreAPI api)
        {
            { return api.ModLoader.GetModSystem<EnchantAccessor>(); }
        }
    }
}