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
        // public List<string> DisabledEnchants = new List<string>();
        // public int MaxEnchantTier = 5;
        public int MaxEnchantsPerItem = 4;
        // Recipe config.
        public double EnchantTimeOverride = -1d;
        public double LatentEnchantResetDays = 7d;
        public int MaxLatentEnchants = 3;
        public int MaxDamageEnchants = -1;
        public Dictionary<string, int> ValidReagents = new Dictionary<string, int>()
        {
            { "game:gem-emerald-rough", 1 },
            { "game:gem-diamond-rough", 1 },
            { "game:gem-olivine_peridot-rough", 1 }
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
            { "CANJewelry", true },
            { "CombatOverhaul", true },
            { "ElectricityAddon", true },
            { "KRPGWands", true },
            { "LitBrig", true },
            { "MaltiezCrossbows", true },
            { "MaltiezFirearms", true },
            { "NDLChiselPick", true },
            { "Paxel", true },
            { "RustboundMagic", true },
            { "ScrapBlocks", true },
            { "SpearExpantion", true },
            { "Swordz", true },
            { "Tonwexp-Neue", true }
        };
        // Lore Configuration
        // public Dictionary<string, int> LoreIDs = new Dictionary<string, int>()
        // {
        //     { "enchantment-chilling", 0 },
        //     { "enchantment-durable", 1 },
        //     { "enchantment-flaming", 2 },
        //     { "enchantment-frost", 3 },
        //     { "enchantment-harming", 4 },
        //     { "enchantment-healing", 5 },
        //     { "enchantment-igniting", 6 },
        //     { "enchantment-knockback", 7 },
        //     { "enchantment-lightning", 8 },
        //     { "enchantment-pit", 9 },
        //     { "enchantment-protection", 10 },
        //     { "enchantment-resistelectric", 11 },
        //     { "enchantment-resistelectricity", 11 },
        //     { "enchantment-resistfire", 12 },
        //     { "enchantment-resistfrost", 13 },
        //     { "enchantment-resistheal", 14 },
        //     { "enchantment-resistinjury", 15 },
        //     { "enchantment-resistpoison", 16 },
        //     { "enchantment-shocking", 17 }
        // };
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
                // DisabledEnchants = new List<string>();
                // DisabledEnchants = config.DisabledEnchants;
                // if (config.MaxEnchantTier >= 0) MaxEnchantTier = config.MaxEnchantTier;
                if (config.MaxEnchantsPerItem >= 0) MaxEnchantsPerItem = config.MaxEnchantsPerItem;
                if (config.EnchantTimeOverride >= 0) EnchantTimeOverride = config.EnchantTimeOverride;
                if (config.LatentEnchantResetDays >= 0) LatentEnchantResetDays = config.LatentEnchantResetDays;
                if (config.MaxLatentEnchants != 3) MaxLatentEnchants = config.MaxLatentEnchants;
                if (config.MaxDamageEnchants != -1) MaxDamageEnchants = config.MaxDamageEnchants;

                if (config.ValidReagents?.Count > 0) ValidReagents = config.ValidReagents;
                if (!config.ValidReagents.ContainsKey("game:gem-emerald-rough"))
                    ValidReagents.Add("game:gem-emerald-rough", 1);
                if (!config.ValidReagents.ContainsKey("game:gem-diamond-rough"))
                    ValidReagents.Add("game:gem-diamond-rough", 1);
                if (!config.ValidReagents.ContainsKey("game:gem-olivine_peridot-rough"))
                    ValidReagents.Add("game:gem-olivine_peridot-rough", 1);

                if (config.CustomPatches?.Count > 0) CustomPatches = config.CustomPatches;
                if (!config.CustomPatches.ContainsKey("AncientArmory"))
                    CustomPatches.Add("AncientArmory", true);
                if (!config.CustomPatches.ContainsKey("Armory"))
                    CustomPatches.Add("Armory", true);
                if (!config.CustomPatches.ContainsKey("CANJewelry"))
                    CustomPatches.Add("CANJewelry", true);
                if (!config.CustomPatches.ContainsKey("CombatOverhaul"))
                    CustomPatches.Add("CombatOverhaul", true);
                if (!config.CustomPatches.ContainsKey("ElectricityAddon"))
                    CustomPatches.Add("ElectricityAddon", true);
                if (!config.CustomPatches.ContainsKey("KRPGWands"))
                    CustomPatches.Add("KRPGWands", true);
                if (!config.CustomPatches.ContainsKey("LitBrig"))
                    CustomPatches.Add("LitBrig", true);
                if (!config.CustomPatches.ContainsKey("MaltiezCrossbows"))
                    CustomPatches.Add("MaltiezFirearms", true);
                if (!config.CustomPatches.ContainsKey("MaltiezFirearms"))
                    CustomPatches.Add("MaltiezCrossbows", true);
                if (!config.CustomPatches.ContainsKey("NDLChiselPick"))
                    CustomPatches.Add("NDLChiselPick", true);
                if (!config.CustomPatches.ContainsKey("Paxel"))
                    CustomPatches.Add("Paxel", true);
                if (!config.CustomPatches.ContainsKey("RustboundMagic"))
                    CustomPatches.Add("RustboundMagic", true);
                if (!config.CustomPatches.ContainsKey("ScrapBlocks"))
                    CustomPatches.Add("ScrapBlocks", true);
                if (!config.CustomPatches.ContainsKey("SpearExpantion"))
                    CustomPatches.Add("SpearExpantion", true);
                if (!config.CustomPatches.ContainsKey("Swordz"))
                    CustomPatches.Add("Swordz", true);
                if (!config.CustomPatches.ContainsKey("Tonwexp-Neue"))
                    CustomPatches.Add("Tonwexp-Neue", true);

                // config.LoreIDs = LoreIDs;

                if (config.Debug == true) Debug = true;
                
            }
        }
    }
}
