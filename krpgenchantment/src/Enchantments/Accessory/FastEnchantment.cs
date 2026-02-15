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
    public class FastEnchantment : Enchantment
    {
        float PowerMultiplier { get { return Modifiers.GetFloat("PowerMultiplier"); } }
        int MaxActivePerEntity { get { return Modifiers.GetInt("MaxActivePerEntity"); } }
        /// <summary>
        /// Provides walk speed modifiers the entity who triggers OnEquip.
        /// </summary>
        /// <param name="api"></param>
        public FastEnchantment(ICoreAPI api) : base(api)
        {
            // Setup the default config
            Enabled = true;
            Code = "fast";
            Category = "Movement";
            LoreCode = "enchantment-fast";
            LoreChapterID = 22;
            MaxTier = 5;
            ValidToolTypes = new List<string> {
                "Arm", "Emblem", "Face", "Hand", "Neck", "Shoulder", "Waist"
            };
            Modifiers = new EnchantModifiers() { { "PowerMultiplier", 0.05f }, { "MaxActivePerEntity", 4 } };
            Version = 1.00f;
        }
        public override void OnEquip(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            // Safety Checks & Setup
            bool IsHotbar = parameters.GetBool("IsHotbar");
            Entity entity = enchant?.CauseEntity;
            if (IsHotbar == true || entity == null) return;
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
            entity.Stats.Set("walkspeed", "krpge" + Code, mul, true);
        }
        public override void OnUnEquip(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            // Safety Checks & Setup
            bool IsHotbar = parameters.GetBool("IsHotbar");
            Entity entity = enchant?.CauseEntity;
            if (IsHotbar == true || entity == null) return;
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
            entity.Stats.Set("walkspeed", "krpge" + Code, mul, true);
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
            entity.Stats.Remove("hungerrate", "krpge" + Code);
        }
    }
}
