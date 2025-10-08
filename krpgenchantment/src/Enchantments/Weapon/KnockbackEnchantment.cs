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
        float HorizontalMultiplier { get { return Modifiers.GetFloat("HorizontalMultiplier"); } }
        float VerticalMultiplier { get { return Modifiers.GetFloat("VerticalMultiplier"); } }
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
            Modifiers = new EnchantModifiers() { {"HorizontalMultiplier", 10 },  { "VerticalMultiplier", 0.1 } };
            Version = 1.01f;
        }
        public override void OnAttack(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] {0} is being affected by a Knockback enchantment.", enchant.TargetEntity.GetName());

            // OBSOLETE
            float hPower = enchant.Power * HorizontalMultiplier * GameMath.Clamp((1f - enchant.TargetEntity.Properties.KnockbackResistance) / 10f, 0f, 1f);
            float vPower = enchant.Power * VerticalMultiplier * GameMath.Clamp((1f - enchant.TargetEntity.Properties.KnockbackResistance) / 10f, 0f, 1f);
            
            // Get attacking direction
            Vec3d pushDir = enchant.TargetEntity.Pos.XYZ - enchant.SourceEntity.Pos.XYZ;
            pushDir.Y = 0;
            pushDir.Normalize();
            // Aplly motion with a little bit of lift
            enchant.TargetEntity.SidedPos.Motion.X += pushDir.X * hPower;
            enchant.TargetEntity.SidedPos.Motion.Y += vPower;
            enchant.TargetEntity.SidedPos.Motion.Z += pushDir.Z * hPower;
        }
    }
}
