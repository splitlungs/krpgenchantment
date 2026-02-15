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
    public class FalseLifeEnchantment : Enchantment
    {
        float PowerMultiplier { get { return Modifiers.GetFloat("PowerMultiplier"); } }
        int MaxActivePerEntity { get { return Modifiers.GetInt("MaxActivePerEntity"); } }
        /// <summary>
        /// Provides max life modifiers the entity who triggers OnEquip.
        /// </summary>
        /// <param name="api"></param>
        public FalseLifeEnchantment(ICoreAPI api) : base(api)
        {
            // Setup the default config
            Enabled = true;
            Code = "falselife";
            Category = "Universal";
            LoreCode = "enchantment-falselife";
            LoreChapterID = 23;
            MaxTier = 5;
            ValidToolTypes = new List<string> {
                "Arm", "Emblem", "Neck", "Waist"
            };
            Modifiers = new EnchantModifiers() { {"PowerMultiplier", 1.0f }, { "MaxActivePerEntity", 4 } };
            Version = 1.00f;
        }
        public override void OnEquip(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            // Safety Checks & Setup
            bool IsHotbar = parameters.GetBool("IsHotbar");
            Entity entity = enchant?.CauseEntity;
            EntityBehaviorHealth ebh = entity?.GetBehavior<EntityBehaviorHealth>();
            if (IsHotbar == true || entity == null || ebh == null) return;
            if (MaxActivePerEntity < 1)
            { 
                RemoveAllMultipliers(entity);
                return;
            }
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] Applying {0} {1} to {1}.", Code, enchant.Power, entity.EntityId);
            // Limit per config
            List<int> powers = GetTotalPowers(entity);
            if (powers == null) return;
            // Calculate total
            float mul = GetTotalMultiplier(powers);
            // Write to Entity
            ebh.SetMaxHealthModifiers("krpge" + Code, mul);
            ebh.UpdateMaxHealth();
        }
        public override void OnUnEquip(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            // Safety Checks & Setup
            bool IsHotbar = parameters.GetBool("IsHotbar");
            Entity entity = enchant?.CauseEntity;
            EntityBehaviorHealth ebh = entity?.GetBehavior<EntityBehaviorHealth>();
            if (IsHotbar == true || entity == null || ebh == null) return;
            if (MaxActivePerEntity < 1)
            { 
                RemoveAllMultipliers(entity);
                return;
            }
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] Removing {0} {1} from {2}.", Code, enchant.Power, entity.EntityId);
            // Limit per config
            List<int> powers = GetTotalPowers(entity);
            if (powers == null) return;
            // Calculate total
            float mul = GetTotalMultiplier(powers);
            // Write to Entity
            ebh.SetMaxHealthModifiers("krpge" + Code, mul);
            ebh.UpdateMaxHealth();
        }
        List<int> GetTotalPowers(Entity entity)
        {
            EnchantmentEntityBehavior eeb = entity?.GetBehavior<EnchantmentEntityBehavior>();
            if (eeb == null) return null;
            List<int> powers = new List<int>();
            foreach (ItemSlot slot in eeb.gearInventory)
            {
                if (slot.Empty) continue;
                Dictionary<string, int> enc = Api.EnchantAccessor().GetActiveEnchantments(slot.Itemstack);
                if (enc.TryGetValue(Code, out int p) == false) continue;
                powers.Add(p);
            }
            powers.Sort();
            powers.Reverse();
            int overflow = powers.Count - MaxActivePerEntity;
            int startAt = powers.Count - overflow;
            if (overflow > 0) powers.RemoveRange(startAt, overflow);
            return powers;
        }
        float GetTotalMultiplier(List<int> powers)
        {
            float mul = 0f;
            foreach (int p in powers)
            {
                float m = p * PowerMultiplier;
                mul += m;
            }
            return mul;
        }
        void RemoveAllMultipliers(Entity entity)
        {
            EntityBehaviorHealth ebh = entity?.GetBehavior<EntityBehaviorHealth>();
            if (ebh == null) return;
            ebh.SetMaxHealthModifiers("krpge" + Code, 0);
            ebh.UpdateMaxHealth();
        }
    }
}
