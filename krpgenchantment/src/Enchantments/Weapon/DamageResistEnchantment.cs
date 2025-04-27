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
    public class DamageResistEnchantment : Enchantment
    {
        double PowerMultiplier { get { return (double)Modifiers[0]; } }
        public DamageResistEnchantment(ICoreAPI api) : base(api)
        {

        }
        public override void OnHit(EnchantmentSource enchant, ItemSlot slot, ref float? damage)
        {
            // Roll 0.01 - 1.00
            double roll = Api.World.Rand.NextDouble() + 0.01;
            double bonus = enchant.Power * PowerMultiplier;
            if ((roll + bonus) >= (1.00 - bonus))
                damage = 0;
            if (EnchantingConfigLoader.Config.Debug == true)
                Api.Logger.VerboseDebug("[KRPGEnchantment] Durable Enchantment processed with {0} damage to item {1}.", damage, slot.Itemstack.GetName());
        }
    }
}
