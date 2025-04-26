using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using KRPGLib.API;
using Vintagestory.GameContent;

namespace KRPGLib.Enchantment
{
    public class DurableEnchantment : Enchantment
    {
        public DurableEnchantment(ICoreAPI api) : base(api)
        {

        }
        public override void OnHit(EnchantmentSource enchant, ItemSlot slot, ref float? damage)
        {
            int baseChance = (int)Modifiers[0];
            int roll = Api.World.Rand.Next(baseChance);
            if (roll + enchant.Power + 1 >= baseChance)
                damage = 0;
            if (EnchantingConfigLoader.Config.Debug == true)
                Api.Logger.VerboseDebug("[KRPGEnchantment] Durable Enchantment processed with {0} damage to item {1}.", damage, slot.Itemstack.GetName());
        }
    }
}
