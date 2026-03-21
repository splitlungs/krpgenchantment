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
        float COMultiplier { get { return Modifiers.GetFloat("CombatOverhaulMultiplier"); } }
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
            Modifiers = new EnchantModifiers() { {"PowerMultiplier", 0.2f }, {"CombatOverhaulMultiplier", 0.1f } };
            Version = 1.00f;
        }
        // TODO: Fix Combat Overhaul overwriting these values periodically
        public override bool TryEnchantItem(ref ItemStack inStack, int enchantPower, bool force, ICoreServerAPI api)
        {
            bool didEnch = base.TryEnchantItem(ref inStack, enchantPower, force, api);
            if (!didEnch) return false;
            // SO FAR, we only need to save to ItemStack for CO
            if (Api.ModLoader.GetModSystem<KRPGEnchantmentSystem>()?.COSysServer == null) return didEnch;
            AddMultipliersCO(ref inStack, enchantPower);
            return true;
        }
        public override void OnAttackStart(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            if (!(Api is ICoreServerAPI sapi)) return;
            Entity entity = enchant?.CauseEntity;
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] Applying {0} {1} to {2}", Code, enchant.Power, entity.GetName());
            // Write to entity
            if (sapi.ModLoader.GetModSystem<KRPGEnchantmentSystem>()?.COSysServer != null)
            {
                // float mul = enchant.Power * COMultiplier;
                // entity.Stats.Set("steadyAim", "krpge:" + Code, mul, true);
                AddMultipliersCO(ref enchant.SourceSlot, enchant.Power);
                enchant.SourceSlot.MarkDirty();
                return;
            }
            else
                AddMultipliers(entity, enchant.Power);
        }
        public override void OnAttackCancel(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            Entity entity = enchant?.CauseEntity;
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] Removing {0} {1} from {2}.", Code, enchant.Power, entity.GetName());
            // CO saves to Itemstack
            if (Api.ModLoader.GetModSystem<KRPGEnchantmentSystem>()?.COSysServer != null) return;
            // Write to entity
            RemoveAllMultipliers(entity);
        }
        public override void OnAttackStop(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            if (!(Api is ICoreServerAPI sapi)) return;
            // CO saves to Itemstack
            if (sapi.ModLoader.GetModSystem<KRPGEnchantmentSystem>()?.COSysServer != null) return;
            Entity entity = enchant?.CauseEntity;
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] Removing {0} {1} from {2}.", Code, enchant.Power, entity.GetName());
            // Write to entity
            RemoveAllMultipliers(entity);
        }
        /*
        public override void OnToggle(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            Entity entity = enchant?.CauseEntity;
            bool activate = parameters.GetBool("ToggleState");
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] Toggling {0} {1} to {2} for {3}.", Code, enchant.Power, activate.ToString(), entity.GetName());
            if (activate == true)
                AddMultipliers(enchant.CauseEntity, enchant.Power);
            else
                RemoveAllMultipliers(enchant.CauseEntity);
        }
        */
        /// <summary>
        /// Adds multipliers for vanilla VS.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="power"></param>
        void AddMultipliers(Entity entity, int power)
        {
            float mul = power * PowerMultiplier;
            entity.Stats.Set("rangedWeaponsAcc", "krpge:" + Code, mul, false);
        }
        /// <summary>
        /// Adds multipliers for Combat Overhaul
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="power"></param>
        public void AddMultipliersCO(ref ItemSlot slot, int power)
        {
            //entity.Stats.Set("steadyAim", "krpge" + Code, enchant.Power * PowerMultiplier, true);
            float mul = power * COMultiplier;
            float curVal = slot.Itemstack.Attributes.GetFloat("aimingDifficulty", 1.0f);
            ITreeAttribute eTree = slot.Itemstack.Attributes.GetOrAddTreeAttribute("enchantments");
            float ogVal = eTree.GetFloat("aimingDifficulty", 1.0f);
            if (ogVal != 1)
                curVal = ogVal - mul;
            else
            {
                eTree.SetFloat("aimingDifficulty", curVal);
                slot.Itemstack.Attributes.MergeTree(eTree);
                curVal = curVal - mul;
            }
            slot.Itemstack.Attributes.SetFloat("aimingDifficulty", curVal);
        }
        /// <summary>
        /// Adds multipliers for Combat Overhaul
        /// </summary>
        /// <param name="stack"></param>
        /// <param name="power"></param>
        public void AddMultipliersCO(ref ItemStack stack, int power)
        {
            // entity.Stats.Set("bowsProficiency", "krpge" + Code, mul, true);
            // entity.Stats.Set("crossbowsProficiency", "krpge" + Code, mul, true);
            // entity.Stats.Set("firearmsProficiency", "krpge" + Code, mul, true);
            float mul = power * COMultiplier;
            float curVal = stack.Attributes.GetFloat("aimingDifficulty", 1.0f);
            ITreeAttribute eTree = stack.Attributes.GetOrAddTreeAttribute("enchantments");
            float ogVal = eTree.GetFloat("aimingDifficulty", 1.0f);
            if (ogVal != 1)
                curVal = mul + ogVal;
            else
            {
                eTree.SetFloat("aimingDifficulty", curVal);
                stack.Attributes.MergeTree(eTree);
                curVal = mul + curVal;
            }
            stack.Attributes.SetFloat("aimingDifficulty", curVal);
        }
        void RemoveAllMultipliers(Entity entity)
        {
            // Remove both, just in case someone is hot swapping CO between triggers
            entity.Stats.Remove("rangedWeaponsAcc", "krpge:" + Code);
            // entity.Stats.Remove("steadyAim", "krpge:" + Code);
            // entity.Stats.Set("rangedWeaponsAcc", "krpge:" + Code, 1.0f, false);
            // entity.Stats.Set("steadyAim", "krpge:" + Code, 1.0f, false);
            
        }
    }
}
