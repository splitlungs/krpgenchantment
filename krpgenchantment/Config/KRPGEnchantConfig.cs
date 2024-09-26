using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace KRPGLib.Enchantment
{
    public class KRPGEnchantConfig
    {
        // Global Options
        public List<string> DisabledEnchants;
        public Dictionary<string, int> AmmoEnchants = new Dictionary<string, int>()
        { ["chilling"] = 5, ["flaming"] = 5, ["frost"] = 5, ["harming"] = 5, ["healing"] = 5, ["knockback"] = 5, ["igniting"] = 5, ["lightning"] = 5, ["pit"] = 5, ["shocking"] = 5 };
        public Dictionary<string, int> ArmorEnchants = new Dictionary<string, int>() 
        { ["fast"] = 5, ["protection"] = 5, ["resistelectric"] = 5, ["resistfire"] = 5, ["resistfrost"] = 5, ["resistheal"] = 5, ["resistinjury"] = 5, ["resistpoison"] = 5, ["running"] = 5 };
        public Dictionary<string, int> MeleeEnchants = new Dictionary<string, int>()
        { ["chilling"] = 5, ["flaming"] = 5, ["frost"] = 5, ["harming"] = 5, ["healing"] = 5, ["knockback"] = 5, ["igniting"] = 5, ["lightning"] = 5, ["pit"] = 5, ["shocking"] = 5 };
        public Dictionary<string, int> RangedEnchants = new Dictionary<string, int>()
        { ["aiming"] = 5, ["fast"] = 5 };
        public Dictionary<string, int> ToolEnchants = new Dictionary<string, int>()
        { ["efficient"] = 5, ["productive"] = 5 };
        public Dictionary<string, int> UniversalEnchants = new Dictionary<string, int>()
        { ["durable"] = 5 };
        public Dictionary<string, int> WandEnchants = new Dictionary<string, int>()
        { ["aiming"] = 5, ["chilling"] = 5, ["fast"] = 5, ["flaming"] = 5, ["frost"] = 5, ["harming"] = 5, ["healing"] = 5, ["knockback"] = 5, ["igniting"] = 5, ["lightning"] = 5, ["pit"] = 5, ["shocking"] = 5 };
        // Compatibility patches
        public bool EnableFantasyCreatures;
        public bool EnableFeverstoneWilds;
        public bool EnableOutlaws;
        public bool EnableRustAndRot;
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

        internal void Reload(KRPGEnchantConfig config)
        {
            if (config != null) 
            {
                DisabledEnchants = new List<string>();
                DisabledEnchants = config.DisabledEnchants;
                AmmoEnchants = new Dictionary<string, int>();
                AmmoEnchants = config.AmmoEnchants;
                ArmorEnchants = new Dictionary<string, int>();
                ArmorEnchants = config.ArmorEnchants;
                MeleeEnchants = new Dictionary<string, int>();
                MeleeEnchants = config.MeleeEnchants;
                RangedEnchants = new Dictionary<string, int>();
                RangedEnchants = config.RangedEnchants;
                ToolEnchants = new Dictionary<string, int>();
                ToolEnchants = config.ToolEnchants;
                UniversalEnchants = new Dictionary<string, int>();
                UniversalEnchants = config.UniversalEnchants;
                EnableFantasyCreatures = config.EnableFantasyCreatures;
                EnableFeverstoneWilds = config.EnableFeverstoneWilds;
                EnableOutlaws = config.EnableOutlaws;
                EnableRustAndRot = config.EnableRustAndRot;
            }
        }
    }
}
