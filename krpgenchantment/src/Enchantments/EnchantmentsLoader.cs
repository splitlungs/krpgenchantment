using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

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
            // Enchanting Recipes load at 1.0
            return 0.1;
        }
        public override bool ShouldLoad(EnumAppSide side)
        {
            // The server should be authoritative
            return side == EnumAppSide.Server;
        }
        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);

            // Make sure each value is unique when registering your enchantment class, so as to prevent conflicts when adding your own Enchantments.
            // Prefix it with something like "mymod-myenchant" as the className.
            // Enable Debug in the KRPGEnchantment_Config.json to get more info in the Debug log.
            int count = 0;
            if (api.EnchantAccessor().RegisterEnchantmentClass("chilling", "Weapon/chilling.json", typeof(ChillingEnchantment)) == true) count++;
            if (api.EnchantAccessor().RegisterEnchantmentClass("durable", "Universal/durable.json", typeof(DurableEnchantment)) == true) count++;
            if (api.EnchantAccessor().RegisterEnchantmentClass("flaming", "Weapon/flaming.json", typeof(FlamingEnchantment)) == true) count++;
            if (api.EnchantAccessor().RegisterEnchantmentClass("frost", "Weapon/frost.json", typeof(FrostEnchantment)) == true) count++;
            if (api.EnchantAccessor().RegisterEnchantmentClass("harming", "Weapon/harming.json", typeof(HarmingEnchantment)) == true) count++;
            if (api.EnchantAccessor().RegisterEnchantmentClass("healing", "Weapon/healing.json", typeof(HealingEnchantment)) == true) count++;
            if (api.EnchantAccessor().RegisterEnchantmentClass("igniting", "Weapon/igniting.json", typeof(IgnitingEnchantment)) == true) count++;
            if (api.EnchantAccessor().RegisterEnchantmentClass("knockback", "Weapon/knockback.json", typeof(KnockbackEnchantment)) == true) count++;
            if (api.EnchantAccessor().RegisterEnchantmentClass("lightning", "Weapon/lightning.json", typeof(LightningEnchantment)) == true) count++;
            if (api.EnchantAccessor().RegisterEnchantmentClass("pit", "Weapon/pit.json", typeof(PitEnchantment)) == true) count++;
            if (api.EnchantAccessor().RegisterEnchantmentClass("protection", "Armor/protection.json", typeof(ProtectionEnchantment)) == true) count++;
            if (api.EnchantAccessor().RegisterEnchantmentClass("resistelectricity", "Armor/resistelectricity.json", typeof(ResistElectricityEnchantment)) == true) count++;
            if (api.EnchantAccessor().RegisterEnchantmentClass("resistfire", "Armor/resistfire.json", typeof(ResistFireEnchantment)) == true) count++;
            if (api.EnchantAccessor().RegisterEnchantmentClass("resistfrost", "Armor/resistfrost.json", typeof(ResistFrostEnchantment)) == true) count++;
            if (api.EnchantAccessor().RegisterEnchantmentClass("resistheal", "Armor/resistheal.json", typeof(ResistHealEnchantment)) == true) count++;
            if (api.EnchantAccessor().RegisterEnchantmentClass("resistinjury", "Armor/resistinjury.json", typeof(ResistInjuryEnchantment)) == true) count++;
            if (api.EnchantAccessor().RegisterEnchantmentClass("resistpoison", "Armor/resistpoison.json", typeof(ResistPoisonEnchantment)) == true) count++;
            if (api.EnchantAccessor().RegisterEnchantmentClass("shocking", "Weapon/shocking.json", typeof(ShockingEnchantment)) == true) count++;

            api.Logger.Notification("[KRPGEnchantment] Registered {0} Enchantment classes to the EnchantmentRegistry.", count);
        }
    }
}
