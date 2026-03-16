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
            {
                //entity.Stats.Set("steadyAim", "krpge" + Code, enchant.Power * PowerMultiplier, true);
                float curVal = enchant.SourceStack.Attributes.GetFloat("aimingDifficulty", 1);
                ITreeAttribute eTree = enchant.SourceStack.Attributes.GetOrAddTreeAttribute("enchantments");
                float ogVal = eTree.GetFloat("aimingDifficulty", 1);
                if (ogVal != 1)
                    curVal = ogVal - (enchant.Power * PowerMultiplier);
                else
                {
                    eTree.SetFloat("aimingDifficulty", curVal);
                    enchant.SourceStack.Attributes.MergeTree(eTree);
                    curVal = curVal - (enchant.Power * PowerMultiplier);
                }
                enchant.SourceStack.Attributes.SetFloat("aimingDifficulty", curVal);
            }
            else
                entity.Stats.Set("rangedWeaponsAcc", "krpge" + Code, enchant.Power * PowerMultiplier, true);
        }
        void AddMultipliers(Entity entity, ItemStack itemStack, int ePower)
        {
            if (Api.ModLoader.GetModSystem<KRPGEnchantmentSystem>()?.COSysServer != null)
            {
                //entity.Stats.Set("steadyAim", "krpge" + Code, enchant.Power * PowerMultiplier, true);
                float curVal = itemStack.Attributes.GetFloat("aimingDifficulty", 1);
                ITreeAttribute eTree = itemStack.Attributes.GetOrAddTreeAttribute("enchantments");
                float ogVal = eTree.GetFloat("aimingDifficulty", 1);
                if (ogVal != 1)
                    curVal = ogVal - (ePower * PowerMultiplier);
                else
                {
                    eTree.SetFloat("aimingDifficulty", curVal);
                    itemStack.Attributes.MergeTree(eTree);
                    curVal = curVal - (ePower * PowerMultiplier);
                }
                itemStack.Attributes.SetFloat("aimingDifficulty", curVal);
            }
            else
                entity.Stats.Set("rangedWeaponsAcc", "krpge" + Code, ePower * PowerMultiplier, true);
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
