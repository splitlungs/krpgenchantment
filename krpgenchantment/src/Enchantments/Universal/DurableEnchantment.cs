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
using KRPGLib.Enchantment.API;
using Vintagestory.GameContent;

namespace KRPGLib.Enchantment
{
    public class DurableEnchantment : Enchantment
    {
        double PowerMultiplier { get { return (double)Modifiers.GetValueOrDefault("PowerMultiplier", 0.10d); } }
        public DurableEnchantment(ICoreAPI api) : base(api)
        {
            // Setup the default config
            Enabled = true;
            Code = "durable";
            Category = "Universal";
            LoreCode = "enchantment-durable";
            LoreChapterID = 1;
            MaxTier = 5;
            Modifiers = new Dictionary<string, object> { { "PowerMultiplier", 0.10d } };
        }
        public override void OnHit(EnchantmentSource enchant, ItemSlot slot, ref float? damage)
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] {0} is being affected by a Durable enchantment.", enchant.TargetEntity.GetName());
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
