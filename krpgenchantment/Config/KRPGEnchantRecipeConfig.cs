﻿using System;
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
        public string[] ValidReagents = new string[] { "game:gem-emerald-rough", "game:gem-diamond-rough", "game:olivine_peridot-rough" };
        // Compatibility patches
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
                ValidReagents = config.ValidReagents;
                CustomPatches = config.CustomPatches;
            }
        }
    }
}
