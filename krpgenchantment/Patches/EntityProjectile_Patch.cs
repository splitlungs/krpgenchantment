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
        // For Returning, probably
        // [HarmonyPatch(typeof(EntityProjectile), "OnGameTick")]
        // public static void Postfix(EntityProjectile __instance, float dt)
        // {
        // 
        // }
        
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
            // Is it a valid target?
            var eeb = entity.GetBehavior<EnchantmentEntityBehavior>();
            if (eeb != null)
            {
                // Projectile Enchantments
                eeb.TryEnchantments(__instance.FiredBy as EntityAgent, __instance.ProjectileStack);
                
                // Default Bow enchantments
                string enchantString = __instance.FiredBy.WatchedAttributes.GetString("pendingRangedEnchants");
                if (enchantString != null)
                {
                    Dictionary<string, int> enchants = new Dictionary<string, int>();
                    int timestamp = 0;
                    string[] eStrings = enchantString.Split(";", StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < eStrings.Length; i++)
                    {
                        string s = eStrings[i];
                        if (i == 0)
                            int.TryParse(s, out timestamp);
                        else
                        {
                            i++;
                            string sP = eStrings[i];
                            int sPower = 0;
                            int.TryParse(sP, out sPower);
                            enchants.Add(s, sPower);
                        }
                    }

                    if (enchants.ContainsKey("healing"))
                        __instance.Damage = 0;

                    ItemStack enchantedStack = __instance.ProjectileStack.Clone();

                    if (entity.Api.World.Calendar.ElapsedSeconds - timestamp > 6)
                        return;

                    ITreeAttribute tree = enchantedStack.Attributes.GetOrAddTreeAttribute("enchantments");
                    foreach (KeyValuePair<string, int> keyValuePair in enchants)
                        tree.SetInt(keyValuePair.Key, keyValuePair.Value);
                    enchantedStack.Attributes.MergeTree(tree);

                    eeb.TryEnchantments(__instance.FiredBy as EntityAgent, enchantedStack);
                }

                // if (didEnchant != true)
                //     entity.World.Api.Logger.Error("[KRPG Enchantment] Attempted to process a projectile's enchantments, but ");
                
                // Projectile Entity Enchantments - WIP
                // 
                // ITreeAttribute pTree = __instance.WatchedAttributes.GetOrAddTreeAttribute("enchantments");
                // if (pTree != null)
                // {
                //     foreach (IAttribute attr in pTree.Values)
                //     {
                //         __instance.World.Logger.Warning("EntityProjectile has {0} {1}", attr.ToString(), attr.GetValue());
                //     }
                //     ItemStack tempStack = __instance.ProjectileStack.Clone();
                //     tempStack.Attributes.MergeTree(pTree);
                //     ITreeAttribute tTree = tempStack.Attributes.GetOrAddTreeAttribute("enchantments");
                //     foreach (IAttribute attr in tTree.Values)
                //     {
                //         __instance.World.Logger.Warning("EntityProjectile has {0} {1}", attr.ToString(), attr.GetValue());
                //     }
                //     eeb.TryEnchantments(__instance.FiredBy as EntityAgent, tempStack);
                // }

                
            }
        }
    }
}