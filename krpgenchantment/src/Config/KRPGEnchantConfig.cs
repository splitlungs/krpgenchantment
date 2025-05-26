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
        // Enchantment Config
        public int MaxEnchantsPerItem = 4;
        public double EnchantTimeHours = 1d;
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
        // Reagent Config
        public bool LegacyReagentPotential = false;
        public double ChargeReagentHours = 1d;
        public int MaxReagentCharge = 5;
        public float ChargePerGear = 1.00f;
        public Dictionary<string, int> ValidReagents = new Dictionary<string, int>()
        {
            { "game:gem-emerald-rough", 1 },
            { "game:gem-diamond-rough", 1 },
            { "game:gem-olivine_peridot-rough", 1 }
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
                // Enchant Config
                if (config.MaxEnchantsPerItem >= 0) MaxEnchantsPerItem = config.MaxEnchantsPerItem;
                if (config.EnchantTimeHours != 1) EnchantTimeHours = config.EnchantTimeHours;
                if (config.LatentEnchantResetDays >= 0) LatentEnchantResetDays = config.LatentEnchantResetDays;
                if (config.MaxLatentEnchants != 3) MaxLatentEnchants = config.MaxLatentEnchants;

                if (!config.MaxEnchantsByCategory.ContainsKey("ControlArea"))
                    MaxEnchantsByCategory.Add("ControlArea", -1);
                if (!config.MaxEnchantsByCategory.ContainsKey("ControlArea"))
                    MaxEnchantsByCategory.Add("ControlArea", -1);
                if (!config.MaxEnchantsByCategory.ContainsKey("ControlArea"))
                    MaxEnchantsByCategory.Add("ControlArea", -1);
                if (!config.MaxEnchantsByCategory.ContainsKey("ControlArea"))
                    MaxEnchantsByCategory.Add("ControlArea", -1);
                if (!config.MaxEnchantsByCategory.ContainsKey("ControlArea"))
                    MaxEnchantsByCategory.Add("ControlArea", -1);
                if (!config.MaxEnchantsByCategory.ContainsKey("ControlArea"))
                    MaxEnchantsByCategory.Add("ControlArea", -1);
                if (!config.MaxEnchantsByCategory.ContainsKey("ControlArea"))
                    MaxEnchantsByCategory.Add("ControlArea", -1);
                // Reagent Config
                if (config.LegacyReagentPotential == true) LegacyReagentPotential = true;
                if (config.ChargeReagentHours != 1) ChargeReagentHours = config.ChargeReagentHours;
                if (config.MaxReagentCharge != 5) MaxReagentCharge = config.MaxReagentCharge;
                if (config.ChargePerGear != 1.00) ChargePerGear = config.ChargePerGear;

                if (config.ValidReagents?.Count > 0) ValidReagents = config.ValidReagents;
                if (!config.ValidReagents.ContainsKey("game:gem-emerald-rough"))
                    ValidReagents.Add("game:gem-emerald-rough", 1);
                if (!config.ValidReagents.ContainsKey("game:gem-diamond-rough"))
                    ValidReagents.Add("game:gem-diamond-rough", 1);
                if (!config.ValidReagents.ContainsKey("game:gem-olivine_peridot-rough"))
                    ValidReagents.Add("game:gem-olivine_peridot-rough", 1);

                if (config.Debug == true) Debug = true;
                
            }
        }
    }
}
