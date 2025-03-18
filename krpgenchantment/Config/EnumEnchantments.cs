namespace KRPGLib.Enchantment
{
    /// <summary>
    /// Authoritative list of available Enchantments.
    /// </summary>
    public enum EnumEnchantments
    {
        aiming, chilling, fast, durable, flaming, frost, harming, healing, knockback, igniting, lightning, pit, protection,
        resistelectricity, resistfire, resistfrost, resistheal, resistinjury, resistpoison, running, shocking
    }

    public enum EnumEnchantArmor
    {
        fast, protection, resistelectricity, resistfire, resistfrost, resistheal, resistinjury, resistpoison, running
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

    public enum EnchantLore
    {
        chilling = 0, durable = 1, flaming = 2, frost = 3, harming = 4, healing = 5, igniting = 6, knockback = 7, lightning = 8, 
        pit = 9, protection = 10, resistelectricity = 11, resistfire = 12, resistfrost = 13, resistheal = 14, resistinjury = 15,
        resistpoison = 16, shocking = 17
    }
}
