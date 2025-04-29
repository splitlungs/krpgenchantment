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
    public class ResistFrostEnchantment : Enchantment
    {
        string DamageResist { get { return (string)Modifiers.GetValueOrDefault("DamageResist", "frost"); } }
        float PowerMultiplier { get { return (float)Modifiers.GetValueOrDefault("PowerMultiplier", 0.1f); } }
        public ResistFrostEnchantment(ICoreAPI api) : base(api)
        {
            // Setup the default config
            Enabled = true;
            Code = "resistfrost";
            Category = "Armor";
            LoreCode = "enchantment-resistfrost";
            LoreChapterID = 13;
            MaxTier = 5;
            Modifiers = new Dictionary<string, object>()
            {
                { "DamageResist", "frost"}, { "PowerMultiplier", 0.1 }
            };
        }
        public override void OnHit(EnchantmentSource enchant, ItemSlot slot, ref float? damage)
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] {0} is being affected by an DamageResist enchantment.", enchant.TargetEntity.GetName());

            if (DamageResist.Contains(enchant.Type.ToString()))
            {
                float resist = enchant.Power * PowerMultiplier;
                float dmg = MathF.Min(((1 - resist) * (float)damage), 0);
                damage = dmg;
            }

            if (EnchantingConfigLoader.Config.Debug == true)
                Api.Logger.VerboseDebug("[KRPGEnchantment] Durable Enchantment processed with {0} damage to item {1}.", damage, slot.Itemstack.GetName());
        }
    }
}
