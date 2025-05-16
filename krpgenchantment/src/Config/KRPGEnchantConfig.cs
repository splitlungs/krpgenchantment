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
        // Recipe config.
        public int MaxEnchantsPerItem = 4;
        public double EnchantTimeOverride = -1d;
        public double LatentEnchantResetDays = 7d;
        public int MaxLatentEnchants = 3;
        public Dictionary<string, int> MaxEnchantsByCategory = new Dictionary<string, int>()
        {
            { "ControlArea", -1 },
            { "ControlTarget", -1 },
            { "DamageArea", -1 },
            { "DamageTarget", -1 },
            { "DamageTick", -1 },
            { "ResistDamage", -1 },
            { "Universal", -1 }
        };
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
            { "BlackguardAdditions", true },
            { "CANJewelry", true },
            { "CombatOverhaul", true },
            { "ElectricityAddon", true },
            { "ForlornAdditions", true },
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
                if (config.MaxEnchantsPerItem >= 0) MaxEnchantsPerItem = config.MaxEnchantsPerItem;
                if (config.EnchantTimeOverride >= 0) EnchantTimeOverride = config.EnchantTimeOverride;
                if (config.LatentEnchantResetDays >= 0) LatentEnchantResetDays = config.LatentEnchantResetDays;
                if (config.MaxLatentEnchants != 3) MaxLatentEnchants = config.MaxLatentEnchants;
                
                if (!config.MaxEnchantsByCategory.ContainsKey("Damage"))
                    MaxEnchantsByCategory.Add("Damage", -1);

                if (config.ValidReagents?.Count > 0) ValidReagents = config.ValidReagents;
                if (!config.ValidReagents.ContainsKey("game:gem-emerald-rough"))
                    ValidReagents.Add("game:gem-emerald-rough", 1);
                if (!config.ValidReagents.ContainsKey("game:gem-diamond-rough"))
                    ValidReagents.Add("game:gem-diamond-rough", 1);
                if (!config.ValidReagents.ContainsKey("game:gem-olivine_peridot-rough"))
                    ValidReagents.Add("game:gem-olivine_peridot-rough", 1);

                if (config.ReagentPotentialTiers?.Count > 0) ReagentPotentialTiers = config.ReagentPotentialTiers;
                if (!config.ReagentPotentialTiers.ContainsKey("low"))
                    ReagentPotentialTiers.Add("low", 2);
                if (!config.ReagentPotentialTiers.ContainsKey("medium"))
                    ReagentPotentialTiers.Add("medium", 3);
                if (!config.ReagentPotentialTiers.ContainsKey("high"))
                    ReagentPotentialTiers.Add("high", 5);

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

                if (config.Debug == true) Debug = true;
                
            }
        }
    }
}
