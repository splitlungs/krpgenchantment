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
    public class MendingEnchantment : Enchantment
    {
        double PowerMultiplier { get { return Modifiers.GetDouble("PowerMultiplier"); } }
        /// <summary>
        /// Rolls a % chance to negate item damage.
        /// </summary>
        /// <param name="api"></param>
        public MendingEnchantment(ICoreAPI api) : base(api)
        {
            // Setup the default config
            Enabled = true;
            Code = "mending";
            Category = "Universal";
            LoreCode = "enchantment-mending";
            LoreChapterID = 19;
            MaxTier = 5;
            ValidToolTypes = new List<string>() {
                "ArmorHead", "ArmorBody", "ArmorLegs",
                "Shield",
                "Chisel", "Cleaver", "Hammer", "Hoe", "Meter", "Pickaxe", "Probe", "Saw", "Scythe", "Shears", "Shovel", "Sickle", "Wrench",
                "Knife", "Axe",
                "Club", "Sword",
                "Spear",
                "Bow", "Sling",
                "Drill",
                "Halberd", "Mace", "Pike", "Polearm", "Poleaxe", "Staff", "Warhammer",
                "Javelin",
                "Crossbow", "Firearm",
                "Wand" };
            Modifiers = new EnchantModifiers { { "PowerMultiplier", 0.10d } };
        }
        public override void OnTick(float deltaTime, ref EnchantTick eTick)
        {
            
        }
    }
}
