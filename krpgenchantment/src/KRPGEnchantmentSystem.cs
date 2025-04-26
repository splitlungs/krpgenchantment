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
        public void LoadEnchantments()
        {
            Dictionary<AssetLocation, JToken> files = new Dictionary<AssetLocation, JToken>();
            // Path to the font file in the ModData folder
            List<EnchantmentProperties> enchantData = new List<EnchantmentProperties>();
            string fPath = Path.Combine(sApi.GetOrCreateDataPath(Path.Combine("ModData", "krpgenchantment", "enchantments")));
            foreach (string file in Directory.GetFiles(fPath))
            {
                string ConfigFile = "KRPGEnchantment//Enchantments";
                EnchantmentProperties Config = new EnchantmentProperties();
                Config = sApi.LoadModConfig<EnchantmentProperties>(ConfigFile);

                if (Config != null)
                {
                    enchantData.Add(Config);
                }
                else
                {
                    EnchantmentProperties newConfig = new EnchantmentProperties();
                    sApi.StoreModConfig(newConfig, ConfigFile);
                    sApi.Logger.Warning("[KRPGEnchantment] KRPGEnchantConfig file not found. A new one has been created.");
                }
            }

            // string ConfigFile = "KRPGEnchantment//Enchantments";
            // EnchantmentData Config = new EnchantmentData();
            // Config = sApi.LoadModConfig<EnchantmentData>(ConfigFile);
            // if (Config == null)
            // {
            //     Config = new EnchantmentData();
            //     sApi.StoreModConfig(Config, ConfigFile);
            // 
            //     sApi.Logger.Warning("[KRPGEnchantment] KRPGEnchantConfig file not found. A new one has been created.");
            // }

            // Dictionary<AssetLocation, JToken> files = sApi.Assets.GetMany<JToken>(sApi.Server.Logger, "enchantments", "krpgenchantment");
            if (EnchantingConfigLoader.Config.CustomPatches.Count > 0)
            {
                foreach (KeyValuePair<string, bool> keyValuePair in EnchantingConfigLoader.Config.CustomPatches)
                {
                    if (keyValuePair.Value != true)
                        continue;

                    if (sApi.ModLoader.IsModEnabled(keyValuePair.Key.ToLower()))
                        files.AddRange(sApi.Assets.GetMany<JToken>(sApi.Server.Logger, "enchantments/" + keyValuePair.Key.ToLower()));
                }
            }

            int fileQuantity = 0;

            foreach (var val in files)
            {
                if (val.Value is JObject)
                {
                    LoadEnchantment(val.Key, val.Value.ToObject<Enchantment>(val.Key.Domain));
                    fileQuantity++;
                }
                if (val.Value is JArray)
                {
                    foreach (var token in (val.Value as JArray))
                    {
                        LoadEnchantment(val.Key, token.ToObject<Enchantment>(val.Key.Domain));
                        fileQuantity++;
                    }
                }
            }

            sApi.World.Logger.Notification("[KRPGEnchantment] {0} enchantments loaded from {1} files.", fileQuantity, files.Count);
        }

        public void LoadEnchantment(AssetLocation loc, Enchantment enchant)
        {
            if (!enchant.Enabled) return;

            // if (enchant.Name == null) enchant.Name = loc;

            // if (enchant.Code != null && !EnchantAccessor.EnchantRegistry.ContainsKey(enchant.Code))
            //     EnchantAccessor.EnchantRegistry.Add(enchant.Code, enchant.GetType());
        }
        public override void StartServerSide(ICoreServerAPI api)
        {
            sApi = api;
            EnchantAccessor.sApi = api;
            sApi.Event.PlayerNowPlaying += RegisterPlayerEEB;
            RegisterCompatibility();

            api.EnchantAccessor().RegisterEnchantmentClass(new AssetLocation("krpgenchantment:enchantments/pit"), typeof(PitEnchantment));
            api.EnchantAccessor().RegisterEnchantmentClass(new AssetLocation("krpgenchantment:enchantments/flaming"), typeof(FlamingEnchantment));
        }
        
        // Dictionary<string, Enchantment> d2 = [];
        // private IEnchantment EnchantGeneric<T>() where T : Enchantment, new() { }

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
        /// Processes EnchantTrigger event for vanilla VS.
        /// </summary>
        /// <param name="enchant"></param>
        /// <param name="trigger"></param>
        /// <param name="target"></param>
        /// <param name="damageSource"></param>
        /// <param name="slot"></param>
        /// <param name="damage"></param>
        // public void OnTriggerEnchantment(string enchant, string trigger, Entity target, DamageSource damageSource, ItemSlot slot, ref float damage)
        // {
        //     if (!Enchantments.ContainsKey(enchant))
        //         return;
        // 
        //     if (Enchantments[enchant]?.Enabled != true)
        //         return;
        // 
        //     Enchantments[enchant].OnTrigger(trigger, target, damageSource, slot, ref damage);
        // }
    }
}
