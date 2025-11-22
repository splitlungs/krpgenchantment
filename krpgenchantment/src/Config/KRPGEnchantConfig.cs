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
    /// <summary>
    /// Defines all possible configurations options in the primary config file.
    /// </summary>
    public class KRPGEnchantConfig
    {
        // Enchantment Config
        public int EntityTickMs = 250;
        public int MaxEnchantsPerItem = 4;
        public double EnchantTimeHours = 1d;
        public double LatentEnchantResetDays = 7d;
        public int MaxLatentEnchants = 3;
        public Dictionary<string, int> MaxEnchantsByCategory;
        // Reagent Config
        public bool LegacyReagentPotential = false;
        public double ChargeReagentHours = 1d;
        public int MaxReagentCharge = 5;
        public float GlobalChargeMultiplier = 1.00f;
        public Dictionary<string, float> ReagentChargeComponents;
        public Dictionary<string, int> ValidReagents;
        public int[,] ChargeScales;
        // Forces the Enchantment class loader to reload all configs from default
        public bool ResetEnchantConfigs = true;
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
    }
}
