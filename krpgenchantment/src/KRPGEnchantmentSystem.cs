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

namespace KRPGLib.Enchantment
{
    public class KRPGEnchantmentSystem : ModSystem
    {
        public ICoreAPI Api;
        public ICoreServerAPI sApi;
        public IWorldAccessor world;
        public static EnchantAccessor EnchantAccessor { get; private set; } = null!;
        private static Harmony harmony;
        private COSystem combatOverhaul;
        private KRPGWandsSystem krpgWands;

        #region Setup
        public override void AssetsLoaded(ICoreAPI api)
        {
            if (!(api is ICoreServerAPI sapi)) return;
            this.sApi = sapi;
        }
        public override void StartServerSide(ICoreServerAPI api)
        {
            sApi = api;
            EnchantAccessor.sApi = api;
            sApi.Event.PlayerNowPlaying += RegisterPlayerEEB;
            RegisterCompatibility();

            api.EnchantAccessor().RegisterEnchantmentClass(new AssetLocation("krpgenchantment", "enchantments/chilling"), typeof(ChillingEnchantment));
            api.EnchantAccessor().RegisterEnchantmentClass(new AssetLocation("krpgenchantment", "enchantments/durable"), typeof(DurableEnchantment));
            api.EnchantAccessor().RegisterEnchantmentClass(new AssetLocation("krpgenchantment", "enchantments/flaming"), typeof(DamageEnchantment));
            api.EnchantAccessor().RegisterEnchantmentClass(new AssetLocation("krpgenchantment", "enchantments/frost"), typeof(DamageEnchantment));
            api.EnchantAccessor().RegisterEnchantmentClass(new AssetLocation("krpgenchantment", "enchantments/harming"), typeof(DamageEnchantment));
            api.EnchantAccessor().RegisterEnchantmentClass(new AssetLocation("krpgenchantment", "enchantments/healing"), typeof(DamageEnchantment));
            api.EnchantAccessor().RegisterEnchantmentClass(new AssetLocation("krpgenchantment", "enchantments/igniting"), typeof(PitEnchantment));
            api.EnchantAccessor().RegisterEnchantmentClass(new AssetLocation("krpgenchantment", "enchantments/knockback"), typeof(PitEnchantment));
            api.EnchantAccessor().RegisterEnchantmentClass(new AssetLocation("krpgenchantment", "enchantments/lightning"), typeof(PitEnchantment));
            api.EnchantAccessor().RegisterEnchantmentClass(new AssetLocation("krpgenchantment", "enchantments/pit"), typeof(PitEnchantment));
            api.EnchantAccessor().RegisterEnchantmentClass(new AssetLocation("krpgenchantment", "enchantments/protection"), typeof(PitEnchantment));
            api.EnchantAccessor().RegisterEnchantmentClass(new AssetLocation("krpgenchantment", "enchantments/resistelectricity"), typeof(PitEnchantment));
            api.EnchantAccessor().RegisterEnchantmentClass(new AssetLocation("krpgenchantment", "enchantments/resistfire"), typeof(PitEnchantment));
            api.EnchantAccessor().RegisterEnchantmentClass(new AssetLocation("krpgenchantment", "enchantments/resistfrost"), typeof(PitEnchantment));
            api.EnchantAccessor().RegisterEnchantmentClass(new AssetLocation("krpgenchantment", "enchantments/resistheal"), typeof(PitEnchantment));
            api.EnchantAccessor().RegisterEnchantmentClass(new AssetLocation("krpgenchantment", "enchantments/resistinjury"), typeof(PitEnchantment));
            api.EnchantAccessor().RegisterEnchantmentClass(new AssetLocation("krpgenchantment", "enchantments/resistpoison"), typeof(PitEnchantment));
            api.EnchantAccessor().RegisterEnchantmentClass(new AssetLocation("krpgenchantment", "enchantments/shocking"), typeof(DamageEnchantment));
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            EnchantAccessor.cApi = api;
        }
        private void RegisterCompatibility()
        {
            if (EnchantingConfigLoader.Config?.CustomPatches.GetValueOrDefault("CombatOverhaul", false) == true
            && sApi.ModLoader.IsModEnabled("combatoverhaul") == true)
            {
                combatOverhaul = new COSystem();
                combatOverhaul.StartServerSide(sApi);
            }
            if (EnchantingConfigLoader.Config?.CustomPatches.GetValueOrDefault("KRPGWands", false) == true
                && sApi.ModLoader.IsModEnabled("krpgwands") == true)
            {
                krpgWands = new KRPGWandsSystem();
                krpgWands.StartServerSide(sApi);
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

            EnchantAccessor = new EnchantAccessor();
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

        // Events in case if I decide to use them later.
        // public event EnchantTriggerDelegate EnchantTrigger;
        // public delegate void EnchantTriggerDelegate(string enchant, string trigger, Entity target, DamageSource damageSource, ItemSlot slot, ref float damage);

        /// <summary>
        /// Processes EnchantTrigger event for vanilla VS. Pass NULL for parameters if none are required.
        /// </summary>
        /// <param name="enchantSource"></param>
        /// <param name="slot"></param>
        /// <param name="parameters"></param>
        // public void OnTriggerEnchantment(EnchantmentSource enchantSource, ItemSlot slot, object[] parameters)
        // {
        //     if (EnchantAccessor.EnchantmentRegistry[enchantSource.Code]?.Enabled != true)
        //         return;
        // 
        //     EnchantAccessor.EnchantmentRegistry[enchantSource.Code].OnTrigger(enchantSource, slot, parameters);
        // }
    }
}
