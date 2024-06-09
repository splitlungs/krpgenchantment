using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KRPGLib.Enchantment
{
    public class KRPGEnchantRecipeConfig
    {
        // Global Config
        public double EnchantTimeOverride = -1d;
        // Compatibility patches
        public bool EnableKRPGWands = false;
        public bool EnablePaxel = false;
        public bool EnableRustboundMagic = false;
        public bool EnableSwordz = false;
        // Version
        public double Version;

        private  bool IsDirty;
        public void MarkDirty()
        {
            if (!IsDirty)
            {
                IsDirty = true;
            }
        }

        internal void Reload(KRPGEnchantRecipeConfig config)
        {
            if (config != null) 
            {
                EnchantTimeOverride = config.EnchantTimeOverride;
                EnableKRPGWands = config.EnableKRPGWands;
                EnablePaxel = config.EnablePaxel;
                EnableRustboundMagic = config.EnableRustboundMagic;
                EnableSwordz = config.EnableSwordz;
            }
        }
    }
}
