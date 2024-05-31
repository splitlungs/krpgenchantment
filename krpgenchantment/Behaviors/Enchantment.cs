namespace KRPGLib.Enchantment
{
    /// <summary>
    /// Authoritative list of available Enchantments.
    /// </summary>
    public enum EnumEnchantments { chilling, flaming, frost, harming, healing, knockback, igniting, lightning, pit, shocking }
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
