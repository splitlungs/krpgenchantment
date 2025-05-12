using System;
using HarmonyLib;
using System.Reflection;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using System.Collections.Generic;
using Vintagestory.API.Common.Entities;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Util;
using Vintagestory.API.Client;
using System.IO;
using System.Runtime.CompilerServices;
using KRPGLib.Enchantment.API;
using SkiaSharp;
using System.Net.Http;
using Vintagestory.API.Datastructures;

namespace KRPGLib.Enchantment
{
    public class KRPGEnchantmentSystem : ModSystem
    {
        public ICoreAPI Api;
        public ICoreClientAPI cApi;
        public ICoreServerAPI sApi;
        public IWorldAccessor world;
        /// <summary>
        /// Primary API for all Enchantment tasks
        /// </summary>
        public EnchantmentAccessor EnchantAccessor { get; private set; }
        /// <summary>
        /// All Enchantments are processed and stored here. Must use RegisterEnchantmentClass to handle adding Enchantments.
        /// </summary>
        // public Dictionary<string, Enchantment> EnchantmentRegistry = new Dictionary<string, Enchantment>();
        // private Dictionary<string, Type> EnchantCodeToTypeMapping = new Dictionary<string, Type>();
        private static Harmony harmony;
        private COSystem combatOverhaul;
        private KRPGWandsSystem krpgWands;

        #region ModSystem & Setup
        // public override void AssetsLoaded(ICoreAPI api)
        // {
        //     if (!(api is ICoreServerAPI sapi)) return;
        //     this.sApi = sapi;
        // }
        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);
            Api = api;
            cApi = api as ICoreClientAPI;
            sApi = api as ICoreServerAPI;
            EnchantAccessor = new EnchantmentAccessor();
            EnchantAccessor.Api = api;
            EnchantAccessor.cApi = cApi;
            EnchantAccessor.sApi = sApi;
            EnchantAccessor.EnchantmentRegistry = new Dictionary<string, Enchantment>();
        }
        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            cApi = api;
            EnchantAccessor.cApi = api;
        }
        public override void StartServerSide(ICoreServerAPI api)
        {
            sApi = api;
            EnchantAccessor.sApi = api;
            sApi.Event.PlayerNowPlaying += RegisterPlayerEEB;
            RegisterCompatibility();
        }
        private void RegisterCompatibility()
        {
            if (EnchantingConfigLoader.Config?.CustomPatches.GetValueOrDefault("CombatOverhaul", false) == true
            && sApi.ModLoader.IsModEnabled("combatoverhaul") == true)
            {
                combatOverhaul = new COSystem();
                combatOverhaul.StartServerSide(Api);
            }
            if (EnchantingConfigLoader.Config?.CustomPatches.GetValueOrDefault("KRPGWands", false) == true
                && sApi.ModLoader.IsModEnabled("krpgwands") == true)
            {
                krpgWands = new KRPGWandsSystem();
                krpgWands.StartServerSide(Api);
            }
        }
        public void RegisterPlayerEEB(IServerPlayer byPlayer)
        {
            EnchantmentEntityBehavior eb = byPlayer.Entity.GetBehavior<EnchantmentEntityBehavior>();
            if (eb != null)
                eb.RegisterPlayer(byPlayer);
            else
                sApi.Logger.Warning("[KRPGEnchantment] No EnchantmentEntityBehavior found on Player {0}.", byPlayer.PlayerUID);
        }
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            Api = api;
            EnchantAccessor.Api = api;

            api.RegisterCollectibleBehaviorClass("ReagentBehavior", typeof(ReagentBehavior));
            api.RegisterCollectibleBehaviorClass("EnchantmentBehavior", typeof(EnchantmentBehavior));
            api.RegisterEntityBehaviorClass("EnchantmentEntityBehavior", typeof(EnchantmentEntityBehavior));
            api.RegisterBlockClass("EnchantingBlock", typeof(EnchantingBlock));
            api.RegisterBlockEntityClass("EnchantingBE", typeof(EnchantingBE));
            api.RegisterItemClass("EnchantersManualItem", typeof(EnchantersManualItem));

            DoHarmonyPatch(api);
            Api.Logger.Notification("[KRPGEnchantment] KRPG Enchantment loaded.");
        }
        private static void DoHarmonyPatch(ICoreAPI api)
        {
            if (KRPGEnchantmentSystem.harmony == null)
            {
                KRPGEnchantmentSystem.harmony = new Harmony("KRPGEnchantmentPatch");
                try
                {
                    harmony.PatchAll(Assembly.GetExecutingAssembly());
                    Console.WriteLine("[KRPGEnchantment] KRPG Enchantment Harmony patches applied successfully.");
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
        #endregion
    }
}