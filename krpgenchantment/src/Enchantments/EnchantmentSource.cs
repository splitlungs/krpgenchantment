using System;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace KRPGLib.Enchantments
{
    public class EnchantmentSource : DamageSource
    {
        // Defines the method called by the trigger.
        public string Trigger;
        // The Code of the Enchantment
        public string Code;
        // The Power or Tier of the Enchantment
        public int Power;
        
    }
}
