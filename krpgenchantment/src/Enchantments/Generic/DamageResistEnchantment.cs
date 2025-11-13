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
    public class DamageResistEnchantment : Enchantment
    {
        string DamageResist { get { return Modifiers.GetString("DamageResist").ToLowerInvariant(); } }
        float PowerMultiplier { get { return Modifiers.GetInt("PowerMultiplier"); } }
        public DamageResistEnchantment(ICoreAPI api) : base(api)
        {
            // Setup the default config
            Enabled = true;
            Code = "damageresist";
            Category = "Armor";
            LoreCode = "enchantment-damageresist";
            LoreChapterID = -1;
            MaxTier = 5;
            ValidToolTypes = new List<string>()
            {
                "ArmorHead", "ArmorBody", "ArmorLegs",
                "Shield"
            };
            Modifiers = new EnchantModifiers()
            {
                { "DamageResist", "fire"}, { "PowerMultiplier", 0.1 }
            };
            Version = 1.00f;
        }
        public override void OnHit(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] {0} is being affected by an {1} enchantment.", enchant.TargetEntity.GetName(), Code);

            // Extract the Damage Values
            float dmg = parameters.GetFloat("damage");
            string dmgType = parameters.GetString("type");

            if (DamageResist == dmgType && dmg > 0)
            {
                float resist = enchant.Power * PowerMultiplier;
                resist = 1 - resist;
                dmg = Math.Max(0f, dmg * resist);
                parameters["damage"] = dmg;

                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] {0} Enchantment processed with {1} damage to {2}.", Code, dmg, enchant.TargetEntity.GetName());
            }
        }
    }
}
