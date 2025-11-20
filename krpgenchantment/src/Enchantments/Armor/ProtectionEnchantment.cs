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
using Vintagestory.API.Util;
using Vintagestory.API.Config;

namespace KRPGLib.Enchantment
{
    public class ProtectionEnchantment : Enchantment
    {
        string DamageResist { get { return Modifiers.GetString("DamageResist").ToLowerInvariant(); } }
        float PowerMultiplier { get { return Modifiers.GetFloat("PowerMultiplier"); } }
        public ProtectionEnchantment(ICoreAPI api) : base(api)
        {
            // Setup the default config
            Enabled = true;
            Code = "protection";
            Category = "ResistDamage";
            LoreCode = "enchantment-protection";
            LoreChapterID = 10;
            MaxTier = 5;
            ValidToolTypes = new List<string> {
                "Armor-Head", "Armor-Body", "Armor-Legs",
                "ArmorHead", "ArmorBody", "ArmorLegs",
                "Shield",
                "Buckler", "Forlorn-Shield"
            };
            Modifiers = new EnchantModifiers()
            { 
                {"DamageResist", "bluntattack;piercingattack;slashingattack;" }, 
                {"PowerMultiplier", 0.1f } 
            };
            Version = 1.01f;
        }
        public override void OnHit(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] {0} is being affected by an {1} enchantment.", enchant.TargetEntity.GetName(), Code);

            // Extract the Damage Values
            float dmg = parameters.GetFloat("damage");
            string dmgType = parameters.GetString("type");
            string[] dmgResists = DamageResist.Split(';', StringSplitOptions.RemoveEmptyEntries);

            foreach (string res in dmgResists)
            {
                if (res == dmgType && dmg > 0)
                {
                    float resist = enchant.Power * PowerMultiplier;
                    resist = 1 - resist;
                    // dmg = MathF.Min((1 - resist) * dmg, 0);
                    dmg = Math.Max(0f, dmg * resist);
                    parameters["damage"] = dmg;

                    if (EnchantingConfigLoader.Config?.Debug == true)
                        Api.Logger.Event("[KRPGEnchantment] {0} Enchantment processed with {1} damage to {2}.", Code, dmg, enchant.TargetEntity.GetName());

                    return;
                }
            }
        }
    }
}
