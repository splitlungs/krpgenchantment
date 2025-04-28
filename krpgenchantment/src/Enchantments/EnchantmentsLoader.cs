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
            return 1;
        }
        public override bool ShouldLoad(EnumAppSide side)
        {
            return side == EnumAppSide.Server;
        }
        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);

            api.EnchantAccessor().RegisterEnchantmentClass("chilling", typeof(ChillingEnchantment));
            api.EnchantAccessor().RegisterEnchantmentClass("durable", typeof(DurableEnchantment));
            api.EnchantAccessor().RegisterEnchantmentClass("flaming", typeof(FlamingEnchantment));
            api.EnchantAccessor().RegisterEnchantmentClass("frost", typeof(FrostEnchantment));
            api.EnchantAccessor().RegisterEnchantmentClass("harming", typeof(HarmingEnchantment));
            api.EnchantAccessor().RegisterEnchantmentClass("healing", typeof(HealingEnchantment));
            api.EnchantAccessor().RegisterEnchantmentClass("igniting", typeof(IgnitingEnchantment));
            api.EnchantAccessor().RegisterEnchantmentClass("knockback", typeof(KnockbackEnchantment));
            api.EnchantAccessor().RegisterEnchantmentClass("lightning", typeof(LightningEnchantment));
            api.EnchantAccessor().RegisterEnchantmentClass("pit", typeof(PitEnchantment));
            api.EnchantAccessor().RegisterEnchantmentClass("protection", typeof(ProtectionEnchantment));
            api.EnchantAccessor().RegisterEnchantmentClass("resistelectricity", typeof(ResistElectricityEnchantment));
            api.EnchantAccessor().RegisterEnchantmentClass("resistfire", typeof(ResistFireEnchantment));
            api.EnchantAccessor().RegisterEnchantmentClass("resistfrost", typeof(ResistFrostEnchantment));
            api.EnchantAccessor().RegisterEnchantmentClass("resistheal", typeof(ResistHealEnchantment));
            api.EnchantAccessor().RegisterEnchantmentClass("resistinjury", typeof(ResistInjuryEnchantment));
            api.EnchantAccessor().RegisterEnchantmentClass("resistpoison", typeof(ResistPoisonEnchantment));
            api.EnchantAccessor().RegisterEnchantmentClass("shocking", typeof(ShockingEnchantment));
        }
    }
}
