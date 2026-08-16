using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using HarmonyLib;
using KRPGLib.Enchantment.API;

namespace KRPGLib.Enchantment
{
    [HarmonyPatch]
    public class CollectibleBehaviorWearable_Patch
    {
        [HarmonyReversePatch]
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CollectibleBehaviorWearable), nameof(CollectibleBehaviorWearable.GetMaxWarmth))]
        public static void GetMaxWarmth_Patch(CollectibleBehaviorWearable __instance, ItemSlot inslot, ref float __result)
        {
            if (inslot.Itemstack?.Attributes?.GetTreeAttribute("enchantments")?.HasAttribute("warmth") != true) return;
            float val = inslot.Itemstack.Attributes.GetTreeAttribute("enchantments").GetFloat("warmth", 0f);
            float val2 = __result;
            __result = val + val2;
        }
    }
    [HarmonyPatch]
    public class CollectibleBehaviorWearable_Patch2
    {
        [HarmonyReversePatch]
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CollectibleBehaviorWearable), nameof(CollectibleBehaviorWearable.GetWarmth))]
        public static void GetWarmth_Patch(CollectibleBehaviorWearable __instance, ItemSlot inslot, ref float __result, ref ICoreAPI ___api)
        {
            if (!(___api is ICoreClientAPI capi)) return;
            IEnchantment ench = capi.EnchantAccessor().GetEnchantment("warmth");
            bool ignoreCond = ench.Modifiers.GetBool("IgnoreCondition");
            // Add warmth only if not applied for some reason
            if (inslot.Itemstack?.Attributes?.GetTreeAttribute("enchantments")?.HasAttribute("warmth") != true) return;
            float val = inslot.Itemstack.Attributes.GetTreeAttribute("enchantments").GetFloat("warmth", 0f);
            if (ignoreCond == true && __result < val)
            {
                __result = val;
            }
        }
    }
}