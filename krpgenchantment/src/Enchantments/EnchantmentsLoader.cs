﻿using System;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using KRPGLib.Enchantment.API;

namespace KRPGLib.Enchantment
{
    /// <summary>
    /// Loads default Enchantments for KRPGEnchantment mod. Modders should use this as an example for their additions.
    /// </summary>
    public class EnchantmentsLoader : ModSystem
    {
        public override double ExecuteOrder()
        {
            // It's important to load after EnchantmentAccessor, but before EnchantmentRecipes loads. 0.1 - 0.9 is valid.
            return 0.1;
        }
        public override bool ShouldLoad(EnumAppSide side)
        {
            // The server should be authoritative
            return side == EnumAppSide.Server;
        }
        public override void AssetsLoaded(ICoreAPI api)
        {
            // We should only load on the Server
            if (!(api is ICoreServerAPI sapi)) return;

            // Make sure each value is unique when registering your enchantment class, so as to prevent conflicts when adding your own Enchantments.
            // Prefix it with something like "mymod-myenchant" as the className.
            // Enable Debug in the KRPGEnchantment_Config.json to get more info in the Debug log.
            int count = 0;
            if (sapi.EnchantAccessor().RegisterEnchantmentClass("chilling", "Weapon/chilling.json", typeof(ChillingEnchantment)) == true) count++;
            if (sapi.EnchantAccessor().RegisterEnchantmentClass("durable", "Universal/durable.json", typeof(EfficientEnchantment)) == true) count++;
            if (sapi.EnchantAccessor().RegisterEnchantmentClass("flaming", "Weapon/flaming.json", typeof(FlamingEnchantment)) == true) count++;
            if (sapi.EnchantAccessor().RegisterEnchantmentClass("frost", "Weapon/frost.json", typeof(FrostEnchantment)) == true) count++;
            if (sapi.EnchantAccessor().RegisterEnchantmentClass("harming", "Weapon/harming.json", typeof(HarmingEnchantment)) == true) count++;
            if (sapi.EnchantAccessor().RegisterEnchantmentClass("healing", "Weapon/healing.json", typeof(HealingEnchantment)) == true) count++;
            if (sapi.EnchantAccessor().RegisterEnchantmentClass("igniting", "Weapon/igniting.json", typeof(IgnitingEnchantment)) == true) count++;
            if (sapi.EnchantAccessor().RegisterEnchantmentClass("knockback", "Weapon/knockback.json", typeof(KnockbackEnchantment)) == true) count++;
            if (sapi.EnchantAccessor().RegisterEnchantmentClass("lightning", "Weapon/lightning.json", typeof(LightningEnchantment)) == true) count++;
            if (sapi.EnchantAccessor().RegisterEnchantmentClass("pit", "Weapon/pit.json", typeof(PitEnchantment)) == true) count++;
            if (sapi.EnchantAccessor().RegisterEnchantmentClass("poison", "Weapon/poison.json", typeof(PoisonEnchantment)) == true) count++;
            if (sapi.EnchantAccessor().RegisterEnchantmentClass("protection", "Armor/protection.json", typeof(ProtectionEnchantment)) == true) count++;
            if (sapi.EnchantAccessor().RegisterEnchantmentClass("resistelectricity", "Armor/resistelectricity.json", typeof(ResistElectricityEnchantment)) == true) count++;
            if (sapi.EnchantAccessor().RegisterEnchantmentClass("resistfire", "Armor/resistfire.json", typeof(ResistFireEnchantment)) == true) count++;
            if (sapi.EnchantAccessor().RegisterEnchantmentClass("resistfrost", "Armor/resistfrost.json", typeof(ResistFrostEnchantment)) == true) count++;
            if (sapi.EnchantAccessor().RegisterEnchantmentClass("resistheal", "Armor/resistheal.json", typeof(ResistHealEnchantment)) == true) count++;
            if (sapi.EnchantAccessor().RegisterEnchantmentClass("resistinjury", "Armor/resistinjury.json", typeof(ResistInjuryEnchantment)) == true) count++;
            if (sapi.EnchantAccessor().RegisterEnchantmentClass("resistpoison", "Armor/resistpoison.json", typeof(ResistPoisonEnchantment)) == true) count++;
            if (sapi.EnchantAccessor().RegisterEnchantmentClass("reversion", "Universal/reversion.json", typeof(ReversionEnchantment)) == true) count++;
            if (sapi.EnchantAccessor().RegisterEnchantmentClass("shocking", "Weapon/shocking.json", typeof(ShockingEnchantment)) == true) count++;
            // You should modify the notification to your mod
            sapi.Logger.Notification("[KRPGEnchantment] Registered {0} Enchantment classes to the EnchantmentRegistry.", count);
        }
    }
}
