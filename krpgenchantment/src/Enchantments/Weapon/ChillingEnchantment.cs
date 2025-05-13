using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using KRPGLib.Enchantment.API;

namespace KRPGLib.Enchantment
{
    public class ChillingEnchantment : Enchantment
    {
        // public float PowerMultiplier { get { return Attributes.GetFloat("PowerMultiplier", -10); } }
        float PowerMultiplier { get { return Modifiers.GetFloat("PowerMultiplier"); } }
        public ChillingEnchantment(ICoreAPI api) : base(api)
        {
            // Setup the default config
            Enabled = true;
            Code = "chilling";
            Category = "Weapon";
            LoreCode = "enchantment-chilling";
            LoreChapterID = 0;
            MaxTier = 5;
            // Attributes = new TreeAttribute();
            // Attributes.SetFloat("PowerMultiplier", -10);
            Modifiers = new EnchantModifiers() { { "PowerMultiplier", -10.00 } };
        }
        public override void OnAttack(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] {0} is being affected by a chilling enchantment.", enchant.TargetEntity.GetName());
            
            EntityBehaviorBodyTemperature ebbt = enchant.TargetEntity.GetBehavior<EntityBehaviorBodyTemperature>();
            if (ebbt != null)
                ebbt.CurBodyTemperature = enchant.Power * PowerMultiplier;
        }
    }
}
