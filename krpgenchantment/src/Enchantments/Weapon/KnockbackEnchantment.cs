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
using static System.Net.Mime.MediaTypeNames;
using Vintagestory.API.Config;

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
            Category = "ControlTarget";
            LoreCode = "enchantment-knockback";
            LoreChapterID = 7;
            MaxTier = 5;
            ValidToolTypes = new List<string> {
                "Knife", "Axe",
                "Club", "Sword",
                "Spear",
                "Bow", "Sling",
                "Drill",
                "Halberd", "Mace", "Pike", "Polearm", "Poleaxe", "Staff", "Warhammer",
                "Javelin",
                "Crossbow", "Firearm",
                "Wand" };
            Modifiers = new EnchantModifiers() { {"PowerMultiplier", 1.00 } };
        }
        public override void OnAttack(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] {0} is being affected by a Knockback enchantment.", enchant.TargetEntity.GetName());

            float weightedPower = enchant.Power * PowerMultiplier;

            // Get attacking direction
            Vec3d pushDir = enchant.TargetEntity.Pos.XYZ - enchant.SourceEntity.Pos.XYZ;
            pushDir.Y = 0;
            pushDir.Normalize();
            // Aplly motion with a little bit of lift
            enchant.TargetEntity.SidedPos.Motion.X += pushDir.X * weightedPower;
            enchant.TargetEntity.SidedPos.Motion.Y += 0.1;
            enchant.TargetEntity.SidedPos.Motion.Z += pushDir.Z * weightedPower;
        }
    }
}
