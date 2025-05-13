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
        // string DamageResist { get { return Attributes.GetString("DamageResist", "blunt;piercing;slashing"); } }
        // float PowerMultiplier { get { return Attributes.GetFloat("PowerMultiplier", 0.1f); } }
        string DamageResist { get { return Modifiers.GetString("DamageResist"); } }
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
            // Attributes = new TreeAttribute();
            // Attributes.SetString("DamageResist", "blunt;piercing;slashing");
            // Attributes.SetFloat("PowerMultiplier", 0.1f);
            Modifiers = new EnchantModifiers()
            {
                { "DamageResist", "fire"}, { "PowerMultiplier", 0.1 }
            };
        }
        public override void OnHit(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] {0} is being affected by an DamageResist enchantment.", enchant.TargetEntity.GetName());

            if (DamageResist.Contains(enchant.Type.ToString()))
            {
                float resist = enchant.Power * PowerMultiplier;
                float dmg = MathF.Min(((1 - resist) * (float)parameters["damage"]), 0);
                parameters["damage"] = dmg;
            }

            if (EnchantingConfigLoader.Config.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] Durable Enchantment processed with {0} damage to item {1}.", 
                    parameters["damage"], enchant.SourceStack.GetName());
        }
    }
}
