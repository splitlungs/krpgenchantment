namespace KRPGLib.Enchantment
{
    /// <summary>
    /// Authoritative list of available Enchantments.
    /// </summary>
    public enum EnumEnchantments { aiming, chilling, fast, durable, flaming, frost, harming, healing, knockback, igniting, lightning, pit, protection,
        resistelectric, resistfire, resistfrost, resistheal, resistinjury, resistpoison, running, shocking }
    public enum EnchantTiers { O = 0, I = 1, II = 2, III = 3, IV = 4 }
    public enum EnchantColors { white = 0, cyan = 1, green = 2, purple = 3, red = 4, yellow = 5}
    /// <summary>
    /// Not in use right now.
    /// </summary>
    public class Enchantment
    {
        public string Code = "enchantment";
        public string Name = "Enchantment";
        public string Description = "Description of an enchantment.";
        public string Trigger = "attack";
        public string ItemType = "collectible";
        public float Multiplier = 1f;
        public bool Enabled = false;
    }
}
