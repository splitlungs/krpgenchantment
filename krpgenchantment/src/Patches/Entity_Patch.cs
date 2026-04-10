using System;
using HarmonyLib;
using Vintagestory.API.Common.Entities;

namespace KRPGLib.Enchantment
{
    [HarmonyPatch]
    public class Entity_OnEntityLoaded_Patch
    {
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Entity), nameof(Entity.OnEntityLoaded))]
        public static bool Prefix(Entity __instance)
        {
            // Setup Enchantment Entity Behaviors on ALL Entities
            bool foundEB = false;
            if (__instance.GetBehavior<EnchantmentEntityBehavior>() != null)
                foundEB = true;
            if (!foundEB)
            {
                if (EnchantingConfigLoader.Config?.Debug == true)
                    __instance.Api.Logger.Event("[KRPGEnchantment] Adding an EnchantmentEntityBehavior to {0} on loaded.", __instance.GetName());
                EnchantmentEntityBehavior eb = new EnchantmentEntityBehavior(__instance);
                __instance.AddBehavior(eb);
            }
            return true;
        }
    }
    [HarmonyPatch]
    public class Entity_OnEntitySpawn_Patch
    {
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Entity), nameof(Entity.OnEntitySpawn))]
        public static bool Prefix(Entity __instance)
        {
            // Setup Enchantment Entity Behaviors on ALL Entities
            bool foundEB = false;
            if (__instance.GetBehavior<EnchantmentEntityBehavior>() != null)
                foundEB = true;
            if (!foundEB)
            {
                if (EnchantingConfigLoader.Config?.Debug == true)
                    __instance.Api.Logger.Event("[KRPGEnchantment] Adding an EnchantmentEntityBehavior to {0} on spawn.", __instance.GetName());
                EnchantmentEntityBehavior eb = new EnchantmentEntityBehavior(__instance);
                __instance.AddBehavior(eb);
            }
            return true;
        }
    }
}