using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KRPGLib.Enchantment
{
    public class KRPGEnchantConfig
    {
        // Global Options
        public List<string> DisabledEnchants;
        public double EnchantTimeOverride;
        // Compatibility patches
        public bool EnableFantasyCreatures;
        public bool EnableFeverstoneWilds;

        private  bool IsDirty;
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
                EnchantTimeOverride = config.EnchantTimeOverride;
                EnableFantasyCreatures = config.EnableFantasyCreatures;
                EnableFeverstoneWilds = config.EnableFeverstoneWilds;
            }
        }
    }
}
