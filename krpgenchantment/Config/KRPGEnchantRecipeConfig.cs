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
        public double EnchantResetOverride = -1d;
        public int MaxLatentEnchants = 3;
        public Dictionary<string, int> ValidReagents = new Dictionary<string, int>() 
        {
            { "game:gem-emerald-rough", 8 },
            { "game:gem-diamond-rough", 4 }, 
            { "game:olivine_peridot-rough", 8 } 
        };
        public Dictionary<string, int> ReagentPotentialTiers = new Dictionary<string, int>()
        {
            { "low", 2 }, 
            { "medium", 3 }, 
            { "high", 5 }
        };
        // Compatibility patches
        // Will be removed in 0.6.x
        public bool EnableAncientArmory = false;
        public bool EnableKRPGWands = false;
        public bool EnablePaxel = false;
        public bool EnableRustboundMagic = false;
        public bool EnableSpearExpantion = false;
        public bool EnableSwordz = false;
        // Compatibility patch list
        public Dictionary<string, bool> CustomPatches = new Dictionary<string, bool>() 
        { 
            { "AncientArmory", false }, 
            { "KRPGWands", false }, 
            { "Paxel", false }, 
            { "RustbowndMagic", false }, 
            { "SpearExpantion", false }, 
            { "Swordz", false }
        };
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

        internal void Reload(KRPGEnchantRecipeConfig config)
        {
            if (config != null) 
            {
                EnchantTimeOverride = config.EnchantTimeOverride;
                EnchantResetOverride = config.EnchantResetOverride;
                MaxLatentEnchants = config.MaxLatentEnchants;
                ValidReagents = config.ValidReagents;
                CustomPatches = config.CustomPatches;
            }
        }
    }
}
