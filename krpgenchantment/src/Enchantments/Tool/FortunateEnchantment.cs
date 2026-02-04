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
using Vintagestory.API.Server;

namespace KRPGLib.Enchantment
{
    public class FortunateEnchantment : Enchantment
    {
        float PowerMultiplier { get { return Modifiers.GetFloat("PowerMultiplier"); } }
        /// <summary>
        /// Increases the drops of a block broken with the enchanted item.
        /// </summary>
        /// <param name="api"></param>
        public FortunateEnchantment(ICoreAPI api) : base(api)
        {
            // Setup the default config
            Enabled = true;
            Code = "fortunate";
            Category = "Gathering";
            LoreCode = "enchantment-fortunate";
            LoreChapterID = 21;
            MaxTier = 5;
            ValidToolTypes = new List<string>() {
                "Cleaver", "Hoe", "Pickaxe", "Scythe", "Shears", "Shovel", "Sickle",
                "Knife", "Axe",
                "Drill"
                };
            Modifiers = new EnchantModifiers { { "PowerMultiplier", 1.0f } };
            Version = 1.00f;
        }
        // There isn't really much to do here. EnchantmentBehavior handles it, as configured by the OnLoaded override.
    }
}
