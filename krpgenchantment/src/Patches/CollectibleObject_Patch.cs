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
            Dictionary<string, int> enchants = world.Api.EnchantAccessor().GetEnchantments(itemslot.Itemstack);
            if (enchants == null)
                return true;

            int durable = enchants.GetValueOrDefault("durable", 0);

            if (durable > 0)
            {
                float amountf = 1f;
                EnchantmentSource enchant = new EnchantmentSource() { Trigger = "OnHit", Code = "durable", Power = durable };
                object[] parameters = new object[1] { amountf };
                bool didEnchantment = world.Api.EnchantAccessor().DoEnchantment(enchant, itemslot, ref parameters);
                if (didEnchantment != true)
                    return true;

                amount = (int)parameters[0];

                // if (didEnchantment != false)
                //     amount = (int)amountf;
                // 
                // int roll = world.Rand.Next(10);
                // if (roll + durable + 1 >= 10)
                //     amount = 0;
            }

            return true;
        }
        // [HarmonyPatch(typeof(CollectibleObject), "OnLoadedNative")]
        // public static void Postfix(CollectibleObject __instance, ICoreAPI api)
        // {
        // 
        //     api.Logger.Event("[KRPGEnchantment] Attempting to add Enchantment Behavior to CO.");
        //     EnchantmentBehavior eb = new EnchantmentBehavior(__instance);
        //     __instance.CollectibleBehaviors.AddToArray(eb);
        // 
        //     if (EnchantingConfigLoader.Config.ValidReagents.ContainsKey(__instance.Code.ToShortString()))
        //     {
        //         api.Logger.Event("[KRPGEnchantment] Attempting to add Reagent Behavior to CO.");
        //         ReagentBehavior rb = new ReagentBehavior(__instance);
        //         rb.Quantity = EnchantingConfigLoader.Config.ValidReagents[__instance.Code.ToShortString()];
        //         __instance.CollectibleBehaviors.AddToArray(rb);
        //     }
        // }
    }
}
