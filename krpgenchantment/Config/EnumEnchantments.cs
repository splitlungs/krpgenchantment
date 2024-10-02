namespace KRPGLib.Enchantment
{
    /// <summary>
    /// Authoritative list of available Enchantments.
    /// </summary>
    public enum EnumEnchantments
    {
        aiming, chilling, fast, durable, flaming, frost, harming, healing, knockback, igniting, lightning, pit, protection,
        resistelectric, resistfire, resistfrost, resistheal, resistinjury, resistpoison, running, shocking
    }

    public enum EnumEnchantArmor
    {
        fast, protection, resistelectric, resistfire, resistfrost, resistheal, resistinjury, resistpoison, running
    }
    public enum EnumEnchantTool
    {
        efficient, productive
    }
    public enum EnumEnchantUniversal
    {
        durable
    }
    public enum EnumEnchantMelee
    {
        chilling, flaming, frost, harming, healing, knockback, igniting, lightning, pit, shocking
    }
    public enum EnumEnchantRanged
    {
        aiming, fast
    }
    public enum EnchantTiers { O = 0, I = 1, II = 2, III = 3, IV = 4, V = 5 }
    public enum EnchantColors { white = 0, cyan = 1, green = 2, purple = 3, red = 4, yellow = 5 }
}
