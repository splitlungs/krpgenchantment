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
        string DamageResist { get { return Modifiers.GetString("DamageResist"); } }
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
                "Shield" };
            Modifiers = new EnchantModifiers()
            { 
                {"DamageResist", "blunt;piercing;slashing" }, 
                {"PowerMultiplier", 0.1f } 
            };
        }
        public override void OnHit(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] {0} is being affected by an DamageResist enchantment.", enchant.TargetEntity.GetName());

            if (DamageResist.Contains(enchant.Type.ToString()) && parameters.ContainsKey("damage") == true)
            {
                float resist = enchant.Power * PowerMultiplier;
                float dmg = MathF.Min(((1 - resist) * (float)parameters["damage"]), 0);
                parameters["damage"] = dmg;
            }

            if (EnchantingConfigLoader.Config.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] {0} Enchantment processed with {1} damage to item {2}.", Lang.Get(Code), parameters["damage"], enchant.SourceStack.GetName());
        }
    }
}
