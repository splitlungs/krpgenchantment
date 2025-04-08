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

namespace KRPGLib.Enchantment
{
    [HarmonyPatch]
    public class CollectibleObject_Patch
    {
        [HarmonyPatch(typeof(CollectibleObject), "DamageItem")]
        public static bool Prefix(CollectibleObject __instance, IWorldAccessor world, Entity byEntity, ItemSlot itemslot, ref int amount)
        {
            Dictionary<string, int> enchants = world.Api.GetEnchantments(itemslot.Itemstack);
            if (enchants == null)
                return true;

            int durable = enchants.GetValueOrDefault("durable", 0);

            if (durable > 0)
            {
                int roll = world.Rand.Next(10);
                if (roll + durable + 1 >= 10)
                    amount = 0;
            }

            return true;
        }

        // [HarmonyPatch(typeof(CollectibleObject), "GetHeldItemInfo")]
        // public static void Postfix(CollectibleObject __instance, ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        // {
        //     if (!EnchantingConfigLoader.Config.ValidReagents.ContainsKey(__instance.Code)) return;
        // 
        //     ITreeAttribute tree = inSlot.Itemstack.Attributes?.GetOrAddTreeAttribute("enchantments");
        //     if (tree == null) return;
        // 
        //     int p = tree.GetInt("potential");
        // 
        //     dsc.Append("<font color=\"" + Enum.GetName(typeof(EnchantColors), p) + "\">" + Lang.Get("krpgenchantment:reagent-potential") + ": " + p);
        // }
        // [HarmonyPatch(typeof(CollectibleObject), "OnCollected")]
        // public static void Postfix(CollectibleObject __instance, ItemStack stack, Entity entity)
        // {
        //     if (!EnchantingConfigLoader.Config.ValidReagents.ContainsKey(__instance.Code)) return;
        // 
        //     ITreeAttribute tree = stack.Attributes?.GetOrAddTreeAttribute("enchantments");
        //     if (tree == null) return;
        // 
        //     int p = tree.GetInt("potential");
        //     if (p == 0)
        //     {
        //         entity.Api.Logger.Event("[KRPGEnchantment] Assessing a collectible with Power {0}.", p);
        //         p = entity.Api.AssessReagent(stack);
        //     }
        //     entity.Api.Logger.Event("[KRPGEnchantment] Collected a collectible with Power {0}.", p);
        // }
    }
}
