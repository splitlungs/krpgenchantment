using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace KRPGLib.Enchantment
{
    [HarmonyPatch]
    public static class EntityProjectileBase_impactOnEntity_Patch
    {
        // Remove damage from Healing enchanted projectile
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(EntityProjectileBase), "ImpactOnEntity")]
        public static bool Prefix(EntityProjectileBase __instance, Entity target)
        {
            // entity.Api.Logger.Event("[KRPGEnchantment] Firing EntityProjectile.impactOnEntity prefix.");
            Entity byEntity = __instance.FiredBy;
            if (!(byEntity.Api is ICoreServerAPI sapi) || target == null) return true;
            if (__instance.ProjectileStack?.Item?.Tool == EnumTool.Spear)
            {
                Dictionary<string, int> enchants = sapi.EnchantAccessor().GetActiveEnchantments(__instance.ProjectileStack);
                if (enchants == null || !enchants.ContainsKey("healing")) return true;
                // Item overrides Entity's Enchantment
                if (enchants["healing"] > 0)
                    __instance.Damage = 0;
            }
            else
            {
                // TODO: Make a better way to detect "healing" typed enchants
                // Get Bow & Enchants
                // ItemStack weaponStack = __instance.FiredBy.WatchedAttributes.GetItemstack("pendingRangedEnchants", null);
                // weaponStack?.ResolveBlockOrItem(sapi.World);
                // Dictionary<string, int> enchants = sapi.EnchantAccessor().GetActiveEnchantments(weaponStack);
                // if (!enchants.ContainsKey("healing")) return true;
                string activeEnchants = byEntity.WatchedAttributes.GetString("pendingRangedEnchants", null);
                if (activeEnchants?.CaseInsensitiveContains("healing") != true) return true;
                // Item overrides Entity's Enchantment
                __instance.Damage = 0;
            }
            return true;
        }
        // Trigger "OnAttackStop" enchants when an entity has been hit
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(EntityProjectileBase), "ImpactOnEntity")]
        public static void Postfix(EntityProjectileBase __instance, Entity target)
        {
            // __instance.Api.Logger.Event("[KRPGEnchantment] Firing EntityProjectile.impactOnEntity postfix");
            Entity byEntity = __instance.FiredBy;
            if (!(byEntity.Api is ICoreServerAPI sapi)) return;
            if (__instance.ProjectileStack?.Item?.Tool == EnumTool.Spear)
            {
                Dictionary<string, int> enchants = sapi.EnchantAccessor().GetActiveEnchantments(__instance.ProjectileStack);
                if (enchants == null) return;

                EnchantModifiers parameters = new EnchantModifiers();
                bool didEnchants = sapi.EnchantAccessor().TryEnchantments(__instance.ProjectileStack, "OnAttacked", byEntity, target, enchants, ref parameters);
                if (!didEnchants)
                    sapi.Logger.Warning("[KRPGEnchantments] Failed to TryEnchantments on {0}!", __instance.ProjectileStack.GetName());
            }
            else
            {
                // Get Bow & Timer
                // ItemStack weaponStack = __instance.FiredBy.WatchedAttributes.GetItemstack("pendingRangedEnchants", null);
                // weaponStack.ResolveBlockOrItem(sapi.World);
                // if (weaponStack == null || (byEntity.World.ElapsedMilliseconds - timestamp) > 6000) 
                // {
                //     sapi.Logger.Event("[KRPGEnchantment] Enchanted arrow failed to retrieve bow stack or was stale.");
                //     return;
                // }
                string activeEnchants = byEntity.WatchedAttributes.GetString("pendingRangedEnchants", null);
                long timestamp = byEntity.WatchedAttributes.GetLong("pendingRangedEnchantsTimer", 0);
                long timediff = sapi.World.ElapsedMilliseconds - timestamp;
                if (activeEnchants == null || timediff > 6000) return;
                Dictionary<string, int> enchants = new Dictionary<string, int>();
                string[] activeString = activeEnchants.Split(";", StringSplitOptions.RemoveEmptyEntries);
                foreach (string s in activeString)
                {
                    string[] ep = s.Split(":", StringSplitOptions.RemoveEmptyEntries);
                    string e = ep[0].ToString();
                    int i = Convert.ToInt32(ep?[1]);
                    if (e == null || i <= 0) continue;
                    enchants.Add(e, i);
                }
                if (enchants.Count < 1)
                {
                    sapi.Logger.Error("[KRPGEnchantment] Enchanted arrow failed to parse any enchantments out of an active string.");
                    return;
                }
                EnchantModifiers parameters = new EnchantModifiers();
                // bool didEnchants = sapi.EnchantAccessor().TryEnchantments(weaponStack, "OnAttackStop", __instance, entity, ref parameters);
                bool didEnchants = sapi.EnchantAccessor().TryEnchantments(__instance.ProjectileStack, "OnAttacked", byEntity, target, enchants, ref parameters);
                if (!didEnchants)
                    sapi.Logger.Warning("[KRPGEnchantments] Failed to TryEnchantments on {0}!", __instance.GetName());
                // __instance.FiredBy.WatchedAttributes.SetItemstack("pendingRangedEnchants", null);
                __instance.FiredBy.WatchedAttributes.SetString("pendingRangedEnchants", null);
                __instance.FiredBy.WatchedAttributes.SetLong("pendingRangedEnchantsTimer", 0);
            }
        }
    }
    // Disabled for now.
    // TODO: Setup triggers on BlockEntity impact.
    /*
    [HarmonyPatch]
    public class EntityProjectile_TryAttackEntity_Patch
    {
        // Trigger OnAttack for non-entities. It's pretty greasey right now, triggering on itself.
        // TODO: Make proper BlockEntity or BlockPos triggers
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(EntityProjectile), "TryAttackEntity")]
        public static void Postfix(EntityProjectile __instance, double impactSpeed, ref bool __result)
        {
            __instance.Api.Logger.Event("[KRPGEnchantment] Firing EntityProjectile.TryAttackEntity postfix");
            Entity byEntity = __instance.FiredBy;
            if (!(byEntity.Api is ICoreServerAPI sapi)) return;
            if (__result == true) return;
            // Hit someTHING
            BlockEntity be = __instance.World.BlockAccessor.GetBlockEntity(__instance.Pos.AsBlockPos);
            if (be == null) return;
            __instance.Api.Logger.Event("[KRPGEnchantment] Collided with {0}.", be.Block?.BlockId);
            if (__instance.ProjectileStack?.Item?.Tool == EnumTool.Spear)
            {
                EnchantModifiers parameters = new EnchantModifiers();
                bool didEnchants = sapi.EnchantAccessor().TryEnchantments(__instance.ProjectileStack, "OnAttackStop", __instance, __instance, ref parameters);
                if (!didEnchants)
                    sapi.Logger.Warning("[KRPGEnchantments] Failed to TryEnchantments on {0}!", __instance.ProjectileStack.GetName());
            }
            else
            {
                // Get Bow & Timer
                // ItemStack weaponStack = __instance.FiredBy.WatchedAttributes.GetItemstack("pendingRangedEnchants", null);
                // long timestamp = __instance.FiredBy.WatchedAttributes.GetLong("pendingRangedEnchantsTimer", 0);
                // if (weaponStack == null || (sapi.World.ElapsedMilliseconds - timestamp) > 6000) return;
                string activeEnchants = byEntity.WatchedAttributes.GetString("pendingRangedEnchants", null);
                long timestamp = byEntity.WatchedAttributes.GetLong("pendingRangedEnchantsTimer", 0);
                long timediff = sapi.World.ElapsedMilliseconds - timestamp;
                if (activeEnchants == null || timediff > 6000) return;
                Dictionary<string, int> enchants = new Dictionary<string, int>();
                string[] activeString = activeEnchants.Split(";", StringSplitOptions.RemoveEmptyEntries);
                foreach (string s in activeString)
                {
                    string[] ep = s.Split(":", StringSplitOptions.RemoveEmptyEntries);
                    string e = ep[0].ToString();
                    int i = Convert.ToInt32(ep?[1]);
                    if (e == null || i <= 0) continue;
                    enchants.Add(e, i);
                }
                if (enchants.Count < 1)
                {
                    sapi.Logger.Error("[KRPGEnchantment] Enchanted arrow failed to parse any enchantments out of an active string.");
                    return;
                }
                EnchantModifiers parameters = new EnchantModifiers();
                // bool didEnchants = sapi.EnchantAccessor().TryEnchantments(__instance.ProjectileStack, "OnAttackStop", __instance, __instance, ref parameters);
                bool didEnchants = sapi.EnchantAccessor().TryEnchantments(__instance.ProjectileStack, "OnAttackStop", byEntity, __instance, enchants, ref parameters);
                if (!didEnchants)
                    sapi.Logger.Warning("[KRPGEnchantments] Failed to TryEnchantments on {0}!", __instance.GetName());
                // __instance.FiredBy.WatchedAttributes.SetItemstack("pendingRangedEnchants", null);
                __instance.FiredBy.WatchedAttributes.SetString("pendingRangedEnchants", null);
                __instance.FiredBy.WatchedAttributes.SetLong("pendingRangedEnchantsTimer", 0);
            }
        }
    }
    */
    /*
    [HarmonyPatch]
    public class EntityProjectile_IsColliding_Patch
    {
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(EntityProjectile), "IsColliding")]
        public static bool IsColliding(EntityProjectile __instance, EntityPos pos, double impactSpeed, ref long __msCollide, ref bool __beforeCollided)
        {
            __instance.Api.Logger.Event("[KRPGEnchantment] Firing EntityProjectile.IsColliding prefix");
            Entity byEntity = __instance.FiredBy;
            if (!(byEntity.Api is ICoreServerAPI sapi)) return true;

            pos.Motion.Set(0.0, 0.0, 0.0);
            if (__beforeCollided || !(__instance.World is IServerWorldAccessor) || __instance.World.ElapsedMilliseconds <= __msCollide + 500)
            {
                return true;
            }
            
            BlockEntity be = __instance.World.BlockAccessor.GetBlockEntity(pos.AsBlockPos);
            __instance.Api.Logger.Event("[KRPGEnchantment] Collided with {0}.", be?.Block?.BlockId);

            if (be != null && impactSpeed >= 0.07)
            {

                // Hit someTHING
                if (__instance.ProjectileStack?.Item?.Tool == EnumTool.Spear)
                {
                    EnchantModifiers parameters = new EnchantModifiers();
                    bool didEnchants = sapi.EnchantAccessor().TryEnchantments(__instance.ProjectileStack, "OnAttackStop", __instance, __instance, ref parameters);
                    if (!didEnchants)
                        sapi.Logger.Warning("[KRPGEnchantments] Failed to TryEnchantments on {0}!", __instance.ProjectileStack.GetName());
                }
                else
                {
                    // Get Bow & Timer
                    // ItemStack weaponStack = __instance.FiredBy.WatchedAttributes.GetItemstack("pendingRangedEnchants", null);
                    // long timestamp = __instance.FiredBy.WatchedAttributes.GetLong("pendingRangedEnchantsTimer", 0);
                    // if (weaponStack == null || (sapi.World.ElapsedMilliseconds - timestamp) > 6000) return;
                    string activeEnchants = byEntity.WatchedAttributes.GetString("pendingRangedEnchants", null);
                    long timestamp = byEntity.WatchedAttributes.GetLong("pendingRangedEnchantsTimer", 0);
                    long timediff = sapi.World.ElapsedMilliseconds - timestamp;
                    if (activeEnchants == null || timediff > 6000) return true;
                    Dictionary<string, int> enchants = new Dictionary<string, int>();
                    string[] activeString = activeEnchants.Split(";", StringSplitOptions.RemoveEmptyEntries);
                    foreach (string s in activeString)
                    {
                        string[] ep = s.Split(":", StringSplitOptions.RemoveEmptyEntries);
                        string e = ep[0].ToString();
                        int i = Convert.ToInt32(ep?[1]);
                        if (e == null || i <= 0) continue;
                        enchants.Add(e, i);
                    }
                    if (enchants.Count < 1)
                    {
                        sapi.Logger.Error("[KRPGEnchantment] Enchanted arrow failed to parse any enchantments out of an active string.");
                        return true;
                    }
                    EnchantModifiers parameters = new EnchantModifiers();
                    // bool didEnchants = sapi.EnchantAccessor().TryEnchantments(__instance.ProjectileStack, "OnAttackStop", __instance, __instance, ref parameters);
                    bool didEnchants = sapi.EnchantAccessor().TryEnchantments(__instance.ProjectileStack, "OnAttackStop", byEntity, __instance, enchants, ref parameters);
                    if (!didEnchants)
                        sapi.Logger.Warning("[KRPGEnchantments] Failed to TryEnchantments on {0}!", __instance.GetName());
                    // __instance.FiredBy.WatchedAttributes.SetItemstack("pendingRangedEnchants", null);
                    __instance.FiredBy.WatchedAttributes.SetString("pendingRangedEnchants", null);
                    __instance.FiredBy.WatchedAttributes.SetLong("pendingRangedEnchantsTimer", 0);
                }
            }
            return true;
        }
    }
    */
}