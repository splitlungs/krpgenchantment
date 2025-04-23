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

namespace KRPGLib.Enchantment
{
    public class KRPGEnchantmentSystem : ModSystem
    {
        public ICoreAPI Api;
        public ICoreServerAPI sApi;
        public IWorldAccessor world;
        public EnchantAccessor EnchantAccessor { get; private set; }
        public static Dictionary <string, Enchantment> Enchantments;
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
            Dictionary<AssetLocation, JToken> files = sApi.Assets.GetMany<JToken>(sApi.Server.Logger, "enchantments", "krpgenchantment");
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

            if (enchant.Name == null) enchant.Name = loc;

            if (enchant.Code != null && !Enchantments.ContainsKey(enchant.Code))
                Enchantments.Add(enchant.Code, enchant);
        }
        public override void StartServerSide(ICoreServerAPI api)
        {
            sApi = api;
            EnchantAccessor.sApi = api;
            sApi.Event.PlayerNowPlaying += RegisterPlayerEEB;
            RegisterCompatibility();
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
