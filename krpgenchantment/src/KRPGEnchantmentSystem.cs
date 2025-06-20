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
using System.Linq;

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
        private static Harmony harmony;
        private COSystem combatOverhaul;
        private KRPGWandsSystem krpgWands;

        public override void AssetsFinalize(ICoreAPI api)
        {
            base.AssetsFinalize(api);
            
            if (api.Side != EnumAppSide.Server) return;

            // Setup ENchantment Behaviors on ALL collectibles
            foreach (CollectibleObject obj in api.World.Collectibles)
            {
                bool foundRB = false;
                if (EnchantingConfigLoader.Config.ValidReagents.ContainsKey(obj.Code)) foundRB = true;
                bool foundEB = false;
                foreach (var behavior in obj.CollectibleBehaviors)
                {
                    if (behavior.GetType() == (typeof(EnchantmentBehavior)))
                        foundEB = true;
                    if (foundEB ==true && foundRB == true) ((EnchantmentBehavior)behavior).IsReagent = true;
                }
                if (!foundEB)
                {
                    EnchantmentBehavior eb = new EnchantmentBehavior(obj);
                    if (foundRB == true) eb.IsReagent = true;
                    obj.CollectibleBehaviors = obj.CollectibleBehaviors.Append(eb).ToArray();
                }
            }
            sApi.World.Logger.StoryEvent(Lang.Get("Enchanting..."));
            Api.Logger.Notification("[KRPGEnchantment] KRPG Enchantment behaviors loaded.");
        }
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
            RegisterCompatibility();
            // sApi.Event.PlayerNowPlaying += RegisterPlayerEEB;
        }
        private void RegisterCompatibility()
        {
            if (sApi.ModLoader.IsModEnabled("combatoverhaul") == true)
            {
                combatOverhaul = new COSystem();
                combatOverhaul.StartServerSide(Api);
            }
            if (sApi.ModLoader.IsModEnabled("krpgwands") == true)
            {
                krpgWands = new KRPGWandsSystem();
                krpgWands.StartServerSide(Api);
            }
        }
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            Api = api;
            EnchantAccessor.Api = api;

            api.RegisterCollectibleBehaviorClass("EnchantmentBehavior", typeof(EnchantmentBehavior));
            api.RegisterEntityBehaviorClass("EnchantmentEntityBehavior", typeof(EnchantmentEntityBehavior));
            api.RegisterBlockClass("ChargingBlock", typeof(ChargingBlock));
            api.RegisterBlockClass("EnchantingBlock", typeof(EnchantingBlock));
            api.RegisterBlockEntityClass("ChargingBE", typeof(ChargingBE));
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
    }
}