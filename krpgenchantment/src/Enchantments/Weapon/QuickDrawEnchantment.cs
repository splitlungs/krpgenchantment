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
using Vintagestory.API.Server;

namespace KRPGLib.Enchantment
{
    public class QuickDrawEnchantment : Enchantment
    {
        float PowerMultiplier { get { return Modifiers.GetFloat("PowerMultiplier"); } }
        float COMultiplier { get { return Modifiers.GetFloat("CombatOverhaulMultiplier"); } }
        /// <summary>
        /// Provides ranged weapon draw speed modifiers the entity who triggers OnEquip.
        /// </summary>
        /// <param name="api"></param>
        public QuickDrawEnchantment(ICoreAPI api) : base(api)
        {
            // Setup the default config
            Enabled = true;
            Code = "quickdraw";
            Category = "Enhancement";
            LoreCode = "enchantment-quickdraw";
            LoreChapterID = 26;
            MaxTier = 5;
            ValidToolTypes = new List<string> {
                "Bow", "Sling", "Spear",
                "Javelin",
                "Crossbow", "Firearm",
                "Wand"
            };
            Modifiers = new EnchantModifiers() { {"PowerMultiplier", 0.1f }, {"CombatOverhaulMultiplier", 0.5f } };
            Version = 1.00f;
        }
        // TODO: Fix Combat Overhaul overwriting these values periodically
        /*
        public override bool TryEnchantItem(ref ItemStack inStack, int enchantPower, bool force, ICoreServerAPI api)
        {
            bool didEnch = base.TryEnchantItem(ref inStack, enchantPower, force, api);
            if (!didEnch) return false;
            // SO FAR, we only need to save to ItemStack for CO
            if (Api.ModLoader.GetModSystem<KRPGEnchantmentSystem>()?.COSysServer == null) return didEnch;
            AddMultipliersCO(ref inStack, enchantPower);
            return true;
        }
        */
        public override void OnAttackStart(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            Entity entity = enchant?.CauseEntity;
            if (EnchantingConfigLoader.Config?.Debug == true)
            {
                // float f = enchant.SourceStack.Collectible.Attributes?["statModifier"]["rangedWeaponsSpeed"].AsFloat() ?? 0f;
                Api.Logger.Event("[KRPGEnchantment] Applying {0} {1} to {2}", Code, enchant.Power, entity.GetName());
            }
            if (Api.ModLoader.GetModSystem<KRPGEnchantmentSystem>()?.COSysServer != null)
            {
                AddMultipliersCO(ref enchant.SourceSlot, enchant.Power);
                enchant.SourceSlot.MarkDirty();
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
            Entity entity = enchant?.CauseEntity;
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] Removing {0} {1} from {2}.", Code, enchant.Power, entity.GetName());
            // CO saves to Itemstack
            if (Api.ModLoader.GetModSystem<KRPGEnchantmentSystem>()?.COSysServer != null) return;
            // Update entity for Vanilla VS
            RemoveAllMultipliers(entity);
        }
        /// <summary>
        /// Adds multipliers for vanilla VS.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="power"></param>
        void AddMultipliers(Entity entity, int power)
        {
            float mul = power * PowerMultiplier;
            entity.Stats.Set("rangedWeaponsSpeed", "krpge:" + Code, mul, true);
        }
        /// <summary>
        /// Adds multipliers for Combat Overhaul
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="power"></param>
        public void AddMultipliersCO(ref ItemSlot slot, int power)
        {
            // entity.Stats.Set("bowsProficiency", "krpge" + Code, mul, true);
            // entity.Stats.Set("crossbowsProficiency", "krpge" + Code, mul, true);
            // entity.Stats.Set("firearmsProficiency", "krpge" + Code, mul, true);
            float mul = power * COMultiplier;
            float curVal = slot.Itemstack.Attributes.GetFloat("reloadSpeed", 1);
            ITreeAttribute eTree = slot.Itemstack.Attributes.GetOrAddTreeAttribute("enchantments");
            float ogVal = eTree.GetFloat("reloadSpeed", 1);
            if (ogVal != 1)
                curVal = mul + ogVal;
            else
            {
                eTree.SetFloat("reloadSpeed", curVal);
                slot.Itemstack.Attributes.MergeTree(eTree);
                curVal = mul + curVal;
            }
            slot.Itemstack.Attributes.SetFloat("reloadSpeed", curVal);
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
            float curVal = stack.Attributes.GetFloat("reloadSpeed", 1);
            ITreeAttribute eTree = stack.Attributes.GetOrAddTreeAttribute("enchantments");
            float ogVal = eTree.GetFloat("reloadSpeed", 1);
            if (ogVal != 1)
                curVal = mul + ogVal;
            else
            {
                eTree.SetFloat("reloadSpeed", curVal);
                stack.Attributes.MergeTree(eTree);
                curVal = mul + curVal;
            }
            stack.Attributes.SetFloat("reloadSpeed", curVal);
        }
        void RemoveAllMultipliers(Entity entity)
        {
            // Remove all, just in case someone is hot swapping CO between triggers
            // entity.Stats.Remove("bowsProficiency", "krpge" + Code);
            // entity.Stats.Remove("crossbowsProficiency", "krpge" + Code);
            // entity.Stats.Remove("firearmsProficiency", "krpge" + Code);
            entity.Stats.Remove("rangedWeaponsSpeed", "krpge:" + Code);
        }
    }
}
