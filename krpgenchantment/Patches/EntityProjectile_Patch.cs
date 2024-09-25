using HarmonyLib;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace KRPGLib.Enchantment
{
    [HarmonyPatch]
    public class EntityProjectile_Patch
    {
        // For Returning, probably
        // [HarmonyPatch(typeof(EntityProjectile), "OnGameTick")]
        // public static void Postfix(EntityProjectile __instance, float dt)
        // {
        // 
        // }

        [HarmonyPatch(typeof(EntityProjectile), "impactOnEntity")]
        public static bool Prefix(EntityProjectile __instance, Entity entity)
        {
            if (__instance.ProjectileStack.Attributes.GetInt(EnumEnchantments.healing.ToString(), 0) > 0 
                || __instance.WatchedAttributes.GetInt(EnumEnchantments.healing.ToString(), 0) > 0)
                __instance.Damage = 0;

            return true;
        }

        [HarmonyPatch(typeof(EntityProjectile), "impactOnEntity")]
        public static void Postfix(EntityProjectile __instance, Entity entity)
        {
            // Is it a valid target?
            var eeb = entity.GetBehavior<EnchantmentEntityBehavior>();
            if (eeb != null)
            {
                // Get Enchantments
                Dictionary<string, int> enchants = new Dictionary<string, int>();
                foreach (var val in Enum.GetValues(typeof(EnumEnchantments)))
                {
                    // Item overrides Entity's Enchantment
                    int ePower = __instance.ProjectileStack.Attributes.GetInt(val.ToString(), 0);
                    if (ePower > 0)
                    {
                        enchants.Add(val.ToString(), ePower);
                    }
                    else
                    {
                        ePower = __instance.WatchedAttributes.GetInt(val.ToString(), 0);
                        if (ePower > 0) { enchants.Add(val.ToString(), ePower); }
                    }
                }
                // Process the Enchantments
                eeb.TryEnchantments(__instance.FiredBy as EntityAgent, __instance.ProjectileStack, enchants);
            }
        }
    }
}