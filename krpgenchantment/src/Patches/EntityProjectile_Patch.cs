using HarmonyLib;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace KRPGLib.Enchantment
{
    [HarmonyPatch]
    public class EntityProjectile_Patch
    {       
        [HarmonyPatch(typeof(EntityProjectile), "impactOnEntity")]
        public static bool Prefix(EntityProjectile __instance, Entity entity)
        {
            // Get Enchantments
            ITreeAttribute tree = __instance.ProjectileStack.Attributes.GetOrAddTreeAttribute("enchantments");
            Dictionary<string, int> enchants = new Dictionary<string, int>();
            // Item overrides Entity's Enchantment
            int ePower = tree.GetInt(EnumEnchantments.healing.ToString(), 0);
            if (ePower > 0 || __instance.WatchedAttributes.GetInt(EnumEnchantments.healing.ToString(), 0) > 0)
                __instance.Damage = 0;

            return true;
        }

        [HarmonyPatch(typeof(EntityProjectile), "impactOnEntity")]
        public static void Postfix(EntityProjectile __instance, Entity entity)
        {
            EnchantModifiers parameters = new EnchantModifiers();
            entity.Api.EnchantAccessor().TryEnchantments(__instance.ProjectileStack, "OnAttack", __instance, entity, ref parameters);
        }
    }
}