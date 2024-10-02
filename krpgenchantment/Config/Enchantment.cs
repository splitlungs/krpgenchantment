using System.Collections.Generic;

namespace KRPGLib.Enchantment
{
    /// <summary>
    /// Not in use right now.
    /// </summary>
    public class Enchantment
    {
        public string Code = "enchantment";
        public string Name = "Enchantment";
        public string Description = "Description of an enchantment.";
        public string Trigger = "attack";
        public string[] ItemType = new string[] { "all" };
        public int MaxTier = 5;
        public float Multiplier = 1f;
        public bool Enabled = false;

        public string[] MeleeTools = new string[]
        { "Axe", "Knife", "Spear", "Sword" };

        public string[] AmmoTools = new string[]
        { "Spear" };

        public string[] tools = new string[]
        { "Knife", "Pickaxe", "Axe", "Sword", "Shovel", "Hammer", "Spear", "Bow", "Shears", "Sickle", "Hoe", "Saw", "Chisel", "Scythe", "Sling", "Wrench",
            "Probe", "Meter", "Drill" };

        public Dictionary<string, int> EnchantTiers = new Dictionary<string, int>()
        {
            ["aiming"] = 5,
            ["chilling"] = 5,
            ["durable"] = 5,
            ["efficient"] = 5,
            ["fast"] = 5,
            ["flaming"] = 5,
            ["frost"] = 5,
            ["harming"] = 5,
            ["healing"] = 5,
            ["knockback"] = 5,
            ["igniting"] = 5,
            ["lightning"] = 5,
            ["pit"] = 5,
            ["productive"] = 5,
            ["protection"] = 5,
            ["resistelectric"] = 5,
            ["resistfire"] = 5,
            ["resistfrost"] = 5,
            ["resistheal"] = 5,
            ["resistinjury"] = 5,
            ["resistpoison"] = 5,
            ["running"] = 5,
            ["shocking"] = 5
        };

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

    }
}
