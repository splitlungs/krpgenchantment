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
using Vintagestory.API.Config;
using Vintagestory.API.Client;
using Vintagestory.API.Server;

namespace KRPGLib.Enchantment
{
    public class LightEnchantment : Enchantment
    {
        double PowerMultiplier { get { return Modifiers.GetDouble("PowerMultiplier"); } }
        int DefaultHue { get { return Modifiers.GetInt("DefaultHue"); } }
        int DefaultSaturation { get { return Modifiers.GetInt("DefaultSaturation"); } }
        bool RandomizeHue { get { return Modifiers.GetBool("RandomizeHue"); } }
        bool RandomizeSaturation { get { return Modifiers.GetBool("RandomizeSaturation"); } }
        /// <summary>
        /// Rolls a % chance to negate item damage.
        /// </summary>
        /// <param name="api"></param>
        public LightEnchantment(ICoreAPI api) : base(api)
        {
            // Setup the default config
            Enabled = true;
            Code = "light";
            Category = "Universal";
            LoreCode = "enchantment-light";
            LoreChapterID = 28;
            MaxTier = 5;
            ValidToolTypes = new List<string>() {
                "Arm", "Emblem", "Neck", "Waist",
                "Armor-Head", "Armor-Body", "Armor-Legs",
                "ArmorHead", "ArmorBody", "ArmorLegs",
                "Shield",
                "Chisel", "Cleaver", "Hammer", "Hoe", "Meter", "Pickaxe", "Probe", "Saw", "Scythe", "Shears", "Shovel", "Sickle", "Wrench",
                "Knife", "Axe",
                "Club", "Sword",
                "Spear",
                "Bow", "Sling",
                "Drill",
                "Halberd", "Mace", "Pike", "Polearm", "Poleaxe", "Quarterstaff", "Sabre", "Staff", "Warhammer",
                "Javelin",
                "Crossbow", "Firearm",
                "Wand",
                "vanillaarmory:Buckler", "vanillaarmory:Forlorn", "vanillaarmory:Club"
            };
            Modifiers = new EnchantModifiers 
            { 
                { "PowerMultiplier", 4.00d }, { "DefaultHue", 32 }, { "DefaultSaturation", 5 }, { "RandomizeHue", true }, { "RandomizeSaturation", true } 
            };
            Version = 1.00f;
        }
        public byte[] GetHSV(int power)
        {
            int hue = DefaultHue;
            int sat = DefaultSaturation;
            int val = (int)(power * PowerMultiplier);
            if (RandomizeHue)
                hue = Api.World.Rand.Next(64);
            if (RandomizeSaturation)
                sat = Api.World.Rand.Next(8);
            return new byte[3] {(byte)hue, (byte)sat, (byte)val};
        }
        public override bool TryEnchantItem(ref ItemStack inStack, int enchantPower, bool force, ICoreServerAPI api)
        {
            bool didEnchant = base.TryEnchantItem(ref inStack, enchantPower, force, api);
            if (didEnchant)
            {
                byte[] b = GetHSV(enchantPower);
                inStack?.Attributes?.SetBytes("lightHsv", b);
                return true;
            }
            return false;
        }
        public override bool TryRemoveEnchant(ItemSlot inSlot, Entity entity)
        {
            bool didEnchant = base.TryRemoveEnchant(inSlot, entity);
            if (didEnchant)
            {
                inSlot?.Itemstack?.Attributes?.SetBytes("lightHsv", [0,0,0]);
                return true;
            }
            return false;
        }
        public override void OnEquip(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] {0} is being affected by a {1} enchantment.", enchant.TargetEntity.GetName(), Code);
            
            if (parameters.GetBool("IsHotbar") == true) return;

            Entity entity = enchant?.SourceEntity;
            byte[] b1 = entity.WatchedAttributes.GetOrAddTreeAttribute("enchantments")?.GetBytes("lightHsv", null);
            byte[] b2 = enchant.SourceStack.Attributes.GetBytes("lightHsv", null);
            if (b2 == null) return;
            if (b1?[2] > b2[2]) return;
            entity.WatchedAttributes.GetTreeAttribute("enchantments")?.SetBytes("lightHsv", b2);
            entity.WatchedAttributes.MarkPathDirty("enchantments/lightHsv");
    
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] {0} successfully applied {1} enchantment.", enchant.TargetEntity.GetName(), Code);
        }
        public override void OnUnEquip(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] {0} is being affected by a {1} enchantment.", enchant.TargetEntity.GetName(), Code);
            if (parameters.GetBool("IsHotbar") == true) return;
            
            Entity entity = enchant?.SourceEntity;
            EnchantmentEntityBehavior eeb = entity.GetBehavior<EnchantmentEntityBehavior>();
            int power = 0;
            byte[] b = null;
            foreach (KeyValuePair<int, ActiveEnchantCache> pair in eeb.GearEnchantCache)
            {
                int p = 0;
                if (pair.Value?.Enchantments?.TryGetValue("light", out p) == true)
                {
                    if (p > power) power = p;
                }
                if (p > 0)
                {
                    b = eeb.gearInventory[pair.Key]?.Itemstack?.Attributes?.GetBytes("lightHsv", null);
                }
            }
            if (b != null)
                entity.WatchedAttributes.GetTreeAttribute("enchantments")?.SetBytes("lightHsv", b);
            else
                entity.WatchedAttributes.GetTreeAttribute("enchantments")?.SetBytes("lightHsv", [0, 0, 0]);
            entity.WatchedAttributes.MarkPathDirty("enchantments/lightHsv");
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] {0} successfully removed {1} enchantment.", enchant.TargetEntity.GetName(), Code);
        }
    }
}