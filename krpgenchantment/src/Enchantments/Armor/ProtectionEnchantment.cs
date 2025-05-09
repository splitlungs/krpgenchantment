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
        // string DamageResist { get { return Attributes.GetString("DamageResist", "blunt;piercing;slashing"); } }
        // float PowerMultiplier { get { return Attributes.GetFloat("PowerMultiplier", 0.1f); } }
        string DamageResist { get { return (string)Modifiers.GetValueOrDefault("DamageResist", "blunt;piercing;slashing"); } }
        float PowerMultiplier { get { return (float)Modifiers.GetValueOrDefault("PowerMultiplier", 0.1f); } }
        public ProtectionEnchantment(ICoreAPI api) : base(api)
        {
            // Setup the default config
            Enabled = true;
            Code = "protection";
            Category = "Armor";
            LoreCode = "enchantment-protection";
            LoreChapterID = 10;
            MaxTier = 5;
            // Attributes = new TreeAttribute();
            // Attributes.SetString("DamageResist", "blunt;piercing;slashing");
            // Attributes.SetFloat("PowerMultiplier", 0.1f);
            Modifiers = new Dictionary<string, object>()
            { 
                {"DamageResist", "blunt;piercing;slashing" }, 
                {"PowerMultiplier", 0.1f } 
            };
        }
        public override void OnHit(EnchantmentSource enchant, ref Dictionary<string, object> parameters)
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
