using System;
using HarmonyLib;
using System.Reflection;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace KRPGLib.Enchantment
{
    public class KRPGEnchantmentSystem : ModSystem
    {
        private const double ConfigVersion = 0.1d;
        public const string ConfigFile = "KRPGEnchantment_Config.json";

        public static KRPGEnchantConfig Config { get; set; } = null!;
        public ICoreAPI Api;
        public ICoreServerAPI sApi;

        public override void AssetsLoaded(ICoreAPI api)
        {
            if (!(api is ICoreServerAPI sapi)) return;
            this.sApi = sapi;

            try
            {
                Config = sapi.LoadModConfig<KRPGEnchantConfig>(ConfigFile);
                if (Config == null)
                {
                    Config = new KRPGEnchantConfig();
                    Config.Version = ConfigVersion;
                    sapi.StoreModConfig(Config, ConfigFile);

                    sapi.Logger.Event("KRPGEnchantConfig file not found. A new one has been created.");
                }
                else if (Config.Version < ConfigVersion)
                {
                    KRPGEnchantConfig tempConfig = new KRPGEnchantConfig();
                    if (Config.DisabledEnchants?.Count > 0) Config.DisabledEnchants = tempConfig.DisabledEnchants;
                    if (Config.EnableFantasyCreatures) tempConfig.EnableFantasyCreatures = true;
                    if (Config.EnableFeverstoneWilds) tempConfig.EnableFeverstoneWilds = true;
                    if (Config.EnableOutlaws) tempConfig.EnableOutlaws = true;
                    if (Config.EnableRustAndRot) tempConfig.EnableRustAndRot = true;

                    tempConfig.Version = ConfigVersion;
                    Config = tempConfig;
                    sapi.StoreModConfig(Config, ConfigFile);

                    sapi.Logger.Event("KRPGEnchantConfig file is outdated. It has been updated to version {0}.", ConfigVersion);
                }
                else
                    sapi.Logger.Event("KRPGEnchantConfig file found. Loaded successfully.");
            }
            catch (Exception e)
            {
                sapi.Logger.Error("Error loading KRPGEnchantConfig: {0}", e);
                return;
            }
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            sApi = api;
            try
            {
                Config = sApi.LoadModConfig<KRPGEnchantConfig>(ConfigFile);
                if (Config == null)
                {
                    Config = new KRPGEnchantConfig();
                    sApi.StoreModConfig(Config, ConfigFile);

                    sApi.Logger.Event("Loaded KRPGEnchantmentConfig from file.");
                }
            }
            catch (Exception e)
            {
                sApi.Logger.Error("Error loading KRPGEnchantmentConfig: {0}", e);
                return;
            }
            sApi.Event.PlayerNowPlaying += RegisterPlayerEEB;
        }
        public void RegisterPlayerEEB(IServerPlayer byPlayer)
        {
            EnchantmentEntityBehavior eb = byPlayer.Entity.GetBehavior<EnchantmentEntityBehavior>();
            if (eb != null)
                eb.RegisterPlayer(byPlayer);
            else
                sApi.Logger.Warning("No EnchantmentEntityBehavior found on Player {0}", byPlayer.PlayerUID);
        }
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            Api = api;

            api.RegisterCollectibleBehaviorClass("EnchantmentBehavior", typeof(EnchantmentBehavior));
            api.RegisterEntityBehaviorClass("EnchantmentEntityBehavior", typeof(EnchantmentEntityBehavior));
            api.RegisterBlockClass("EnchantingBlock", typeof(EnchantingBlock));
            api.RegisterBlockEntityClass("EnchantingBE", typeof(EnchantingBE));
            api.RegisterItemClass("ReagentItem", typeof(ReagentItem));

            DoHarmonyPatch(api);
            Api.Logger.Event("KRPG Enchantment loaded.");
        }
        private static void DoHarmonyPatch(ICoreAPI api)
        {
            if (KRPGEnchantmentSystem.harmony == null)
            {
                KRPGEnchantmentSystem.harmony = new Harmony("KRPGEnchantmentPatch");
                try
                {
                    harmony.PatchAll(Assembly.GetExecutingAssembly());
                    Console.WriteLine("KRPG Enchantment Harmony patches applied successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception during patching: {ex}");
                }
            }
        }

        private static Harmony harmony;
    }
}
