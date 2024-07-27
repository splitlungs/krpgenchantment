namespace KRPGLib.Enchantment
{
    /// <summary>
    /// Authoritative list of available Enchantments.
    /// </summary>
    public enum EnumEnchantments { aiming, chilling, fast, flaming, frost, harming, healing, knockback, igniting, lightning, pit, protection,
        resistelectric, resistfire, resistfrost, resistheal, resistinjury, resistpoison, running, shocking }
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
