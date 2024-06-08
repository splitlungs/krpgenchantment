using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KRPGLib.Enchantment
{
    public class KRPGEnchantmentConfig
    {
        // Global Options
        public List<string> DisabledEnchants;
        public double EnchantTimeOverride;
        // Compatibility patches
        public bool EnableFantasyCreatures;
        public bool EnableFeverstoneWilds;
        public bool EnableKRPGWands;
        public bool EnablePaxel;
        public bool EnableRustboundMagic;
        public bool EnableSwordz;

        public bool IsDirty;
        public void MarkDirty()
        {
            if (!IsDirty)
            {
                IsDirty = true;
            }
        }

    }
}
