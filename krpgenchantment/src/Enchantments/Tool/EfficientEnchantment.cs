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

namespace KRPGLib.Enchantment
{
    public class EfficientEnchantment : Enchantment
    {
        double PowerMultiplier { get { return Modifiers.GetDouble("PowerMultiplier"); } }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="api"></param>
        public EfficientEnchantment(ICoreAPI api) : base(api)
        {
            // Setup the default config
            Enabled = false;
            Code = "efficient";
            Category = "Universal";
            LoreCode = "enchantment-efficient";
            LoreChapterID = 20;
            MaxTier = 5;
            ValidToolTypes = new List<string>() {
                "Chisel", "Cleaver", "Hammer", "Hoe", "Meter", "Pickaxe", "Probe", "Saw", "Scythe", "Shears", "Shovel", "Sickle", "Wrench",
                "Knife", "Axe",
                "Drill",
                };
            Modifiers = new EnchantModifiers { { "PowerMultiplier", 0.10d } };
            Version = 1.00f;
        }
        public override void OnAttack(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            // enchant.SourceSlot.Itemstack
        }
    }
}
