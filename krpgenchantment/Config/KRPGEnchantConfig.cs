using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace KRPGLib.Enchantment
{
    public class KRPGEnchantConfig
    {
        // Global Options
        public List<string> DisabledEnchants = new List<string>();
        public int MaxEnchantsPerItem = 4;
        // Recipe Config
        public double EnchantTimeOverride = -1d;
        public double LatentEnchantResetDays = 7d;
        public int MaxLatentEnchants = 3;
        public Dictionary<string, int> ValidReagents = new Dictionary<string, int>()
        {
            { "game:gem-emerald-rough", 3 },
            { "game:gem-diamond-rough", 1 },
            { "game:gem-olivine_peridot-rough", 3 }
        };
        public Dictionary<string, int> ReagentPotentialTiers = new Dictionary<string, int>()
        {
            { "low", 2 },
            { "medium", 3 },
            { "high", 5 }
        };
        // Compatibility patch list
        public Dictionary<string, bool> CustomPatches = new Dictionary<string, bool>()
        {
            { "AncientArmory", true },
            { "Armory", true },
            { "CombatOverhaul", true },
            { "ElectricityAddon", true },
            { "KRPGWands", true },
            { "MaltiezCrossbows", true },
            { "MaltiezFirearms", true },
            { "Paxel", true },
            { "RustboundMagic", true },
            { "ScrapBlocks", true },
            { "SpearExpantion", true },
            { "Swordz", true },
        };
        // Lore Configuration
        public Dictionary<string, int> LoreIDs = new Dictionary<string, int>()
        {
            { "enchantment-chilling", 0 },
            { "enchantment-durable", 1 },
            { "enchantment-flaming", 2 },
            { "enchantment-frost", 3 },
            { "enchantment-harming", 4 },
            { "enchantment-healing", 5 },
            { "enchantment-igniting", 6 },
            { "enchantment-knockback", 7 },
            { "enchantment-lightning", 8 },
            { "enchantment-pit", 9 },
            { "enchantment-protection", 10 },
            { "enchantment-resistelectric", 11 },
            { "enchantment-resistfire", 12 },
            { "enchantment-resistfrost", 13 },
            { "enchantment-resistheal", 14 },
            { "enchantment-resistinjury", 15 },
            { "enchantment-resistpoison", 16 },
            { "enchantment-shocking", 17 }
        };
        // Deboog
        public bool Debug = false;
        // Version
        public double Version;
        // Not supported yet
        private bool IsDirty;
        public void MarkDirty()
        {
            if (!IsDirty)
            {
                IsDirty = true;
            }
        }
        internal void Reload(KRPGEnchantConfig config)
        {
            if (config != null) 
            {
                DisabledEnchants = new List<string>();
                DisabledEnchants = config.DisabledEnchants;
            }
        }
    }
}
