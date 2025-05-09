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
        public ICoreServerAPI sApi;
        public ICoreClientAPI cApi;
        public IWorldAccessor world;
        public static EnchantAccessor EnchantmentAccessor { get; private set; } = null!;
        /// <summary>
        /// All Enchantments are processed and stored here. Must use RegisterEnchantmentClass to handle adding Enchantments.
        /// </summary>
        public Dictionary<string, Enchantment> EnchantmentRegistry = new Dictionary<string, Enchantment>();
        private Dictionary<string, Type> EnchantCodeToTypeMapping = new Dictionary<string, Type>();
        private static Harmony harmony;
        private COSystem combatOverhaul;
        private KRPGWandsSystem krpgWands;

        #region ModSystem & Setup
        public override void AssetsLoaded(ICoreAPI api)
        {
            if (!(api is ICoreServerAPI sapi)) return;
            this.sApi = sapi;
        }
        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);
            EnchantmentAccessor = new EnchantAccessor();
            EnchantmentRegistry = new Dictionary<string, Enchantment>();
        }
        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            cApi = api;
            EnchantmentAccessor.cApi = cApi;
        }
        public override void StartServerSide(ICoreServerAPI api)
        {
            sApi = api;
            EnchantmentAccessor.sApi = sApi;
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
            EnchantmentAccessor.Api = Api;

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
        #region Enchantments
        /// <summary>
        /// Register an Enchantment to the EnchantmentRegistry. All Enchantments must be registered here. Returns false if it fails to register.
        /// </summary>
        /// <param name="enchantClass"></param>
        /// <param name="configLocation"></param>
        /// <param name="t"></param>
        public bool RegisterEnchantmentClass(string enchantClass, string configLocation, Type t)
        {
            if (EnchantingConfigLoader.Config.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] Attempting to RegisterEnchantmentClass.");
            if (enchantClass == null || configLocation == null || t.BaseType != typeof(Enchantment))
            {
                Api.Logger.Error("[KRPGEnchantment] Attempted to register an Enchantment with an invalid or missing registration information.");
                return false;
            }
            try
            {
                // Register the Enchantment Class
                this.EnchantCodeToTypeMapping[enchantClass] = t;
                // Create a new instance & assign registered class name
                var enchant = CreateEnchantment(enchantClass);
                // Setup the Config
                EnchantmentProperties props = Api.LoadModConfig<EnchantmentProperties>("KRPGEnchantment/Enchantments/" + configLocation);
                if (props == null)
                {
                    props = new EnchantmentProperties()
                    {
                        Enabled = enchant.Enabled,
                        Code = enchant.Code,
                        LoreCode = enchant.LoreCode,
                        LoreChapterID = enchant.LoreChapterID,
                        MaxTier = enchant.MaxTier,
                        Modifiers = enchant.Modifiers
                        // Attributes = enchant.Attributes.Clone()
                    };

                    Api.StoreModConfig(props, "KRPGEnchantment/Enchantments/" + configLocation);
                }
                enchant.Initialize(props);
                // Add to the Registry
                EnchantmentRegistry.Add(enchant.Code, enchant);

                if (EnchantingConfigLoader.Config.Debug == true)
                    Api.World.Logger.Event("[KRPGEnchantment] Enchantment {0} registered to the Enchantment Registry.", enchantClass);

                return true;
            }
            catch (Exception e)
            {
                Api.Logger.Error("[KRPGEnchantment] Error loading Enchantment Class: {0}", e);
                return false;
            }
        }
        private Type GetEnchantmentClass(string enchantClass)
        {
            Type val = null;
            this.EnchantCodeToTypeMapping.TryGetValue(enchantClass, out val);
            return val;
        }
        private Enchantment CreateEnchantment(string enchantClass)
        {
            Type enchantType;
            if (enchantClass == null || !this.EnchantCodeToTypeMapping.TryGetValue(enchantClass, out enchantType))
            {
                throw new Exception("[KRPGEnchantment] Don't know how to instantiate enchantment of class '" + enchantClass + "' did you forget to register a mapping?");
            }
            Enchantment result;
            try
            {
                result = (Enchantment)Activator.CreateInstance(enchantType, new object[1] { Api });
                result.ClassName = enchantClass;
            }
            catch (Exception exception)
            {
                DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(39, 2);
                defaultInterpolatedStringHandler.AppendLiteral("[KRPGEnchantment] Error on instantiating enchantment class '");
                defaultInterpolatedStringHandler.AppendFormatted(enchantClass);
                defaultInterpolatedStringHandler.AppendLiteral("':\n");
                defaultInterpolatedStringHandler.AppendFormatted<Exception>(exception);
                throw new Exception(defaultInterpolatedStringHandler.ToStringAndClear(), exception);
            }

            return result;
        }
        public IEnchantment GetEnchantment(string enchantCode)
        {
            return EnchantmentRegistry.GetValueOrDefault(enchantCode, null);
        }
        /// <summary>
        /// Bulk convenience processor for Enchantments. Returns false if it fails to run an Enchantment trigger.
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="trigger"></param>
        /// <param name="byEntity"></param>
        /// <param name="targetEntity"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public bool TryEnchantments(ItemSlot slot, string trigger, Entity byEntity, Entity targetEntity, ref Dictionary<string, object> parameters)
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] TryEnchantments has been called.");

            Dictionary<string, int> enchants = GetEnchantments(slot.Itemstack);
            if (enchants != null)
            {
                if (enchants.ContainsKey("healing") && parameters?.ContainsKey("damage") == true)
                    parameters["damage"] = 0;

                foreach (KeyValuePair<string, int> pair in enchants)
                {
                    IEnchantment enc = GetEnchantment(pair.Key);
                    if (enc?.Enabled != true)
                    {
                        if (EnchantingConfigLoader.Config?.Debug == true)
                            Api.Logger.Event("[KRPGEnchantment] Tried Enchantment {0}, but it was either Disabled or not get-able.", pair.Key);
                        continue;
                    }

                    EnchantmentSource enchant = new EnchantmentSource()
                    {
                        SourceStack = slot.Itemstack,
                        Trigger = trigger,
                        Code = pair.Key,
                        Power = pair.Value,
                        SourceEntity = byEntity,
                        CauseEntity = byEntity,
                        TargetEntity = targetEntity
                    };

                    enc.OnTrigger(enchant, ref parameters);
                }
                return true;
            }
            return false;
        }
        /// <summary>
        /// Bulk convenience processor for Enchantments. Returns false if it fails to run an Enchantment trigger.
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="trigger"></param>
        /// <param name="byEntity"></param>
        /// <param name="targetEntity"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public bool TryEnchantments(ItemStack stack, string trigger, Entity byEntity, Entity targetEntity, ref Dictionary<string, object> parameters)
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] TryEnchantments has been called.");

            Dictionary<string, int> enchants = GetEnchantments(stack);
            if (enchants != null)
            {
                foreach (KeyValuePair<string, int> pair in enchants)
                {
                    IEnchantment enc = GetEnchantment(pair.Key);
                    if (enc?.Enabled != true)
                    {
                        if (EnchantingConfigLoader.Config?.Debug == true)
                            Api.Logger.Event("[KRPGEnchantment] Tried Enchantment {0}, but it was either Disabled or not get-able.", pair.Key);
                        continue;
                    }

                    EnchantmentSource enchant = new EnchantmentSource()
                    {
                        SourceStack = stack,
                        Trigger = trigger,
                        Code = pair.Key,
                        Power = pair.Value,
                        SourceEntity = byEntity,
                        CauseEntity = byEntity,
                        TargetEntity = targetEntity
                    };

                    enc.OnTrigger(enchant, ref parameters);
                    // if (didEnchantment != true)
                    // {
                    //     if (EnchantingConfigLoader.Config?.Debug == true)
                    //         Api.Logger.Event("[KRPGEnchantment] Tried Enchantment {0}, but it failed.", enchant.Code);
                    // }
                }
                return true;
            }
            return false;
        }
        /// <summary>
        /// Generic convenience processor for Enchantments. Requires a pre-formed EnchantmentSource Returns false if it fails to run an Enchantment trigger.
        /// </summary>
        /// <param name="enchant"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public bool TryEnchantment(EnchantmentSource enchant, ref Dictionary<string, object> parameters)
        {
            if (GetEnchantment(enchant.Code)?.Enabled != true)
                return false;

            GetEnchantment(enchant.Code).OnTrigger(enchant, ref parameters);

            return true;
        }
        /// <summary>
        /// Returns all Enchantments in the ItemStack's Attributes or null if none are found.
        /// </summary>
        /// <param name="itemStack"></param>
        /// <returns></returns>
        public Dictionary<string, int> GetEnchantments(ItemStack itemStack)
        {
            if (EnchantingConfigLoader.Config.Debug == true)
                Api.World.Logger.Event("[KRPGEnchantment] Attempting to GetEnchantments on {0}", itemStack.GetName());

            ITreeAttribute tree = itemStack?.Attributes?.GetTreeAttribute("enchantments");
            if (tree == null)
                return null;
            ITreeAttribute active = tree?.GetTreeAttribute("active");
            if (active == null)
                return null;

            // Get Enchantments
            Dictionary<string, int> enchants = new Dictionary<string, int>();
            foreach (KeyValuePair<string, IAttribute> pair in active)
            {
                enchants.Add(pair.Key, (int)pair.Value.GetValue());
            }
            // foreach (KeyValuePair<string, Enchantment> pair in EnchantmentRegistry)
            // {
            //     IEnchantment enc = pair.Value;
            //     int ePower = tree.GetInt(enc.Code, 0);
            //     if (ePower > 0) 
            //     {
            //         if (EnchantingConfigLoader.Config.Debug == true)
            //             sApi.World.Logger.Event("[KRPGEnchantment] Attempting to GetEnchantments on {0} and found {1}: {2}", itemStack.GetName(), enc.Code, ePower);
            //         enchants.Add(enc.Code, ePower); 
            //     }
            // }

            // Throw null if we failed to get anything
            if (enchants.Count <= 0) return null;
            return enchants;
        }
        #endregion
        #region GUI
        /// <summary>
        /// Returns a request font file from ModData/krpgenchantment/fonts, downloads it if possible, or null if it doesn't exist
        /// </summary>
        /// <param name="fName"></param>
        /// <returns></returns>
        public SKTypeface LoadCustomFont(string fName)
        {
            // Path to the font file in the ModData folder
            string fontPath = System.IO.Path.Combine(cApi.GetOrCreateDataPath(System.IO.Path.Combine("ModData", "krpgenchantment", "fonts")), fName);

            // Download the file to the client's ModData if it doesn't exist
            if (!File.Exists(fontPath))
            {
                cApi.World.Logger.Warning("[KRPGEnchantment] Font file not found at path: {0}.", fontPath);
                cApi.World.Logger.Event("[KRPGEnchantment] Copying font file to path: {0}.", fontPath);

                try
                {
                    using (var client = new HttpClient())
                    {
                        using (var s = client.GetStreamAsync("http://kronos-gaming.net/downloads/files/" + fName))
                        {
                            using (var fs = new FileStream(fontPath, FileMode.OpenOrCreate))
                            {
                                s.Result.CopyTo(fs);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    cApi.World.Logger.Error("[KRPGEnchantment] Failed to download custom font: {0}", e.Message);
                    return null;
                }
            }
            // Check if the font file was created and bail if not
            if (!File.Exists(fontPath))
            {
                cApi.World.Logger.Error("[KRPGEnchantment] Font file not found at path: {0}.", fontPath);
                return null;
            }

            try
            {
                // Load the custom font using SkiaSharp
                using (var fontStream = File.OpenRead(fontPath))
                {
                    SKTypeface customTypeface = SKTypeface.FromStream(fontStream);
                    if (customTypeface != null)
                    {
                        // api.World.Logger.Notification("Custom font successfully loaded from: " + fontPath);
                        return customTypeface;
                    }
                    else
                    {
                        cApi.World.Logger.Error("[KRPGEnchantment] Failed to create SKTypeface from the font file.");
                        return null;
                    }
                }
            }
            catch (Exception e)
            {
                cApi.World.Logger.Error("[KRPGEnchantment] Failed to load custom font: " + e.Message);
                return null;
            }
        }
        #endregion
    }
}