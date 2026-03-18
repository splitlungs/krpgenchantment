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
using Vintagestory.API.Server;
using KRPGLib.Enchantment.API;
using Vintagestory.GameContent;
using Vintagestory.API.Util;
using Vintagestory.API.Config;

namespace KRPGLib.Enchantment
{
    public class AccurateEnchantment : Enchantment
    {
        float PowerMultiplier { get { return Modifiers.GetFloat("PowerMultiplier"); } }
        /// <summary>
        /// Provides ranged weapon accuracy modifiers the entity who triggers OnEquip.
        /// </summary>
        /// <param name="api"></param>
        public AccurateEnchantment(ICoreAPI api) : base(api)
        {
            // Setup the default config
            Enabled = true;
            Code = "accurate";
            Category = "Enhancement";
            LoreCode = "enchantment-accurate";
            LoreChapterID = 25;
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
        // Disabled for now
        // Combat Overhaul will overwrite these values at server start
        public override bool TryEnchantItem(ref ItemStack inStack, int enchantPower, bool force, ICoreServerAPI api)
        {
            bool didEnch = base.TryEnchantItem(ref inStack, enchantPower, force, api);
            if (!didEnch) return false;
            float curVal = inStack.Attributes.GetFloat("aimingDifficulty", 1);
            ITreeAttribute eTree = inStack.Attributes.GetOrAddTreeAttribute("enchantments");
            float ogVal = eTree.GetFloat("aimingDifficulty", 1);
            if (ogVal != 1)
                curVal = ogVal - (enchantPower * PowerMultiplier);
            else
            {
                eTree.SetFloat("aimingDifficulty", curVal);
                inStack.Attributes.MergeTree(eTree);
                curVal = curVal - (enchantPower * PowerMultiplier);
            }
            inStack.Attributes.SetFloat("aimingDifficulty", curVal);
            return true;
        }
        public override void OnAttackStart(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            Entity entity = enchant?.CauseEntity;
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] Applying {0} {1} to {2}", Code, enchant.Power, entity.GetName());
            // Write to entity
            if (Api.ModLoader.GetModSystem<KRPGEnchantmentSystem>()?.COSysServer != null)
                AddMultipliersCO(enchant.SourceStack, enchant.Power);
            else
                AddMultipliers(entity, enchant.Power);
        }
        /// <summary>
        /// Adds multipliers for vanilla VS.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="power"></param>
        void AddMultipliers(Entity entity, int power)
        {
            float mul = power * PowerMultiplier;
            entity.Stats.Set("rangedWeaponsAcc", "krpge" + Code, mul, true);
        }
        /// <summary>
        /// Adds multipliers for Combat Overhaul
        /// </summary>
        /// <param name="stack"></param>
        /// <param name="power"></param>
        void AddMultipliersCO(ItemStack stack, int power)
        {
            //entity.Stats.Set("steadyAim", "krpge" + Code, enchant.Power * PowerMultiplier, true);
            float mul = power * PowerMultiplier;
            float curVal = stack.Attributes.GetFloat("aimingDifficulty", 1);
            ITreeAttribute eTree = stack.Attributes.GetOrAddTreeAttribute("enchantments");
            float ogVal = eTree.GetFloat("aimingDifficulty", 1);
            if (ogVal != 1)
                curVal = ogVal - mul;
            else
            {
                eTree.SetFloat("aimingDifficulty", curVal);
                stack.Attributes.MergeTree(eTree);
                curVal = curVal - mul;
            }
            stack.Attributes.SetFloat("aimingDifficulty", curVal);
        }
        public override void OnAttackCancel(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            Entity entity = enchant?.CauseEntity;
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] Removing {0} {1} from {2}.", Code, enchant.Power, entity.EntityId);
            // Write to entity
            RemoveAllMultipliers(entity);
        }
        public override void OnAttackStop(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            Entity entity = enchant?.CauseEntity;
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] Removing {0} {1} from {2}.", Code, enchant.Power, entity.EntityId);
            // Write to entity
            RemoveAllMultipliers(entity);
        }
        void RemoveAllMultipliers(Entity entity)
        {
            // Remove both, just in case someone is hot swapping CO between triggers
            entity.Stats.Remove("rangedWeaponsAcc", "krpge" + Code);
            // entity.Stats.Remove("steadyAim", "krpge" + Code);
        }
    }
}
