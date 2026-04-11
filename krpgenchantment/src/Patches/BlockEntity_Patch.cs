// Disabled for now, but could be usefule later
/*
using System;
using HarmonyLib;
using Vintagestory.API.Common;

namespace KRPGLib.Enchantment
{
    [HarmonyPatch]
    public class BlockEntity_OnEntityLoaded_Patch
    {
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(BlockEntity), nameof(BlockEntity.Initialize))]
        public static bool Prefix(BlockEntity __instance, ICoreAPI api)
        {
            // Setup Enchantment Entity Behaviors on ALL Entities
            bool foundEB = false;
            if (__instance.GetBehavior<EnchantmentBEBehavior>() != null)
                foundEB = true;
            if (!foundEB)
            {
                if (EnchantingConfigLoader.Config?.Debug == true)
                    api.Logger.Event("[KRPGEnchantment] Adding an EnchantmentBEBehavior to {0} on loaded.", __instance.Block.Code.ToShortString());
                EnchantmentBEBehavior eb = new EnchantmentBEBehavior(__instance);
                __instance.Behaviors.Add(eb);
            }
            return true;
        }
    }
}
*/