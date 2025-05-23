using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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
    public class EnchantingConfigLoader : ModSystem
    {
        private const double ConfigVersion = 1.00d;
        public const string ConfigFile = "KRPGEnchantment/KRPGEnchantment_Config.json";
        public static KRPGEnchantConfig Config { get; set; } = null!;

        ICoreServerAPI sApi;

        // Load before anything else, especially before ConfigLib does anything.
        public override double ExecuteOrder()
        {
            return 0;
        }

        public override bool ShouldLoad(EnumAppSide side)
        {
            return side == EnumAppSide.Server;
        }

        bool classExclusiveRecipes = true;

        public override void AssetsLoaded(ICoreAPI api)
        {
            if (!(api is ICoreServerAPI sapi)) return;
            this.sApi = sapi;

            LoadEnchantingConfig();
        }

        public void LoadEnchantingConfig()
        {
            try
            {
                Config = sApi.LoadModConfig<KRPGEnchantConfig>(ConfigFile);
                if (Config == null)
                {
                    Config = new KRPGEnchantConfig();
                    Config.Version = ConfigVersion;
                    sApi.StoreModConfig(Config, ConfigFile);

                    sApi.Logger.Warning("[KRPGEnchantment] KRPGEnchantConfig file not found. A new one has been created.");
                }
                else if (Config.Version < ConfigVersion)
                {
                    KRPGEnchantConfig tempConfig = new KRPGEnchantConfig();
                    // Enchant Config
                    if (Config.MaxEnchantsPerItem >= 0) tempConfig.MaxEnchantsPerItem = Config.MaxEnchantsPerItem;
                    if (Config.EnchantTimeHours != 1) tempConfig.EnchantTimeHours = Config.EnchantTimeHours;
                    if (Config.LatentEnchantResetDays >= 0) tempConfig.LatentEnchantResetDays = Config.LatentEnchantResetDays;
                    if (Config.MaxLatentEnchants != 3) tempConfig.MaxLatentEnchants = Config.MaxLatentEnchants;

                    if (Config.MaxEnchantsByCategory?.Count > 0) tempConfig.MaxEnchantsByCategory = Config.MaxEnchantsByCategory;
                    if (!Config.MaxEnchantsByCategory.ContainsKey("ControlArea"))
                        tempConfig.MaxEnchantsByCategory.Add("ControlArea", -1);
                    if (!Config.MaxEnchantsByCategory.ContainsKey("ControlTarget"))
                        tempConfig.MaxEnchantsByCategory.Add("ControlTarget", -1);
                    if (!Config.MaxEnchantsByCategory.ContainsKey("DamageArea")) 
                        tempConfig.MaxEnchantsByCategory.Add("DamageArea", -1);
                    if (!Config.MaxEnchantsByCategory.ContainsKey("DamageTarget"))
                        tempConfig.MaxEnchantsByCategory.Add("DamageTarget", -1);
                    if (!Config.MaxEnchantsByCategory.ContainsKey("DamageTick"))
                        tempConfig.MaxEnchantsByCategory.Add("DamageTick", -1);
                    if (!Config.MaxEnchantsByCategory.ContainsKey("ResistDamage"))
                        tempConfig.MaxEnchantsByCategory.Add("ResistDamage", -1);
                    if (!Config.MaxEnchantsByCategory.ContainsKey("Universal"))
                        tempConfig.MaxEnchantsByCategory.Add("Universal", -1);
                    // ReagentConfig
                    if (Config.LegacyReagentPotential == true) tempConfig.LegacyReagentPotential = true;
                    if (Config.ChargeReagentHours != 1) tempConfig.ChargeReagentHours = Config.ChargeReagentHours;
                    if (Config.MaxReagentCharge != 5) tempConfig.MaxReagentCharge = Config.MaxReagentCharge;
                    if (Config.ChargePerGear != 1.00) tempConfig.ChargePerGear = Config.ChargePerGear;

                    if (Config.ValidReagents?.Count > 0) tempConfig.ValidReagents = Config.ValidReagents;
                    if (!Config.ValidReagents.ContainsKey("gem-emerald-rough"))
                        tempConfig.ValidReagents.Add("gem-emerald-rough", 1);
                    if (!Config.ValidReagents.ContainsKey("gem-diamond-rough"))
                        tempConfig.ValidReagents.Add("gem-diamond-rough", 1);
                    if (!Config.ValidReagents.ContainsKey("gem-olivine_peridot-rough"))
                        tempConfig.ValidReagents.Add("gem-olivine_peridot-rough", 1);

                    if (Config.Debug == true) tempConfig.Debug = true;
                    tempConfig.Version = ConfigVersion;
                    Config = tempConfig;
                    sApi.StoreModConfig(Config, ConfigFile);

                    sApi.Logger.Warning("[KRPGEnchantment] KRPGEnchantConfig file is outdated. Migrated to version {0} successfully.", ConfigVersion);
                }
                else
                    sApi.Logger.Notification("[KRPGEnchantment] KRPGEnchantConfig file found. Loaded successfully.");
            }
            catch (Exception e)
            {
                sApi.Logger.Error("[KRPGEnchantment] Error loading KRPGEnchantConfig: {0}", e);
                return;
            }
            classExclusiveRecipes = sApi.World.Config.GetBool("classExclusiveRecipes", true);
        }


        public override void StartServerSide(ICoreServerAPI api)
        {
            api.ChatCommands.GetOrCreate("krpg")
            .WithDescription(Lang.Get("krpgenchantment:dsc-cmd-krpg"))
            .RequiresPrivilege(Privilege.controlserver)
            .BeginSubCommand("enchantment")
            .WithDescription(Lang.Get("krpgenchantment:dsc-cmd-enchantment"))
            .RequiresPrivilege(Privilege.controlserver)
            .BeginSubCommand("reload")
            .WithDescription(Lang.Get("krpgenchantment:dsc-cmd-reload-config"))
            .RequiresPrivilege(Privilege.controlserver)
            .HandleWith(_ =>
            {
                if (ReloadConfig())
                {
                    return TextCommandResult.Success(Lang.Get("krpgenchantment:cmd-reloadcfg-msg"));
                }

                return TextCommandResult.Error(Lang.Get("krpgenchantment:cmd-reloadcfg-fail"));
            })
            .EndSubCommand()
            .EndSubCommand()
            .Validate();
        }
        private bool ReloadConfig()
        {
            try
            {
                var configTemp = sApi.LoadModConfig<KRPGEnchantConfig>(ConfigFile);
                Config.Reload(configTemp);

                Config.Version = ConfigVersion;
                sApi.StoreModConfig(Config, ConfigFile);
                sApi.Logger.Warning("[KRPGEnchantment] KRPGEnchantConfig file is outdated. Migrated to version {0} successfully.", ConfigVersion);
            }
            catch (Exception e)
            {
                sApi.Logger.Error("[KRPGEnchantment] Error reloading KRPGEnchantment Recipe Config: ", e.ToString());
                return false;
            }

            return true;
        }

    }
}
