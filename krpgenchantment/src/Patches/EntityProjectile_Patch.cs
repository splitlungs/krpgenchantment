using CombatOverhaul.Implementations;
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
    public class EntityProjectile_impactOnEntity_Patch
    {
        // Remove damage from Healing enchanted projectile
        [HarmonyPatch(typeof(EntityProjectile), "impactOnEntity")]
        public static bool Prefix(EntityProjectile __instance, Entity entity)
        {
            if (__instance.Api.Side != EnumAppSide.Server || entity == null) return true;

            ICoreServerAPI sApi = __instance.Api as ICoreServerAPI;
            if (__instance.ProjectileStack?.Item?.Tool == EnumTool.Spear)
            {
                Dictionary<string, int> enchants = sApi.EnchantAccessor().GetActiveEnchantments(__instance.ProjectileStack);
                if (enchants == null || !enchants.ContainsKey("healing")) return true;

                // Item overrides Entity's Enchantment
                if (enchants["healing"] > 0)
                    __instance.Damage = 0;
            }
            else
            {
                // Get Bow & Enchants
                ItemStack weaponStack = __instance.FiredBy.WatchedAttributes.GetItemstack("pendingRangedEnchants", null);
                long timestamp = __instance.FiredBy.WatchedAttributes.GetLong("pendingRangedEnchantsTimer", 0);
                if (weaponStack == null || (entity.Api.World.ElapsedMilliseconds - timestamp) > 6000) return true;
                Dictionary<string, int> enchants = sApi.EnchantAccessor().GetActiveEnchantments(weaponStack);
                if (enchants == null || !enchants.ContainsKey("healing")) return true;

                // Item overrides Entity's Enchantment
                if (enchants["healing"] > 0)
                    __instance.Damage = 0;
            }
            return true;
        }
        // Trigger "OnAttack" enchants when an entity has been hit
        [HarmonyPatch(typeof(EntityProjectile), "impactOnEntity")]
        public static void Postfix(EntityProjectile __instance, Entity entity)
        {
            if (__instance.Api.Side != EnumAppSide.Server || entity == null) return;

            ICoreServerAPI sApi = __instance.Api as ICoreServerAPI;
            if (__instance.ProjectileStack?.Item?.Tool == EnumTool.Spear)
            {
                EnchantModifiers parameters = new EnchantModifiers();
                bool didEnchants = sApi.EnchantAccessor().TryEnchantments(__instance.ProjectileStack, "OnAttack", __instance, entity, ref parameters);
                if (!didEnchants)
                    entity.Api.Logger.Warning("[KRPGEnchantments] Failed to TryEnchantments on {0}!", __instance.ProjectileStack.GetName());
            }
            else
            {
                // Get Bow & Timer
                ItemStack weaponStack = __instance.FiredBy.WatchedAttributes.GetItemstack("pendingRangedEnchants", null);
                long timestamp = __instance.FiredBy.WatchedAttributes.GetLong("pendingRangedEnchantsTimer", 0);
                if (weaponStack == null || (entity.Api.World.ElapsedMilliseconds - timestamp) > 6000) return;

                EnchantModifiers parameters = new EnchantModifiers();
                bool didEnchants = sApi.EnchantAccessor().TryEnchantments(weaponStack, "OnAttack", __instance, entity, ref parameters);
                if (!didEnchants)
                    entity.Api.Logger.Warning("[KRPGEnchantments] Failed to TryEnchantments on {0}!", weaponStack.GetName());

                __instance.FiredBy.WatchedAttributes.SetItemstack("pendingRangedEnchants", null);
                __instance.FiredBy.WatchedAttributes.SetLong("pendingRangedEnchantsTimer", 0);
            }
        }
    }
    [HarmonyPatch]
    public class EntityProjectile_TryAttackEntity_Patch
    {
        // Trigger OnAttack for non-entities. It's pretty greasey right now, triggering on itself.
        // TODO: Make proper BlockEntity or BlockPos triggers
        [HarmonyPatch(typeof(EntityProjectile), "TryAttackEntity")]
        public void Postfix(EntityProjectile __instance, double impactSpeed, ref bool __result)
        {
            if (!(__instance.Api is ICoreServerAPI sapi)) return; 
            if (__result == true) return;
            // Hit someTHING
            if (__instance.ProjectileStack?.Item?.Tool == EnumTool.Spear)
            {
                EnchantModifiers parameters = new EnchantModifiers();
                bool didEnchants = sapi.EnchantAccessor().TryEnchantments(__instance.ProjectileStack, "OnAttack", __instance, __instance, ref parameters);
                if (!didEnchants)
                    sapi.Logger.Warning("[KRPGEnchantments] Failed to TryEnchantments on {0}!", __instance.ProjectileStack.GetName());
            }
            else
            {
                // Get Bow & Timer
                ItemStack weaponStack = __instance.FiredBy.WatchedAttributes.GetItemstack("pendingRangedEnchants", null);
                long timestamp = __instance.FiredBy.WatchedAttributes.GetLong("pendingRangedEnchantsTimer", 0);
                if (weaponStack == null || (sapi.World.ElapsedMilliseconds - timestamp) > 6000) return;

                EnchantModifiers parameters = new EnchantModifiers();
                bool didEnchants = sapi.EnchantAccessor().TryEnchantments(weaponStack, "OnAttack", __instance, __instance, ref parameters);
                if (!didEnchants)
                    sapi.Logger.Warning("[KRPGEnchantments] Failed to TryEnchantments on {0}!", weaponStack.GetName());

                __instance.FiredBy.WatchedAttributes.SetItemstack("pendingRangedEnchants", null);
                __instance.FiredBy.WatchedAttributes.SetLong("pendingRangedEnchantsTimer", 0);
            }
        }
    }
}