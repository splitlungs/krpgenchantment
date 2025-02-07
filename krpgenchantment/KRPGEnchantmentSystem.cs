using System;
using HarmonyLib;
using System.Reflection;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using System.Collections.Generic;

namespace KRPGLib.Enchantment
{
    public class KRPGEnchantmentSystem : ModSystem
    {
        public ICoreAPI Api;
        public ICoreServerAPI sApi;
        public COSystem combatOverhaul;

        public override void AssetsLoaded(ICoreAPI api)
        {
            if (!(api is ICoreServerAPI sapi)) return;
            this.sApi = sapi;
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            sApi = api;
            sApi.Event.PlayerNowPlaying += RegisterPlayerEEB;

            if (EnchantingConfigLoader.Config?.CustomPatches.GetValueOrDefault("CombatOverhaul", false) == true 
                && api.ModLoader.IsModEnabled("combatoverhaul") == true)
            {
                combatOverhaul = new COSystem();
                combatOverhaul.StartServerSide(sApi);
            }
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
            api.RegisterItemClass("EnchantersManualItem", typeof(EnchantersManualItem));

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

        public override void Dispose()
        {
            harmony?.UnpatchAll("KRPGEnchantmentPatch");
        }

        private static Harmony harmony;
    }
}
