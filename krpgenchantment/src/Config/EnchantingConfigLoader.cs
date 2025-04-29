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
        private const double ConfigVersion = 0.92d;
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
                    // if (Config.MaxEnchantTier >= 0) tempConfig.MaxEnchantTier = Config.MaxEnchantTier;
                    if (Config.MaxEnchantsPerItem >= 0) tempConfig.MaxEnchantsPerItem = Config.MaxEnchantsPerItem;
                    if (Config.EnchantTimeOverride >= 0) tempConfig.EnchantTimeOverride = Config.EnchantTimeOverride;
                    if (Config.LatentEnchantResetDays >= 0) tempConfig.LatentEnchantResetDays = Config.LatentEnchantResetDays;
                    if (Config.MaxLatentEnchants != 3) tempConfig.MaxLatentEnchants = Config.MaxLatentEnchants;
                    if (Config.MaxDamageEnchants != -1) tempConfig.MaxDamageEnchants = Config.MaxDamageEnchants;

                    if (Config.ValidReagents?.Count > 0) tempConfig.ValidReagents = Config.ValidReagents;
                    if (!Config.ValidReagents.ContainsKey("game:gem-emerald-rough"))
                        tempConfig.ValidReagents.Add("game:gem-emerald-rough", 1);
                    if (!Config.ValidReagents.ContainsKey("game:gem-diamond-rough"))
                        tempConfig.ValidReagents.Add("game:gem-diamond-rough", 1);
                    if (!Config.ValidReagents.ContainsKey("game:gem-olivine_peridot-rough"))
                        tempConfig.ValidReagents.Add("game:gem-olivine_peridot-rough", 1);

                    if (Config.CustomPatches?.Count > 0) tempConfig.CustomPatches = Config.CustomPatches;
                    if (!Config.CustomPatches.ContainsKey("AncientArmory"))
                        tempConfig.CustomPatches.Add("AncientArmory", true);
                    if (!Config.CustomPatches.ContainsKey("Armory"))
                        tempConfig.CustomPatches.Add("Armory", true);
                    if (!Config.CustomPatches.ContainsKey("CANJewelry"))
                        tempConfig.CustomPatches.Add("CANJewelry", true);
                    if (!Config.CustomPatches.ContainsKey("CombatOverhaul"))
                        tempConfig.CustomPatches.Add("CombatOverhaul", true);
                    if (!Config.CustomPatches.ContainsKey("ElectricityAddon"))
                        tempConfig.CustomPatches.Add("ElectricityAddon", true);
                    if (!Config.CustomPatches.ContainsKey("KRPGWands"))
                        tempConfig.CustomPatches.Add("KRPGWands", true);
                    if (!Config.CustomPatches.ContainsKey("LitBrig"))
                        tempConfig.CustomPatches.Add("LitBrig", true);
                    if (!Config.CustomPatches.ContainsKey("MaltiezCrossbows"))
                        tempConfig.CustomPatches.Add("MaltiezFirearms", true);
                    if (!Config.CustomPatches.ContainsKey("MaltiezFirearms"))
                        tempConfig.CustomPatches.Add("MaltiezCrossbows", true);
                    if (!Config.CustomPatches.ContainsKey("NDLChiselPick"))
                        tempConfig.CustomPatches.Add("NDLChiselPick", true);
                    if (!Config.CustomPatches.ContainsKey("Paxel"))
                        tempConfig.CustomPatches.Add("Paxel", true);
                    if (!Config.CustomPatches.ContainsKey("RustboundMagic"))
                        tempConfig.CustomPatches.Add("RustboundMagic", true);
                    if (!Config.CustomPatches.ContainsKey("ScrapBlocks"))
                        tempConfig.CustomPatches.Add("ScrapBlocks", true);
                    if (!Config.CustomPatches.ContainsKey("SpearExpantion"))
                        tempConfig.CustomPatches.Add("SpearExpantion", true);
                    if (!Config.CustomPatches.ContainsKey("Swordz"))
                        tempConfig.CustomPatches.Add("Swordz", true);
                    if (!Config.CustomPatches.ContainsKey("Tonwexp-Neue"))
                        tempConfig.CustomPatches.Add("Tonwexp-Neue", true);

                    // Config.LoreIDs = tempConfig.LoreIDs;

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
