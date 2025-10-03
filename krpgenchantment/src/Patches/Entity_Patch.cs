using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.API.Datastructures;
using static System.Net.Mime.MediaTypeNames;
using System.Reflection;

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
                int tickMs = EnchantingConfigLoader.Config?.EntityTickMs ?? 250;
                EnchantmentEntityBehavior eb = new EnchantmentEntityBehavior(__instance, tickMs);
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
                int tickMs = EnchantingConfigLoader.Config?.EntityTickMs ?? 250;
                EnchantmentEntityBehavior eb = new EnchantmentEntityBehavior(__instance, tickMs);
                __instance.AddBehavior(eb);
            }

            return true;
        }
    }
}
