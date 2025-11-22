using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace KRPGLib.Enchantment
{
    public class EnchantingConfigLoader : ModSystem
    {
        public static KRPGEnchantConfig Config { get; set; } = null!;
        public const double ConfigVersion = 1.04d;
        public const string ConfigFile = "KRPGEnchantment/KRPGEnchantment_Config.json";

        // We cannot initialize dictionaries in the Config class, and must do so here
        private Dictionary<string, int> defaultMaxEnchantsByCategory = new Dictionary<string, int>()
        {
            { "ControlArea", -1 },
            { "ControlTarget", -1 },
            { "DamageArea", -1 },
            { "DamageTarget", -1 },
            { "DamageTick", -1 },
            { "HealArea", -1 },
            { "HealTarget", -1 },
            { "HealTick", -1 },
            { "ResistDamage", -1 },
            { "Universal", -1 }
        };
        private Dictionary<string, float> defaultReagentChargeComponents = new Dictionary<string, float>()
        {
            {"game:gear-temporal", 1.0f }
        };
        private Dictionary<string, int> defaultValidReagents = new Dictionary<string, int>()
        {
            { "game:gem-emerald-rough", 1 },
            { "game:gem-diamond-rough", 1 },
            { "game:gem-olivine_peridot-rough", 1 }
        };
        // This is really just a Min-Max of the Reagent's Charge divided by 2 and rounded up to the Charge value.
        private int[,] defaultChargeScales =
        {
            {1,1},
            {1,2},
            {2,3},
            {2,4},
            {3,5}
        };

        private ICoreServerAPI sApi;

        // Load before anything else, especially before ConfigLib does anything.
        public override double ExecuteOrder()
        {
            return 0;
        }

        public override bool ShouldLoad(EnumAppSide side)
        {
            return side == EnumAppSide.Server;
        }

        public override void AssetsLoaded(ICoreAPI api)
        {
            if (!(api is ICoreServerAPI sapi)) return;
            this.sApi = sapi;

            LoadEnchantingConfig();
        }
        /// <summary>
        /// Lods the KRPGEnchantment_Config.json file to static Config. Will attempt to upgrade an old file or make a new one if it's not present.
        /// </summary>
        public void LoadEnchantingConfig()
        {
            try
            {
                Config = sApi.LoadModConfig<KRPGEnchantConfig>(ConfigFile);
                if (Config == null)
                {
                    Config = new KRPGEnchantConfig();
                    Config.Version = ConfigVersion;
                    Config.MaxEnchantsByCategory = new Dictionary<string, int>(defaultMaxEnchantsByCategory);
                    Config.ReagentChargeComponents = new Dictionary<string, float>(defaultReagentChargeComponents);
                    Config.ValidReagents = new Dictionary<string, int>(defaultValidReagents);
                    // Config.ChargeScales = new Dictionary<int, int>(defaultChargeScales);
                    Config.ChargeScales = defaultChargeScales;
                    sApi.StoreModConfig(Config, ConfigFile);

                    sApi.Logger.Warning("[KRPGEnchantment] KRPGEnchantConfig file not found. A new one has been created.");
                }
                else if (Config.Version < ConfigVersion)
                {
                    KRPGEnchantConfig tempConfig = new KRPGEnchantConfig();
                    // Enchant Config
                    if (Config.EntityTickMs != 250) tempConfig.EntityTickMs = Config.EntityTickMs;
                    if (Config.MaxEnchantsPerItem >= 0) tempConfig.MaxEnchantsPerItem = Config.MaxEnchantsPerItem;
                    if (Config.EnchantTimeHours != 1) tempConfig.EnchantTimeHours = Config.EnchantTimeHours;
                    if (Config.LatentEnchantResetDays >= 0) tempConfig.LatentEnchantResetDays = Config.LatentEnchantResetDays;
                    if (Config.MaxLatentEnchants != 3) tempConfig.MaxLatentEnchants = Config.MaxLatentEnchants;

                    // Enchantment Category Limiters - Default
                    if (Config.MaxEnchantsByCategory is null)
                    {
                        tempConfig.MaxEnchantsByCategory = new Dictionary<string, int>(defaultMaxEnchantsByCategory);
                    }
                    else // Enchantment Category Limiters - Update
                    {
                        tempConfig.MaxEnchantsByCategory = Config.MaxEnchantsByCategory;
                        foreach (KeyValuePair<string, int> pair in defaultMaxEnchantsByCategory)
                        {
                            if (Config.MaxEnchantsByCategory.ContainsKey(pair.Key) != true)
                                tempConfig.MaxEnchantsByCategory.Add(pair.Key, pair.Value);
                        }
                    }

                    // Reagent Charge Config
                    if (Config.LegacyReagentPotential == true) tempConfig.LegacyReagentPotential = true;
                    if (Config.ChargeReagentHours != 1) tempConfig.ChargeReagentHours = Config.ChargeReagentHours;
                    if (Config.MaxReagentCharge != 5) tempConfig.MaxReagentCharge = Config.MaxReagentCharge;
                    if (Config.GlobalChargeMultiplier != 1.00) tempConfig.GlobalChargeMultiplier = Config.GlobalChargeMultiplier;

                    // Reagent Charge Components - Default
                    if (Config.ReagentChargeComponents is null)
                    {
                        tempConfig.ReagentChargeComponents = new Dictionary<string, float>(defaultReagentChargeComponents);
                    }
                    else // Reagent Charge Components - Update
                    {
                        tempConfig.ReagentChargeComponents = Config.ReagentChargeComponents;
                        foreach (KeyValuePair<string, float> pair in defaultReagentChargeComponents)
                        {
                            if (Config.ReagentChargeComponents.ContainsKey(pair.Key) != true)
                                tempConfig.ReagentChargeComponents.Add(pair.Key, pair.Value);
                        }
                    }

                    // Valid Reagents - Default
                    if (Config.ValidReagents is null)
                    {
                        tempConfig.ValidReagents = new Dictionary<string, int>(defaultValidReagents);
                    }
                    else // Valid Reagents - Update
                    {
                        tempConfig.ValidReagents = new Dictionary<string, int>(Config.ValidReagents);
                        foreach (KeyValuePair<string, int> pair in defaultValidReagents)
                        {
                            if (Config.ValidReagents.ContainsKey(pair.Key) != true)
                                tempConfig.ValidReagents.Add(pair.Key, pair.Value);
                        }
                    }

                    // Charge to Enchant Tier Scales - Default
                    if (Config.ChargeScales is null)
                    {
                        tempConfig.ChargeScales = defaultChargeScales;
                    }
                    // Charge Scales - Update ONLY IF they don't qualify for their current MaxCharge
                    else if (tempConfig.ChargeScales.Length < tempConfig.MaxReagentCharge)
                    {
                        tempConfig.ChargeScales = Config.ChargeScales;
                        if (tempConfig.ChargeScales.Length < tempConfig.MaxReagentCharge)
                        {
                            for (int i = tempConfig.ChargeScales.Length; i < tempConfig.MaxReagentCharge; i++)
                            {
                                int j = (int)MathF.Ceiling((i / 2));
                                tempConfig.ChargeScales[i, 0] = j; // Min
                                tempConfig.ChargeScales[i, 1] = i; // Max
                            }
                        }
                    }
                    // Charge Scales - Leave it alone if it's valid
                    else if (tempConfig.ChargeScales.Length == tempConfig.MaxReagentCharge)
                    {
                        tempConfig.ChargeScales = Config.ChargeScales;
                    }
                    // Charge Scales - How did we get here? idk, this is just a fall-back if something goes horribly wrong
                    else
                        tempConfig.ChargeScales = defaultChargeScales;

                    // Force reset if they haven't done Enchantments 1.2.5 upgrade yet
                    if (Config.Version < 1.01) tempConfig.ResetEnchantConfigs = true;
                    else if (tempConfig.ResetEnchantConfigs == true) tempConfig.ResetEnchantConfigs = true;
                    
                    // System Options
                    if (Config.Debug == true) tempConfig.Debug = true;
                    tempConfig.Version = ConfigVersion;
                    Config = tempConfig;

                    // Write back to JSON
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
        }
        /// <summary>
        /// Reloads the config from the server, then notifies ConfigReloaded delegates.
        /// </summary>
        /// <returns></returns>
        public bool ReloadConfig()
        {
            sApi.Logger.Warning("[KRPGEnchantment] KRPGEnchantConfig file is being reloaded from JSON.");
            LoadEnchantingConfig();
            // Notify the masses
            ConfigReloaded.Invoke();

            return true;
        }
        // We don't have any subscribers *just yet*, but exists for when I update config management again
        public delegate void ConfigReloadDelegate();
        public event ConfigReloadDelegate ConfigReloaded;
    }
}
