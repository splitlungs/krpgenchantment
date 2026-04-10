// Not in use - Change aim animation for vanilla VS for QuickDraw
/*
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace KRPGLib.Enchantment
{
    public static class BowAnimationReplacement
    {
        public static void CustomStartAnimation(EntityAgent entity, string animation)
        {
            entity.Api.Logger.Event("[KRPGEnchantment] Replaced animation call");
            // Your custom logic here
            entity.AnimManager.StartAnimation(animation);
        }
    }
    [HarmonyPatch(typeof(ItemBow), "OnHeldInteractStart")]
    public static class ItemBow_OnHeldInteractStart_Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var startAnimMethod = AccessTools.Method(
                typeof(AnimationManager),
                "StartAnimation",
                new[] { typeof(string) }
            );
            var replacementMethod = AccessTools.Method(
                typeof(BowAnimationReplacement),
                nameof(BowAnimationReplacement.CustomStartAnimation)
            );
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].Calls(startAnimMethod))
                {
                    // Remove original StartAnimation call
                    codes.RemoveAt(i);
                    // Insert replacement call
                    codes.Insert(i, new CodeInstruction(OpCodes.Call, replacementMethod));
                    break;
                }
            }
            return codes;
        }
    }
}
*/