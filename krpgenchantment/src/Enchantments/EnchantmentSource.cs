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
        // The ItemSlot from which the Enchantment originated
        public ItemSlot SourceSlot;
        // The ItemStack from which the Enchantment originated
        public ItemStack SourceStack;
        // Entity being affected by the Enchantment
        public Entity TargetEntity;
        // Location where an Enchantment should affect
        public BlockPos TargetPos;
        public EnchantmentSource Clone()
        {
            return new EnchantmentSource()
            {
                Trigger = Trigger,
                Code = Code,
                Power = Power,
                SourceStack = SourceStack,
                TargetEntity = TargetEntity,
                TargetPos = TargetPos,
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
