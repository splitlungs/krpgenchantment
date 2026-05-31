using System;
using System.Collections.Generic;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace KRPGLib.Enchantment
{
    [HarmonyPatch]
    public class EntityPlayer_Patch
    {
        [HarmonyReversePatch]
        [HarmonyPostfix]
        [HarmonyPatch(typeof(EntityPlayer), nameof(EntityPlayer.LightHsv), MethodType.Getter)]
        public static void LightHsv_Postfix(EntityPlayer __instance, ref byte[] __result)
        {
            byte[] b = __instance.WatchedAttributes?.GetTreeAttribute("enchantments")?.GetBytes("lightHsv", null);
            if (b != null)
                __result = ColorUtil.MergeLightHSV(__result, b);
        }
    }
}