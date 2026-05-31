using Vintagestory.API.Common;
using Vintagestory.GameContent;
using HarmonyLib;

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
}