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
            Modifiers = new EnchantModifiers() { {"PowerMultiplier", 0.5f } };
            Version = 1.00f;
        }
        public override bool TryEnchantItem(ref ItemStack inStack, int enchantPower, bool force, ICoreServerAPI api)
        {
            bool didEnch = base.TryEnchantItem(ref inStack, enchantPower, force, api);
            if (!didEnch) return false;
            float curVal = inStack.Attributes.GetFloat("reloadSpeed", 1);
            ITreeAttribute eTree = inStack.Attributes.GetOrAddTreeAttribute("enchantments");
            float ogVal = eTree.GetFloat("reloadSpeed", 1);
            if (ogVal != 1)
                curVal = (enchantPower * PowerMultiplier) + ogVal;
            else
            {
                eTree.SetFloat("reloadSpeed", curVal);
                inStack.Attributes.MergeTree(eTree);
                curVal = (enchantPower * PowerMultiplier) + curVal;
            }
            inStack.Attributes.SetFloat("reloadSpeed", curVal);
            return true;
        }
        public override void OnAttackStart(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            Entity entity = enchant?.CauseEntity;
            if (EnchantingConfigLoader.Config?.Debug == true)
            {
                // float f = enchant.SourceStack.Collectible.Attributes?["statModifier"]["rangedWeaponsSpeed"].AsFloat() ?? 0f;
                Api.Logger.Event("[KRPGEnchantment] Applying {0} {1} to {2}", Code, enchant.Power, entity.GetName());
            }
            // Write to entity
            float mul = enchant.Power * PowerMultiplier;
            if (Api.ModLoader.GetModSystem<KRPGEnchantmentSystem>()?.COSysServer != null)
            {
                // entity.Stats.Set("bowsProficiency", "krpge" + Code, mul, true);
                // entity.Stats.Set("crossbowsProficiency", "krpge" + Code, mul, true);
                // entity.Stats.Set("firearmsProficiency", "krpge" + Code, mul, true);
            }
            else
            {
                entity.Stats.Set("rangedWeaponsSpeed", "krpge" + Code, mul, true);
            }
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
            // Remove all, just in case someone is hot swapping CO between triggers
            // entity.Stats.Remove("bowsProficiency", "krpge" + Code);
            // entity.Stats.Remove("crossbowsProficiency", "krpge" + Code);
            // entity.Stats.Remove("firearmsProficiency", "krpge" + Code);
            entity.Stats.Remove("rangedWeaponsSpeed", "krpge" + Code);
        }
    }
}
