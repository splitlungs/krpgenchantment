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
                EnchantmentSource enchant = new EnchantmentSource() { 
                    SourceStack = itemslot.Itemstack, 
                    Trigger = "OnHit", 
                    Code = "durable", 
                    Power = durable };
                EnchantModifiers parameters = new EnchantModifiers() { { "damage", amount } };
                bool didEnchantment = world.Api.EnchantAccessor().TryEnchantment(enchant, ref parameters);
                if (didEnchantment != true)
                    return true;
            }

            return true;
        }
    }
}
