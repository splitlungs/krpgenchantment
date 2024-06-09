using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KRPGLib.Enchantment
{
    public class KRPGEnchantRecipeConfig
    {
        // Compatibility patches
        public bool EnableKRPGWands;
        public bool EnablePaxel;
        public bool EnableRustboundMagic;
        public bool EnableSwordz;

        private  bool IsDirty;
        public void MarkDirty()
        {
            if (!IsDirty)
            {
                IsDirty = true;
            }
        }

        internal void ReloadConfig(KRPGEnchantRecipeConfig config)
        {
            if (config != null) 
            {
                EnableKRPGWands = config.EnableKRPGWands;
                EnablePaxel = config.EnablePaxel;
                EnableRustboundMagic = config.EnableRustboundMagic;
                EnableSwordz = config.EnableSwordz;
            }
        }
    }
}
