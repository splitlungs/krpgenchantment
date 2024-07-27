using System;
using HarmonyLib;
using System.IO;
using System.Reflection;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace KRPGLib.Enchantment
{
    public class KRPGEnchantmentSystem : ModSystem
    {
        public const string ConfigFile = "KRPGEnchantment_Config.json";
        public KRPGEnchantConfig Config { get; set; }
        public ICoreAPI Api;
        public ICoreServerAPI sApi;
        

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
            sApi.Event.PlayerNowPlaying += OnPlayerNowPlaying;
        }
        public void OnPlayerNowPlaying(IServerPlayer byPlayer)
        {
            EnchantmentEntityBehavior eb = byPlayer.Entity.GetBehavior<EnchantmentEntityBehavior>();
            if (eb != null)
            {
                sApi.Logger.Event("Player {0} has EEBehavior", byPlayer.PlayerUID);
                eb.PlayerUID = byPlayer.PlayerUID;
                byPlayer.Entity.GearInventory.SlotModified += eb.OnGearModified;
            }
            else
                sApi.Logger.Event("No EEBehavior found on Player {0}", byPlayer.PlayerUID);
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
