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
                EnchantmentSource enchant = new EnchantmentSource() { Trigger = "OnTrigger", Code = "durable", Power = durable };
                bool didEnchantment = world.Api.EnchantAccessor().DoEnchantment(enchant, itemslot, ref amountf);
                if (didEnchantment != false)
                    amount = (int)amountf;

                int roll = world.Rand.Next(10);
                if (roll + durable + 1 >= 10)
                    amount = 0;
            }

            return true;
        }
    }
}
