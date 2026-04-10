// IGNORE ME
/*
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Vintagestory.GameContent;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using System;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;


namespace KRPGLib.Enchantment
{
    [HarmonyPatch]
    public static class ItemBow_Patch2
    {
        [HarmonyPatch(typeof(ItemBow), "OnHeldInteractStop")]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            MethodInfo setFromMethod = AccessTools.Method(
                typeof(EntityPos),
                nameof(EntityPos.SetFrom),
                new[] { typeof(EntityPos) }
            );

            FieldInfo worldField = AccessTools.Field(
                typeof(Entity),
                nameof(Entity.World)
            );

            MethodInfo injectMethod = AccessTools.Method(
                typeof(ItemBow_Patch2),
                nameof(InjectPendingEnchants)
            );

            matcher.SearchForward(ci =>
                ci.opcode == OpCodes.Callvirt &&
                ci.operand is MethodInfo mi &&
                mi == setFromMethod
            );

            if (!matcher.IsValid)
                throw new Exception("SetFrom not found");

            matcher.Advance(1);

            if (!matcher.IsValid || matcher.Opcode != OpCodes.Pop)
                throw new Exception("Expected pop after SetFrom");

            matcher.SearchForward(ci =>
                ci.opcode == OpCodes.Stfld &&
                ci.operand is FieldInfo fi &&
                fi == worldField
            );

            if (!matcher.IsValid)
                throw new Exception("stfld Entity.World not found");

            // Insert before stfld
            matcher.Insert(
                new CodeInstruction(OpCodes.Ldarg_2),   // slot
                new CodeInstruction(OpCodes.Ldloc_S, 5), // entity (from your IL dump)
                new CodeInstruction(OpCodes.Call, injectMethod)
            );

            return matcher.InstructionEnumeration();
        }

        public static void InjectPendingEnchants(ItemSlot slot, Entity entity)
        {
            entity.Api.Logger.Event("[KRPGEnchantment] ItemBow method injection is working!");
            if (slot?.Itemstack?.Attributes == null)
                return;

            string active = slot.Itemstack.Attributes
                .GetTreeAttribute("enchantment")
                ?.GetString("active");

            if (active != null)
            {
                entity.WatchedAttributes.SetString("pendingRangedEnchants", active);
            }
        }
    }
}
*/