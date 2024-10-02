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
        
        // Compatibility patches
        public bool EnableFantasyCreatures;
        public bool EnableFeverstoneWilds;
        public bool EnableOutlaws;
        public bool EnableRustAndRot;
        // Version
        public double Version;

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
                EnableFantasyCreatures = config.EnableFantasyCreatures;
                EnableFeverstoneWilds = config.EnableFeverstoneWilds;
                EnableOutlaws = config.EnableOutlaws;
                EnableRustAndRot = config.EnableRustAndRot;
            }
        }
    }
}
