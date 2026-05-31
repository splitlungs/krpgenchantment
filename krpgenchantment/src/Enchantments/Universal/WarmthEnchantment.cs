using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using KRPGLib.Enchantment.API;
using Vintagestory.GameContent;
using Vintagestory.API.Util;
using Vintagestory.API.Config;
using CombatOverhaul.Armor;

namespace KRPGLib.Enchantment
{
    public class WarmthEnchantment : Enchantment
    {
        float PowerMultiplier { get { return Modifiers.GetFloat("PowerMultiplier"); } }
        /// <summary>
        /// Provides hunger rate modifiers the entity who triggers OnEquip.
        /// </summary>
        /// <param name="api"></param>
        public WarmthEnchantment(ICoreAPI api) : base(api)
        {
            // Setup the default config
            Enabled = true;
            Code = "warmth";
            Category = "StatusPersonal";
            LoreCode = "enchantment-warmth";
            LoreChapterID = 30;
            MaxTier = 5;
            ValidToolTypes = new List<string> {
                "Arm", "Emblem", "Neck", "Waist",
                "Armor-Head", "Armor-Body", "Armor-Legs",
                "ArmorHead", "ArmorBody", "ArmorLegs",
            };
            Modifiers = new EnchantModifiers() { {"PowerMultiplier", 1.0f } };
            Version = 1.00f;
        }
        public override bool TryEnchantItem(ref ItemStack inStack, int enchantPower, bool force, ICoreServerAPI api)
        {
            bool didEnchant = base.TryEnchantItem(ref inStack, enchantPower, force, api);
            if (didEnchant)
            {
                float val = enchantPower * PowerMultiplier;
                inStack?.Attributes?.GetOrAddTreeAttribute("enchantments").SetFloat("warmth", val);
                return true;
            }
            return false;
        }
        public override bool TryRemoveEnchant(ItemSlot inSlot, Entity entity)
        {
            bool didEnchant = base.TryRemoveEnchant(inSlot, entity);
            if (didEnchant)
            {
                inSlot?.Itemstack?.Attributes?.GetOrAddTreeAttribute("enchantments").SetFloat("warmth", 0f);
                return true;
            }
            return false;
        }
    }
}
