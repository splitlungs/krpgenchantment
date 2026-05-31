using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.API.Datastructures;
using static System.Net.Mime.MediaTypeNames;
using System.Reflection;
using Vintagestory.GameContent.Mechanics;

namespace KRPGLib.Enchantment
{
    [HarmonyPatch]
    public class CollectibleObject_Patch
    {
        [HarmonyReversePatch]
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CollectibleObject), nameof(CollectibleObject.GetLightHsv))]
        public static void GetLightHsv_Postfix(CollectibleObject __instance, IBlockAccessor blockAccessor, BlockPos pos, ItemStack stack, ref byte[] __result)
        {
            if (pos != null) return;
            if (stack != null)
            {
                byte[] b = stack?.Attributes?.GetBytes("lightHsv", null);
                if (b != null) __result = b;
            }
        }
    }
}
