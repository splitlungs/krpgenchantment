using System;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace KRPGLib.Enchantment
{
    public class EnchantmentSource : DamageSource
    {
        // Defines the method called by the trigger.
        public string Trigger;
        // The Code of the Enchantment
        public string Code;
        // The Power or Tier of the Enchantment
        public int Power;
        // Entity being affected by the Enchantment
        public Entity TargetEntity;
        // Location where an Enchantment should affect
        public BlockPos TargetPos;
        /// <summary>
        /// Create a valid DamageSource from this EnchantmentSource.
        /// </summary>
        /// <returns></returns>
        public DamageSource ToDamageSource()
        {
            return new DamageSource()
            {
                Source = Source,
                Type = Type,
                HitPosition = HitPosition,
                SourceEntity = SourceEntity,
                CauseEntity = CauseEntity,
                SourceBlock = SourceBlock,
                SourcePos = SourcePos,
                DamageTier = DamageTier,
                KnockbackStrength = KnockbackStrength,
                YDirKnockbackDiv = YDirKnockbackDiv
            };
        }
    }
}
