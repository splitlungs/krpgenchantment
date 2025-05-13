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
    public class KnockbackEnchantment : Enchantment
    {
        float PowerMultiplier { get { return Modifiers.GetFloat("PowerMultiplier"); } }
        public KnockbackEnchantment(ICoreAPI api) : base(api)
        {
            // Setup the default config
            Enabled = true;
            Code = "knockback";
            Category = "Weapon";
            LoreCode = "enchantment-knockback";
            LoreChapterID = 7;
            MaxTier = 5;
            Modifiers = new EnchantModifiers() { {"PowerMultiplier", 20.00 } };
        }
        public override void OnAttack(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] {0} is being affected by a Knockback enchantment.", enchant.TargetEntity.GetName());

            double weightedPower = enchant.Power * PowerMultiplier;
            // EntityPos facing = entity.SidedPos.AheadCopy(0.1);
            // entity.SidedPos.Motion.Mul(facing.X * -weightedPower, 1, facing.Z * -weightedPower);
            enchant.TargetEntity.SidedPos.Motion.AddCopy(-weightedPower, 1, -weightedPower);
            // Vec3d repulse = entity.ownPosRepulse;

            if (EnchantingConfigLoader.Config.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] Durable Enchantment processed with {0} damage to item {1}.", 
                    (float)parameters["damage"], enchant.SourceStack.GetName());
        }
    }
}
