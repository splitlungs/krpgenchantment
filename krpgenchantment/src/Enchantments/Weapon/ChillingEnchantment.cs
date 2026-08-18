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
        float PowerMultiplier { get { return Modifiers.GetFloat("PowerMultiplier"); } }
        bool TriggerOnBlocked { get { return Modifiers.GetBool("TriggerOnBlocked"); } } 
        /// <summary>
        /// Reduces the target's internal temperature OnAttack.
        /// </summary>
        /// <param name="api"></param>
        public ChillingEnchantment(ICoreAPI api) : base(api)
        {
            // Setup the default config
            Enabled = true;
            Code = "chilling";
            Category = "StatusTarget";
            LoreCode = "enchantment-chilling";
            LoreChapterID = 0;
            MaxTier = 5;
            ValidToolTypes = new List<string>() {
                "Knife", "Axe",
                "Club", "Sword",
                "Spear",
                "Bow", "Sling",
                "Drill",
                "Halberd", "Mace", "Pike", "Polearm", "Poleaxe", "Quarterstaff" , "Sabre", "Staff", "Warhammer",
                "Javelin",
                "Crossbow", "Firearm",
                "Wand",
                "vanillaarmory:Club",
                "forgottenfirearms:gearlock-doublebarrel", "forgottenfirearms:gearlock-pistol", "forgottenfirearms:gearlock-repeater", "forgottenfirearms:gearlock-rifle"
            };
            Modifiers = new EnchantModifiers() { { "PowerMultiplier", -10.00 }, {"TriggerOnBlocked", false} };
            Version = 1.04f;
        }
        public override void OnAttacked(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] {0} is being affected by a chilling enchantment.", enchant.TargetEntity.GetName());
            
            bool blocked = parameters.GetBool("blocked");
            if (blocked && !TriggerOnBlocked)
                return;

            EntityBehaviorBodyTemperature ebbt = enchant.TargetEntity.GetBehavior<EntityBehaviorBodyTemperature>();
            if (ebbt != null)
                ebbt.CurBodyTemperature = enchant.Power * PowerMultiplier;
        }
    }
}
