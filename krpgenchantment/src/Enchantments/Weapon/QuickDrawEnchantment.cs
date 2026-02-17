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
    public class QuickDrawEnchantment : Enchantment
    {
        float PowerMultiplier { get { return Modifiers.GetFloat("PowerMultiplier"); } }
        /// <summary>
        /// Provides ranged weapon draw speed modifiers the entity who triggers OnEquip.
        /// </summary>
        /// <param name="api"></param>
        public QuickDrawEnchantment(ICoreAPI api) : base(api)
        {
            // Setup the default config
            Enabled = true;
            Code = "quickdraw";
            Category = "Universal";
            LoreCode = "enchantment-quickdraw";
            LoreChapterID = 26;
            MaxTier = 5;
            ValidToolTypes = new List<string> {
                "Bow", "Sling", "Spear",
                "Javelin",
                "Crossbow", "Firearm",
                "Wand"
            };
            Modifiers = new EnchantModifiers() { {"PowerMultiplier", 0.05f } };
            Version = 1.00f;
        }
        public override void OnEquip(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            bool IsHotbar = parameters.GetBool("IsHotbar");
            Entity entity = enchant?.CauseEntity;
            EnchantmentEntityBehavior eeb = entity?.GetBehavior<EnchantmentEntityBehavior>();
            if (IsHotbar == false || eeb == null) return;
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] Applying {0} {1} to {1}.", Code, enchant.Power, entity.EntityId);
            // Write to entity
            if (Api.ModLoader.GetModSystem<KRPGEnchantmentSystem>()?.combatOverhaul != null)
            {
                entity.Stats.Set("bowsProficiency", "krpge" + Code, enchant.Power * PowerMultiplier, true);
                entity.Stats.Set("crossbowsProficiency", "krpge" + Code, enchant.Power * PowerMultiplier, true);
                entity.Stats.Set("firearmsProficiency", "krpge" + Code, enchant.Power * PowerMultiplier, true);
            }
            else
                entity.Stats.Set("rangedWeaponsSpeed", "krpge" + Code, enchant.Power * PowerMultiplier, true);
        }
        public override void OnUnEquip(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            bool IsHotbar = parameters.GetBool("IsHotbar");
            Entity entity = enchant?.CauseEntity;
            EnchantmentEntityBehavior eeb = entity?.GetBehavior<EnchantmentEntityBehavior>();
            if (IsHotbar == false || eeb == null) return;
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] Removing {0} {1} from {2}.", Code, enchant.Power, entity.EntityId);
            // Write to entity
            RemoveAllMultipliers(entity);
        }
        void RemoveAllMultipliers(Entity entity)
        {
            // Remove all, just in case someone is hot swapping CO between triggers
            entity.Stats.Remove("bowsProficiency", "krpge" + Code);
            entity.Stats.Remove("crossbowsProficiency", "krpge" + Code);
            entity.Stats.Remove("firearmsProficiency", "krpge" + Code);
            entity.Stats.Remove("rangedWeaponsSpeed", "krpge" + Code);
        }
    }
}
