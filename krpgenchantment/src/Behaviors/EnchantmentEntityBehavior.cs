using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using KRPGLib.Enchantment.API;
// using CombatOverhaul.Armor;
using System.Reflection.Metadata;
using Vintagestory.API.Util;
using Cairo.Freetype;
using System.Numerics;
using Vintagestory.API.Datastructures;
using static System.Net.Mime.MediaTypeNames;
using System.Data;
// using CombatOverhaul.MeleeSystems;
using System.Collections;
using Cairo;

namespace KRPGLib.Enchantment
{
    public class EnchantmentEntityBehavior : EntityBehavior
    {
        #region Vars
        public override string PropertyName() { return "EnchantmentEntityBehavior"; }
        public ICoreAPI Api { get { return entity.Api;} }
        public EntityStats stats { get { return entity.Stats; } }
        public int TickTime
        {
            get
            {
                return EnchantingConfigLoader.Config?.EntityTickMs ?? 1000;
            }
        }
        private long onTickID = 0;
        private EntityAgent agent
        {
            get
            {
                if (!(entity is EntityAgent ea)) return null;
                return ea;
            }
        }
        private string playerUID;
        /// <summary>
        /// Gets the Player from API. Always returns null on client.
        /// </summary>
        private IServerPlayer player
        {
            get
            {
                if (!(Api is ICoreServerAPI api)) return null;
                return (IServerPlayer)api.World.PlayerByUid(playerUID);
            }
        }
        public bool IsPlayer { get { if (player != null) return true; return false; } }
        public IInventory gearInventory
        {
            get 
            { 
                if (!IsPlayer) return null;
                // return player.InventoryManager.GetOwnInventory("character");
                return entity.GetBehavior<EntityBehaviorPlayerInventory>()?.Inventory;
            }
        }
        public  IInventory hotbarInventory
        {
            get
            {
                if (!IsPlayer) return null;
                return player.InventoryManager.GetHotbarInventory();
            }
        }
        public Dictionary<int, ActiveEnchantCache> GearEnchantCache = null;
        public Dictionary<int, ActiveEnchantCache> HotbarEnchantCache = null;
        public Dictionary<string, EnchantTick> TickRegistry = new Dictionary<string, EnchantTick>();
        #endregion
        #region Setup
        public EnchantmentEntityBehavior(Entity entity) : base(entity)
        {
            // Api = entity.Api;
            // Well look what I fuckin found in EntityPlayer. I don't remember any patch notes about this shit
            // entity.Stats.Register("healingeffectivness").Register("maxhealthExtraPoints").Register("walkspeed");
        }
        public void RegisterPlayer(IServerPlayer byPlayer)
        {
            if (!(Api is ICoreServerAPI sapi)) return;
            // Save the IServerPlayer
            playerUID = byPlayer.PlayerUID;
            // Register Gear cache
            if (gearInventory != null)
            {
                GearEnchantCache = new Dictionary<int, ActiveEnchantCache>();
                foreach (ItemSlot slot in gearInventory)
                {
                    int slotId = gearInventory.GetSlotId(slot);
                    if (slot?.Itemstack != null)
                    {
                        // Trigger OnEquip since we just logged in
                        EnchantModifiers parameters = new EnchantModifiers() { { "IsHotbar", false } };
                        bool didEnchants = sapi.EnchantAccessor().TryEnchantments(slot, "OnEquip", entity, entity, ref parameters);
                    }
                    GenerateGearEnchantCache(slotId);
                }
                gearInventory.SlotModified += OnGearModified;
            }
            else
            {
                Api?.Logger?.Error("[KRPGEnchantment] Player {0} tried to register GearInventory, but returned null. Gear & Hotbar enchants will not trigger.", player.PlayerUID);
                return;
            }
            // Register Hotbar cache
            if (hotbarInventory != null)
            {
                int offHandID = hotbarInventory.GetSlotId(player.InventoryManager.OffhandHotbarSlot);
                HotbarEnchantCache = new Dictionary<int, ActiveEnchantCache>();
                foreach (ItemSlot slot in hotbarInventory)
                {
                    int slotId = hotbarInventory.GetSlotId(slot);
                    if (slot?.Itemstack != null)
                    {
                        // Trigger OnEquip since we just logged in
                        bool isOffhand = false;
                        if (slotId == offHandID) isOffhand = true;
                        EnchantModifiers parameters = new EnchantModifiers() { { "IsHotbar", true }, { "IsOffHand", isOffhand } };
                        bool didEnchants = sapi.EnchantAccessor().TryEnchantments(slot, "OnEquip", entity, entity, ref parameters);
                    }
                    GenerateHotbarEnchantCache(slotId);
                }
                hotbarInventory.SlotModified += OnHotbarModified;
            }
            else
            {
                Api?.Logger?.Error("[KRPGEnchantment] Player {0} tried to register HotbarInventory, but returned null. Hotbar enchants will not trigger.", player.PlayerUID);
                return;
            }
            entity.GetBehavior<EntityBehaviorHealth>().onDamaged += OnHit;
        }
        public override void OnEntitySpawn()
        {
            base.OnEntitySpawn();
            // Toggle passives - Disabled for now
            // if (IsPlayer) ToggleHeldItems();
            // Only tick when spawned
            enchantTickLocked = false;
        }
        public override void OnEntityDeath(DamageSource damageSourceForDeath)
        {
            if (!(Api is ICoreServerAPI sapi)) 
            {
                base.OnEntityDeath(damageSourceForDeath);
                return;
            }
            // All entities
            foreach (KeyValuePair<string, EnchantTick> pair in TickRegistry)
            {
                if (pair.Value.Persistent) continue;
                pair.Value.IsTrash = true;
            }
            // Only players wear/hold items (so far)
            if (IsPlayer)
            {
                foreach (KeyValuePair<int, ActiveEnchantCache> pair in GearEnchantCache)
                {
                    EnchantModifiers parameters = new EnchantModifiers() { { "IsHotbar", false }, { "IsOffhand", false } };
                    bool didEnchants = sapi.EnchantAccessor().TryEnchantments(gearInventory[pair.Key], "OnDeath", damageSourceForDeath.CauseEntity, entity, ref parameters);
                    if (didEnchants == true && EnchantingConfigLoader.Config?.Debug == true)
                        sapi.Logger.Event("[KRPGEnchantment] {0} succesfully triggered OnDeath for {1}.", entity.GetName(), gearInventory[pair.Key].Itemstack?.GetName());
                }
                if (player.InventoryManager.ActiveHotbarSlot?.Itemstack != null)
                {
                    EnchantModifiers parameters = new EnchantModifiers() { { "IsHotbar", true }, { "IsOffhand", false } };
                    bool didEnchants = sapi.EnchantAccessor().TryEnchantments(player.InventoryManager.ActiveHotbarSlot, "OnDeath", damageSourceForDeath.CauseEntity, entity, ref parameters);
                    if (didEnchants == true && EnchantingConfigLoader.Config?.Debug == true)
                        sapi.Logger.Event("[KRPGEnchantment] {0} succesfully triggered OnDeath  for {1}.", entity.GetName(), player.InventoryManager.ActiveHotbarSlot.Itemstack?.GetName());
                }
                // Only process off hand if not two-handed main hand
                if (!(player.InventoryManager.OffhandHotbarSlot?.Itemstack?.Id == player.InventoryManager.ActiveHotbarSlot?.Itemstack?.Id))
                {
                    EnchantModifiers parameters = new EnchantModifiers() { { "IsHotbar", true }, { "IsOffhand", true } };
                    bool didEnchants = sapi.EnchantAccessor().TryEnchantments(player.InventoryManager.OffhandHotbarSlot, "OnDeath", damageSourceForDeath.CauseEntity, entity, ref parameters);
                    if (didEnchants == true && EnchantingConfigLoader.Config?.Debug == true)
                        sapi.Logger.Event("[KRPGEnchantment] {0} succesfully triggered OnDeath for {1}.", entity.GetName(), player.InventoryManager.ActiveHotbarSlot.Itemstack?.GetName());
                }
            }
            base.OnEntityDeath(damageSourceForDeath);
        }
        public override void OnEntityDespawn(EntityDespawnData despawn)
        {
            base.OnEntityDespawn(despawn);

            if (!(Api is ICoreServerAPI sapi)) return;

            if (IsPlayer)
            {
                entity.GetBehavior<EntityBehaviorHealth>().onDamaged -= OnHit;
                gearInventory.SlotModified -= OnGearModified;
                hotbarInventory.SlotModified -= OnHotbarModified;
                sapi.World.UnregisterGameTickListener(onTickID);
            }
        }
        #endregion
        #region Cache
        /// <summary>
        /// Creates or updates an ActiveEnchantCache for each slot in the player's Character inventory.
        /// </summary>
        /// <param name="slotId"></param>
        public void GenerateGearEnchantCache(int slotId)
        {
            if (!(Api is ICoreServerAPI sapi)) return;
            // Don't generate cache for null slots or inventories
            if (gearInventory?[slotId] == null) return;
            ItemSlot slot = gearInventory[slotId];
            // Generate Cache if it worked
            ActiveEnchantCache cache = new ActiveEnchantCache();
            cache.Enchantments = Api.EnchantAccessor().GetActiveEnchantments(slot?.Itemstack);
            cache.LastCheckTime = Api.World.ElapsedMilliseconds;
            if (gearInventory[slotId].Empty == true)
                cache.ItemId = -1;
            else
                cache.ItemId = gearInventory[slotId].Itemstack.Item.ItemId;
            // Set the cache
            if (GearEnchantCache.ContainsKey(slotId) == true)
                GearEnchantCache[slotId] = cache;
            else
                GearEnchantCache.Add(slotId, cache);
        }
        /// <summary>
        /// Creates or updates an ActiveEnchantCache for each slot in the player's Hotbar inventory.
        /// </summary>
        /// <param name="slotId"></param>
        public void GenerateHotbarEnchantCache(int slotId)
        {
            if (!(Api is ICoreServerAPI sapi)) return;
            // Don't generate cache for null slots or inventories
            if (hotbarInventory?[slotId] == null) return;
            ItemSlot slot = hotbarInventory[slotId];
            // Generate Cache if it worked
            ActiveEnchantCache cache = new ActiveEnchantCache();
            cache.Enchantments = Api.EnchantAccessor().GetActiveEnchantments(slot?.Itemstack);
            cache.LastCheckTime = Api.World.ElapsedMilliseconds;
            if (hotbarInventory[slotId].Empty == true)
                cache.ItemId = -1;
            else
                cache.ItemId = hotbarInventory[slotId]?.Itemstack?.Item?.ItemId ?? -1;
            // Set the cache
            if (HotbarEnchantCache.ContainsKey(slotId) == true)
                HotbarEnchantCache[slotId] = cache;
            else
                HotbarEnchantCache.Add(slotId, cache);
        }
        // WIP - Create OnToggle system.
        /*
        // Main Slot, Main Itemstack.Item.ItemId, Offhand Slot, Offhand Itemstack.Item.ItemId
        ItemStack[] HeldItemCache = { null, null };
        /// <summary>
        /// Trigger a an "OnToggle" for the given item.
        /// </summary>
        /// <param name="stack"></param>
        /// <param name="state"></param>
        public void ToggleItem(ItemStack stack, bool state)
        {
            if (!(Api is ICoreServerAPI sapi)) return;
            EnchantModifiers parameters = new EnchantModifiers() { {"ToggleState", state } };
            bool didEnchants = sapi.EnchantAccessor().TryEnchantments(stack, "OnToggle", entity, entity, ref parameters);
            if (didEnchants && EnchantingConfigLoader.Config?.Debug == true)
                sapi.Logger.Event("[KRPGEnchantment] Successfully completed an OnToggle trigger state {0} for {1}.", state, entity.GetName());
        }
        /// <summary>
        /// Toggling: The art of scrolling one's mouse wheel along a hotbar.
        /// Checks for currently held item and offhand item, then assigns them to HeldItemUID. 
        /// Finally, it calls "OnToggle" enchantments if necessary.
        /// </summary>
        public void ToggleHeldItems()
        {
            // if (!(Api is ICoreServerAPI sapi)) return;
            // 1. Setup new cache and compare
            IPlayerInventoryManager inv = player?.InventoryManager;
            // New - Cache
            ItemStack[] newItemCache = { 
                inv?.ActiveHotbarSlot?.Itemstack?.Clone(), 
                inv?.OffhandHotbarSlot?.Itemstack?.Clone() };
            // New - Two Handed?
            // bool? new2H = newItemCache[0]?.Equals(newItemCache[1]);
            // Old - Two Handed?
            // bool? old2H = HeldItemCache[0]?.Equals(HeldItemCache[1]);
            bool? mainMatch = newItemCache[0]?.Equals(HeldItemCache[0]);
            bool? offMatch = newItemCache[1]?.Equals(HeldItemCache[1]);
            // 2. Toggle On/Off based on cache
            // 2a. Old - New Match - Do nothing
            if (newItemCache.Equals(HeldItemCache) == true) 
                return;
            // 2b. Main Change - Off Match
            else if (mainMatch != true && offMatch == true)
            {
                // Toggle off Old Main
                if (HeldItemCache[0] != null) 
                    ToggleItem(HeldItemCache[0], false);
                // Toggle  on New Main
                if (newItemCache[0] != null)
                    ToggleItem(newItemCache[0], true);
            }
            // 2c. Main Match - Off Change
            else if (mainMatch == true && offMatch != true)
            {
                // Toggle off Old Offhand
                if (HeldItemCache[1] != null)
                    ToggleItem(HeldItemCache[1], false);
                // Toggle on New Offhand
                if (newItemCache[1] != null)
                    ToggleItem(newItemCache[1], true);
            }
            // 2d. Main Change - Off Change
            else if (mainMatch != true && offMatch != true)
            {
                // Toggle off Old Main
                if (HeldItemCache[0] != null)
                    ToggleItem(HeldItemCache[0], false);
                // Toggle on New Main
                if (newItemCache[0] != null)
                    ToggleItem(newItemCache[0], false);
                // Toggle off Old Offhand
                if (HeldItemCache[1] != null)
                    ToggleItem(HeldItemCache[1], false);
                // Toggle on New Offhand
                if (newItemCache[1] != null)
                    ToggleItem(newItemCache[1], false);
            }
            // 3. Write cache
            HeldItemCache = newItemCache;
        }
        /// <summary>
        /// WIP. Maybe don't use?
        /// </summary>
        void RecalculateEntityStats()
        {
            if (!(Api is ICoreServerAPI sapi)) return;
            // Only players should
            if (!IsPlayer) return;
            bool mainExists = HotbarEnchantCache.TryGetValue(player.InventoryManager.ActiveHotbarSlotNumber, out ActiveEnchantCache mainCache);
            bool offExists = HotbarEnchantCache.TryGetValue(player.InventoryManager.ActiveHotbarSlotNumber, out ActiveEnchantCache offCache);
            // Both Empty
            if (!mainExists && !offExists) return;
            // Off Empty
            else if (mainExists && !offExists)
            {
                if (mainCache.Enchantments?.TryGetValue("accurate", out int p) == true)
                {
                    IEnchantment ench = sapi.EnchantAccessor().GetEnchantment("accurate");
                    float pmul = ench.Modifiers.GetFloat("PowerMultiplier");
                    float mul = p * pmul;
                    entity.Stats.Set("rangedWeaponsAcc", "krpge:" + "accurate", mul, false);
                }
                else
                {
                    entity.Stats.Set("rangedWeaponsAcc", "krpge:" + "accurate", 1.0f, true);
                }
            }
            // Main Empty
            else if (!mainExists && offExists)
            {
                
            }
            // None Empty
            else if (mainExists && offExists)
            {
                if (mainCache.Enchantments?.TryGetValue("accurate", out int p) == true)
                {
                    IEnchantment ench = sapi.EnchantAccessor().GetEnchantment("accurate");
                    float pmul = ench.Modifiers.GetFloat("PowerMultiplier");
                    float mul = p * pmul;
                    entity.Stats.Set("rangedWeaponsAcc", "krpge:" + "accurate", mul, false);
                }
                else
                {
                    entity.Stats.Set("rangedWeaponsAcc", "krpge:" + "accurate", 1.0f, true);
                }
            }


            // ItemStack mainStack = player?.InventoryManager?.ActiveHotbarSlot?.Itemstack ?? null;
            // ItemStack offStack = player?.InventoryManager?.OffhandHotbarSlot?.Itemstack ?? null;
            // // Skip if no items
            // if (mainStack == null && offStack == null) return;
            // // Only run once if two-handed
            // else if (mainStack?.Item?.ItemId == offStack?.Item?.ItemId)
            // {
            //     Dictionary<string, int> mainEnch = Api.EnchantAccessor().GetActiveEnchantments(mainStack);
            //     if (mainEnch.TryGetValue("accurate", out int power))
            //     {
            //         
            //     }
            // }
            // Dictionary<string, int> mainEnch = Api.EnchantAccessor().GetActiveEnchantments(mainStack);
            // Dictionary<string, int> offEnch = Api.EnchantAccessor().GetActiveEnchantments(offStack);
            
        }
        [Obsolete]
        /// <summary>
        /// Recalculate and reapply Combat Overhaul stats on the given item.
        /// </summary>
        /// <param name="slotId"></param>
        public void RecalculateCOStats(int slotId)
        {
            if (!(Api is ICoreServerAPI sapi)) return;
            Dictionary<string, int> enchants = HotbarEnchantCache[slotId]?.Enchantments;
            if (enchants == null) return;
            ItemSlot slot = hotbarInventory?[slotId];
            ItemStack stack = slot?.Itemstack;
            if (stack == null) return;
            // if (enchants.ContainsKey("accurate"))
            // {
            //     AccurateEnchantment ench = sapi.EnchantAccessor().GetEnchantment("accurate") as AccurateEnchantment;
            //     ench.AddMultipliersCO(ref stack, enchants["accurate"]);
            // }
            if (enchants.ContainsKey("quickdraw"))
            {
                QuickDrawEnchantment ench = sapi.EnchantAccessor().GetEnchantment("quickdraw") as QuickDrawEnchantment;
                ench.AddMultipliersCO(ref stack, enchants["quickdraw"]);
            }
            ITreeAttribute tree = stack.Attributes;
            hotbarInventory[slotId].Itemstack.Attributes.MergeTree(tree);
        }
        */
        #endregion
        #region Triggers
        /// <summary>
        /// Called when the character's inventory is modified.
        /// </summary>
        /// <param name="slotId"></param>
        public void OnGearModified(int slotId)
        {
            // 0. Sanity Checks
            if (!(Api is ICoreServerAPI sapi)) return;
            if (entity?.Alive != true) return;
            if (gearInventory?[slotId] == null) return;

            // 1. If Slot is empty AND there was a cached enchantment, do cleanup
            if (gearInventory[slotId].Empty == true && GearEnchantCache[slotId]?.Enchantments != null)
            {
                // 1.1 Trigger UnEquip
                // ItemStack dummyStack = new ItemStack();
                // string enchantString = "";
                foreach (KeyValuePair<string, int> pair in GearEnchantCache[slotId].Enchantments)
                {
                    // dummyStack.Attributes.GetOrAddTreeAttribute("enchantments");
                    // enchantString += pair.Key + ":" + pair.Value + ";";
                    EnchantmentSource enchant = new EnchantmentSource()
                    {
                        SourceSlot = gearInventory[slotId],
                        SourceStack = null,
                        Trigger = "OnUnEquip",
                        Code = pair.Key,
                        Power = pair.Value,
                        SourceEntity = entity,
                        CauseEntity = entity,
                        TargetEntity = entity,
                        DamageTier = pair.Value 
                    };
                    EnchantModifiers parameters2 = new EnchantModifiers() { { "IsHotbar", false } };
                    sapi.EnchantAccessor().TryEnchantment(enchant, ref parameters2);
                }
                
                // 1.2 Cleanup the EnchantTicks
                DisposeEnchantTicks(slotId);
                // 1.3 Update the cache and exit
                GenerateGearEnchantCache(slotId);
                return;
            }

            // 2. Item is still equipped
            if (gearInventory[slotId].Itemstack?.Id == GearEnchantCache[slotId]?.ItemId) return;

            // Armor slots, probably
            // 12.Head 13.Body 14.Legs
            // int[] wearableSlots = new int[3] { 12, 13, 14 };

            // 3. Trigger OnEquip for any Enchantments
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] Player {0} gear modified slot {1}. Attempting to trigger OnEquip enchantments.", player.PlayerUID, slotId);
            // ItemSlot slot = gearInventory[slotId];
            EnchantModifiers parameters = new EnchantModifiers() { { "IsHotbar", false }, { "IsOffhand", false } };
            bool didEnchants = sapi.EnchantAccessor().TryEnchantments(gearInventory[slotId], "OnEquip", entity, entity, ref parameters);
            if (didEnchants == true)
            {
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] {0} is generating an ActiveEnchantCache for {1}.", entity.GetName(), gearInventory[slotId].Itemstack?.GetName());
            }
            
            // 4. Update the cache
            GenerateGearEnchantCache(slotId);
        }
        /// <summary>
        /// Called when the player changes an item on the hotbar.
        /// </summary>
        /// <param name="slotId"></param>
        public void OnHotbarModified(int slotId)
        {
            // 0. Sanity Check
            if (!(Api is ICoreServerAPI sapi)) return;
            if (entity?.Alive != true) return;
            if (hotbarInventory?[slotId] == null) return;

            // 1. If Slot is empty, Remove any ticks registered to the slot
            if (hotbarInventory[slotId].Empty == true && GearEnchantCache[slotId]?.Enchantments != null)
            {
                // 1.1 Cleanup the EnchantTicks
                DisposeEnchantTicks(slotId);
                // 1.2 Update the cache and exit
                GenerateHotbarEnchantCache(slotId);
                return;
            }

            // 2. Item is still equipped
            if (hotbarInventory[slotId]?.Itemstack?.Id == HotbarEnchantCache[slotId]?.ItemId) return;

            // 11. Offhand, probably
            // int activeSlot = player.InventoryManager.ActiveHotbarSlotNumber;
            // if (activeSlot != (slotId | 11)) return;

            // 3. Check if this is the Offhand slot
            bool isOffhand = false;
            if (player.InventoryManager.OffhandHotbarSlot?.Itemstack?.Id == hotbarInventory[slotId]?.Itemstack?.Id)
                isOffhand = true;

            // 4. Trigger OnEquip for any Enchantments
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] Player {0} modified hotbar slot {1}. Attempting to trigger OnEquip enchantments.", player.PlayerUID, slotId);

            EnchantModifiers parameters = new EnchantModifiers() { { "IsHotbar", true }, { "IsOffhand", isOffhand }, { "equip", true } };
            bool didEnchants = sapi.EnchantAccessor().TryEnchantments(hotbarInventory[slotId], "OnEquip", entity, entity, ref parameters);
            if (didEnchants == true)
            {
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] {0} is generating an ActiveEnchantCache for {1}.", entity.GetName(), hotbarInventory[slotId].Itemstack?.GetName());
            }

            // 5. Update the cache
            GenerateHotbarEnchantCache(slotId);

            // 6. Update CO Stats - Note this uses Cache
            // if (sapi.ModLoader.GetModSystem<KRPGEnchantmentSystem>().COSysServer != null)
            //     RecalculateCOStats(slotId);
        }
        // After the attack has completed
        public override void DidAttack(DamageSource source, EntityAgent targetEntity, ref EnumHandling handled)
        {
            if (!(Api is ICoreServerAPI sapi)) 
            {
                base.DidAttack(source, targetEntity, ref handled);
                return;
            }
            if (!IsPlayer) 
            {
                base.DidAttack(source, targetEntity, ref handled);
                return;
            }
            handled = EnumHandling.Handled;
            ItemSlot slot = player.InventoryManager.ActiveHotbarSlot;
            EnchantModifiers parameters = new EnchantModifiers();
            bool didEnchantments = sapi.EnchantAccessor().TryEnchantments(slot, "OnAttackStop", entity, targetEntity, ref parameters);
            if (didEnchantments == true && EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] Finished processing Enchantments for EnchantmentEntitybehavior.DidAttack().");
            base.DidAttack(source, targetEntity, ref handled);
        }
        // When THIS ENTITY is interacted with by another entity. Not called by projectiles
        public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
        {
            if (!(byEntity.Api is ICoreServerAPI sapi)) 
            {
                base.OnInteract(byEntity, itemslot, hitPosition, mode, ref handled);
                return;
            }
            if (itemslot.Empty == true) 
            {
                base.OnInteract(byEntity, itemslot, hitPosition, mode, ref handled);
                return;
            }
            // EnchantModifiers parameters = new EnchantModifiers();
            // bool didEnchantments = sapi.EnchantAccessor().TryEnchantments(itemslot, "OnAttackStop", byEntity, entity, ref parameters);
            handled = EnumHandling.Handled;
            // TODO: Update for ActiveEnchantCache?
            // OnAttack triggers
            if (mode != EnumInteractMode.Attack) 
            {
                base.OnInteract(byEntity, itemslot, hitPosition, mode, ref handled);
                return;
            }
            // Get Enchantments for the that attacked this entity
            Dictionary<string, int> enchants = sapi.EnchantAccessor().GetActiveEnchantments(itemslot.Itemstack);
            if (enchants != null)
            {
                if (EnchantingConfigLoader.Config?.Debug == true)
                    sapi.Logger.Event("[KRPGEnchantment] {0} was attacked by an enchanted weapon.", entity.GetName());
                
                // Translate Handling through Int32 value in Enchantments.
                // PassThrough = 0, Handled = 1, PreventDefault = 2, PreventSubsequent = 3
                int eHandled = (int)handled;
                float damage = itemslot.Itemstack.Item.AttackPower;
                EnchantModifiers parameters = new EnchantModifiers() { { "damage", damage }, { "handled", eHandled } };
                bool didEnchants = sapi.EnchantAccessor().TryEnchantments(itemslot, "OnAttacked", byEntity, entity, ref parameters);
                if (didEnchants)
                {
                    eHandled = parameters.GetInt("handled");
                    handled = (EnumHandling)eHandled;
                }
                if(EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] Finished processing Enchantments on EnchantmentEntityBehavior.OnInteract().");
            }
            base.OnInteract(byEntity, itemslot, hitPosition, mode, ref handled);
        }
        /// <summary>
        /// Primary trigger call for OnHit enchantments, which is applied BEFORE damage is dealt to HP.
        /// </summary>
        /// <param name="damage"></param>
        /// <param name="damageSource"></param>
        /// <returns></returns>
        public float OnHit(float damage, DamageSource damageSource)
        {
            // Only living players should actually have OnHit triggers
            if (!(Api is ICoreServerAPI sapi)) return damage;
            if (!IsPlayer || !entity.Alive) return damage;

            string dmgType = Enum.GetName(damageSource.Type).ToLower();

            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] {0} is receiving {1} {2} damage. Attempting to trigger OnHit enchantments.",
                    entity.GetName(), damage, dmgType);

            // Push OnHit to each cached Gear enchants
            foreach (KeyValuePair<int, ActiveEnchantCache> pair in GearEnchantCache)
            {
                if (pair.Value.Enchantments == null) continue;

                EnchantModifiers parameters = new EnchantModifiers() { { "damage", damage }, { "type" , dmgType } };
                bool didEnchants =
                    sapi.EnchantAccessor().TryEnchantments(gearInventory[pair.Key]?.Itemstack, "OnHit", damageSource.CauseEntity, entity, pair.Value.Enchantments, ref parameters);
                if (didEnchants) 
                {
                    damage = parameters.GetFloat("damage");
                }
            }

            return damage;
        }
        /// <summary>
        /// Trigger Enchantments on the entity AFTER it has taken damage.
        /// </summary>
        /// <param name="damageSource"></param>
        /// <param name="damage"></param>
        public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
        {
            // Only living players should actually have OnDamaged triggers
            if (!(Api is ICoreServerAPI sapi)) 
            {
                base.OnEntityReceiveDamage(damageSource, ref damage);
                return;
            }
            if (!IsPlayer || !entity.Alive) 
            {
                base.OnEntityReceiveDamage(damageSource, ref damage);
                return;
            }
            
            string dmgType = Enum.GetName(damageSource.Type).ToLower();

            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] {0} received {1} {2} damage. Attempting to trigger OnDamaged enchantments.",
                    entity.GetName(), damage, dmgType);

            // Push OnHit to each cached Gear enchants
            foreach (KeyValuePair<int, ActiveEnchantCache> pair in GearEnchantCache)
            {
                if (pair.Value.Enchantments == null) continue;
                EnchantModifiers parameters = new EnchantModifiers() { { "damage", damage }, { "type", dmgType } };
                bool didEnchants =
                    sapi.EnchantAccessor().TryEnchantments(gearInventory[pair.Key], "OnDamaged", damageSource.CauseEntity, entity, pair.Value.Enchantments);
                if (didEnchants)
                    damage = parameters.GetFloat("damage");
            }
            base.OnEntityReceiveDamage(damageSource, ref damage);
        }
        #endregion
        #region Network
        public override void OnReceivedServerPacket(int packetid, byte[] data, ref EnumHandling handled)
        {
            switch(packetid)
            {
                case 1616: {
                    if (EnchantingConfigLoader.Config?.Debug == true)
                        Api.Logger.Event("[KRPGEnchantment] Received Server packet 1616. Attempting to Particle");
                    ParticlePacket packet = SerializerUtil.Deserialize<ParticlePacket>(data);
                    float amount = packet.Amount;
                    EnumDamageType type = packet.DamageType;
                    GenerateParticles(type, amount);
                    break;
                }
                case 1617: {
                    if (EnchantingConfigLoader.Config?.Debug == true)
                        Api.Logger.Event("[KRPGEnchantment] Received Server packet 1617. Attempting to ");
                    
                    // Saving this for later future endeavors
                    // 
                    // EnchantPacket packet = SerializerUtil.Deserialize<EnchantPacket>(data);
                    // string eCode = packet.Code;
                    // int ePower = packet.Power;

                    break;
                }
            }
        }
        public override void OnReceivedClientPacket(IServerPlayer player, int packetid, byte[] data, ref EnumHandling handled)
        {
            base.OnReceivedClientPacket(player, packetid, data, ref handled);
        }
        #endregion
        #region Stats

        #endregion
        #region TickRegistry
        /// <summary>
        /// Attempt to register a formated EnchantTick into this entity's TickRegistry.
        /// </summary>
        /// <param name="enchant"></param>
        /// <param name="tickDuration"></param>
        /// <param name="persistent"></param>
        /// <param name="isHotbar"></param>
        /// <param name="isOffhand"></param>
        public void RegisterEnchantTick(EnchantmentSource enchant, long tickDuration, bool persistent, bool isHotbar, bool isOffhand)
        {
            // Get ID's
            int stackID = enchant.SourceStack.Item.ItemId;
            int slotID = enchant.SourceSlot.Inventory.GetSlotId(enchant.SourceSlot);
            string codeID = enchant.Code + ":" + slotID + ":" + stackID;
            // Toggle On
            if (!TickRegistry.ContainsKey(codeID))
            {
                EnchantTick eTick = enchant.ToEnchantTick();
                eTick.SlotID = slotID;
                eTick.Persistent = persistent;
                eTick.IsHotbar = isHotbar;
                eTick.IsOffhand = isOffhand;
                eTick.TickDuration = tickDuration;
                TickRegistry.Add(codeID, eTick);
            }
        }
        /// <summary>
        /// Removes all EnchantTicks for the given ItemSLot's contents. Be sure to update cache after.
        /// </summary>
        /// <param name="slotId"></param>
        public void DisposeEnchantTicks(int slotId)
        {
            foreach (KeyValuePair<string, EnchantTick> pair in TickRegistry)
            {
                string eCode = pair.Key;
                if (pair.Key.Contains(":")) eCode = eCode.Split(":")?[1];
                if (eCode == slotId.ToString())
                {
                    TickRegistry[pair.Key].Dispose();
                }
            }
        }
        /// <summary>
        /// Safely removes all EnchantTicks marked as IsTrash.
        /// </summary>
        internal void RemoveTrashEnchantTicks()
        {
            enchantTickLocked = true;
            foreach (KeyValuePair<string, EnchantTick> pair in TickRegistry)
            {
                if (pair.Value.IsTrash == true)
                    TickRegistry.Remove(pair.Key);
            }
            enchantTickLocked = false;
        }
        // public delegate void EnchantTickDelegate(Entity byEntity, EnchantTick eTick);
        // public event EnchantTickDelegate? OnEnchantTick;
        private long lastTickMs = 0;
        private bool enchantTickLocked = true;
        /// <summary>
        /// Returns true if the given ItemStack's Id matches a main hand or off hand hotbar slot containing an item with the same Id.
        /// </summary>
        /// <param name="slotID"></param>
        /// <returns></returns>
        public bool IsHeld(int slotID)
        {
            ItemSlot slot = hotbarInventory[slotID];
            // TODO: Make a better way to identify a unique item
            if (player?.InventoryManager?.ActiveHotbarSlotNumber == slotID && player?.InventoryManager?.ActiveHotbarSlot?.Itemstack == slot?.Itemstack)
                return true;
            if (player?.InventoryManager?.ActiveHotbarSlotNumber == slotID && player?.InventoryManager?.OffhandHotbarSlot?.Itemstack == slot?.Itemstack)
                return true;
            return false;
        }
        /// <summary>
        /// Be sure to lock the registry before 
        /// </summary>
        /// <param name="api"></param>
        private void ProcessServerTickRegistry(ICoreServerAPI api)
        {
            // 0. Safety Checks
            // Verify the TickRegistry
            if (TickRegistry?.Count <= 0) return;
            // 1. Prepare time
            long tickStart = api.World.ElapsedMilliseconds;
            // 2. Loop Registry
            foreach (KeyValuePair<string, EnchantTick> pair in TickRegistry)
            {
                // 2a. Skip Trash ticks
                if (pair.Value.IsTrash == true)
                    continue;
                // 2b. Hotbar Checks
                // Don't run if it's on hotbar, but unselected & not in the offhand
                if ((pair.Value.IsHotbar == true || pair.Value.IsOffhand == true) && !IsHeld(pair.Value.SlotID))
                    continue;
                // 2c. Duration Checks
                long curDur = tickStart - pair.Value.LastTickTime;
                if (!(curDur >= pair.Value.TickDuration)) continue;
                if (EnchantingConfigLoader.Config?.Debug == true)
                    api.Logger.Event("[KRPGEnchantment] {0} is being ticked.", pair.Key);
                // 2d. Process the tick if it meets run conditions
                // Handle OnTick() or remove from the registry if expired.
                // Be sure to handle your EnchantTick updates (LastTickTime, TicksRemaining, etc. in OnTick())
                if (pair.Value.TicksRemaining > 0 || pair.Value.Persistent == true)
                {
                    // 2d1. Mark the tick as trash if we cannot resolve the Enchantment
                    IEnchantment enchant = api.EnchantAccessor().GetEnchantment(pair.Value.Code);
                    if (enchant == null)
                    {
                        api.Logger.Error("[KRPGEnchantment] Failed to get the required IEnchantment. Removing this tick.");
                        pair.Value.Dispose();
                        continue;
                    }
                    // 2d2. Trigger OnTick
                    if (EnchantingConfigLoader.Config?.Debug == true)
                        api.Logger.Event("[KRPGEnchantment] {0} is being triggered.", pair.Value.Code);
                    EnchantTick eTick = pair.Value;
                    enchant.OnTick(ref eTick);
                    TickRegistry[pair.Key] = eTick;
                }
                // 2e. Mark the tick as trash if it does not meet run conditions
                else
                {
                    if (EnchantingConfigLoader.Config?.Debug == true)
                        api.Logger.Event("[KRPGEnchantment] Enchantment finished Ticking for {0}.", pair.Value.Code);
                    pair.Value.Dispose();
                    continue;
                }
            }
        }
        public override void OnGameTick(float dt)
        {
            // 0. Verify the tick rate limiter
            long curTime = Api.World.ElapsedMilliseconds;
            if (!((curTime - lastTickMs) >= TickTime)) return;
            // 1. Only on the server
            if (!(Api is ICoreServerAPI sapi)) return;
            // 2. Lock toggle
            if (enchantTickLocked == true) return;
            else enchantTickLocked = true;
            // Disable the toggler, for it is too aggressive
            // 3. Held Item Toggles (Players Only)
            // if (IsPlayer) ToggleHeldItems();
            // 3. Recalculate the stats of a player
            // if (IsPlayer) RecalculateEntityStats();
            // 4. Process ticks - Server only right now
            ProcessServerTickRegistry(sapi);
            // 5. Trash Removal - This will unlock the EnchantTicks
            RemoveTrashEnchantTicks();
        }
        #endregion
        #region Particles
        public void GenerateParticles(EnumDamageType damageType, float damage)
        {
            if (!(Api is ICoreClientAPI capi)) return;

            if (EnchantingConfigLoader.Config?.Debug == true)
                capi?.Logger.Event("[KRPGEnchantment] Enchantment is generating particles for entity {0}.", entity.EntityId);

            KRPGEnchantmentSystem eSys = capi.ModLoader.GetModSystem<KRPGEnchantmentSystem>();

            int power = (int)MathF.Ceiling(damage);

            if (damageType == EnumDamageType.Fire)
            {
                int r = capi.World.Rand.Next(eSys.FireParticleProps.Length + 1);
                int num = Math.Min(eSys.FireParticleProps.Length - 1, r);
                AdvancedParticleProperties advancedParticleProperties = eSys.FireParticleProps[num];
                advancedParticleProperties.basePos.Set(entity.Pos.X, entity.Pos.Y + (double)(entity.SelectionBox.YSize / 2f), entity.Pos.Z);
                advancedParticleProperties.PosOffset[0].var = entity.SelectionBox.XSize / 2f;
                advancedParticleProperties.PosOffset[1].var = entity.SelectionBox.YSize / 2f;
                advancedParticleProperties.PosOffset[2].var = entity.SelectionBox.ZSize / 2f;
                advancedParticleProperties.Velocity[0].avg = (float)entity.Pos.Motion.X * 10f;
                advancedParticleProperties.Velocity[1].avg = (float)entity.Pos.Motion.Y * 5f;
                advancedParticleProperties.Velocity[2].avg = (float)entity.Pos.Motion.Z * 10f;
                advancedParticleProperties.Quantity.avg = GameMath.Sqrt(advancedParticleProperties.PosOffset[0].var + advancedParticleProperties.PosOffset[1].var + advancedParticleProperties.PosOffset[2].var) * num switch
                {
                    1 => 3f,
                    0 => 0.5f,
                    _ => 1.25f,
                };
                for (int i = 0; i <= power; i++)
                {
                    capi.World.SpawnParticles(advancedParticleProperties);
                }
            }
            if (damageType == EnumDamageType.Frost)
            {
                int r = capi.World.Rand.Next(eSys.FrostParticleProps.Length + 1);
                int num = Math.Min(eSys.FrostParticleProps.Length - 1, r);
                AdvancedParticleProperties advancedParticleProperties = eSys.FrostParticleProps[num];
                advancedParticleProperties.basePos.Set(entity.Pos.X, entity.Pos.Y + (double)(entity.SelectionBox.YSize / 2f), entity.Pos.Z);
                advancedParticleProperties.PosOffset[0].var = entity.SelectionBox.XSize / 2f;
                advancedParticleProperties.PosOffset[1].var = entity.SelectionBox.YSize / 2f;
                advancedParticleProperties.PosOffset[2].var = entity.SelectionBox.ZSize / 2f;
                advancedParticleProperties.Velocity[0].avg = (float)entity.Pos.Motion.X * 10f;
                advancedParticleProperties.Velocity[1].avg = (float)entity.Pos.Motion.Y * 5f;
                advancedParticleProperties.Velocity[2].avg = (float)entity.Pos.Motion.Z * 10f;
                advancedParticleProperties.Quantity.avg = GameMath.Sqrt(advancedParticleProperties.PosOffset[0].var + advancedParticleProperties.PosOffset[1].var + advancedParticleProperties.PosOffset[2].var) * num switch
                {
                    1 => 3f,
                    0 => 0.5f,
                    _ => 1.25f,
                };
                for (int i = 0; i <= power; i++)
                {
                    capi.World.SpawnParticles(advancedParticleProperties);
                }
            }
            if (damageType == EnumDamageType.Electricity)
            {
                int r = capi.World.Rand.Next(eSys.ElectricityParticleProps.Length + 1);
                int num = Math.Min(eSys.ElectricityParticleProps.Length - 1, r);
                AdvancedParticleProperties advancedParticleProperties = eSys.ElectricityParticleProps[num];
                advancedParticleProperties.basePos.Set(entity.Pos.X, entity.Pos.Y + (double)(entity.SelectionBox.YSize / 2f), entity.Pos.Z);
                advancedParticleProperties.PosOffset[0].var = entity.SelectionBox.XSize / 2f;
                advancedParticleProperties.PosOffset[1].var = entity.SelectionBox.YSize / 2f;
                advancedParticleProperties.PosOffset[2].var = entity.SelectionBox.ZSize / 2f;
                advancedParticleProperties.Velocity[0].avg = (float)entity.Pos.Motion.X * 10f;
                advancedParticleProperties.Velocity[1].avg = (float)entity.Pos.Motion.Y * 5f;
                advancedParticleProperties.Velocity[2].avg = (float)entity.Pos.Motion.Z * 10f;
                advancedParticleProperties.Quantity.avg = GameMath.Sqrt(advancedParticleProperties.PosOffset[0].var + advancedParticleProperties.PosOffset[1].var + advancedParticleProperties.PosOffset[2].var) * num switch
                {
                    1 => 3f,
                    0 => 0.5f,
                    _ => 1.25f,
                };
                for (int i = 0; i <= power; i++)
                {
                    capi.World.SpawnParticles(advancedParticleProperties);
                }
            }
            if (damageType == EnumDamageType.Heal)
            {
                int r = capi.World.Rand.Next(eSys.HealParticleProps.Length + 1);
                int num = Math.Min(eSys.HealParticleProps.Length - 1, r);
                AdvancedParticleProperties advancedParticleProperties = eSys.HealParticleProps[num];
                advancedParticleProperties.basePos.Set(entity.Pos.X, entity.Pos.Y + (double)(entity.SelectionBox.YSize / 2f), entity.Pos.Z);
                advancedParticleProperties.PosOffset[0].var = entity.SelectionBox.XSize / 2f;
                advancedParticleProperties.PosOffset[1].var = entity.SelectionBox.YSize / 2f;
                advancedParticleProperties.PosOffset[2].var = entity.SelectionBox.ZSize / 2f;
                advancedParticleProperties.Velocity[0].avg = (float)entity.Pos.Motion.X * 10f;
                advancedParticleProperties.Velocity[1].avg = (float)entity.Pos.Motion.Y * 5f;
                advancedParticleProperties.Velocity[2].avg = (float)entity.Pos.Motion.Z * 10f;
                advancedParticleProperties.Quantity.avg = GameMath.Sqrt(advancedParticleProperties.PosOffset[0].var + advancedParticleProperties.PosOffset[1].var + advancedParticleProperties.PosOffset[2].var) * num switch
                {
                    1 => 3f,
                    0 => 0.5f,
                    _ => 1.25f,
                };
                for (int i = 0; i <= power; i++)
                {
                    capi.World.SpawnParticles(advancedParticleProperties);
                }
            }
            if (damageType == EnumDamageType.Injury)
            {
                int r = capi.World.Rand.Next(eSys.InjuryParticleProps.Length + 1);
                int num = Math.Min(eSys.InjuryParticleProps.Length - 1, r);
                AdvancedParticleProperties advancedParticleProperties = eSys.InjuryParticleProps[num];
                advancedParticleProperties.basePos.Set(entity.Pos.X, entity.Pos.Y + (double)(entity.SelectionBox.YSize / 2f), entity.Pos.Z);
                advancedParticleProperties.PosOffset[0].var = entity.SelectionBox.XSize / 2f;
                advancedParticleProperties.PosOffset[1].var = entity.SelectionBox.YSize / 2f;
                advancedParticleProperties.PosOffset[2].var = entity.SelectionBox.ZSize / 2f;
                advancedParticleProperties.Velocity[0].avg = (float)entity.Pos.Motion.X * 10f;
                advancedParticleProperties.Velocity[1].avg = (float)entity.Pos.Motion.Y * 5f;
                advancedParticleProperties.Velocity[2].avg = (float)entity.Pos.Motion.Z * 10f;
                advancedParticleProperties.Quantity.avg = GameMath.Sqrt(advancedParticleProperties.PosOffset[0].var + advancedParticleProperties.PosOffset[1].var + advancedParticleProperties.PosOffset[2].var) * num switch
                {
                    1 => 3f,
                    0 => 0.5f,
                    _ => 1.25f,
                };
                for (int i = 0; i <= power; i++)
                {
                    capi.World.SpawnParticles(advancedParticleProperties);
                }
            }
            if (damageType == EnumDamageType.Poison)
            {
                int r = capi.World.Rand.Next(eSys.PoisonParticleProps.Length + 1);
                int num = Math.Min(eSys.PoisonParticleProps.Length - 1, r);
                AdvancedParticleProperties advancedParticleProperties = eSys.PoisonParticleProps[num];
                advancedParticleProperties.basePos.Set(entity.Pos.X, entity.Pos.Y + (double)(entity.SelectionBox.YSize / 2f), entity.Pos.Z);
                advancedParticleProperties.PosOffset[0].var = entity.SelectionBox.XSize / 2f;
                advancedParticleProperties.PosOffset[1].var = entity.SelectionBox.YSize / 2f;
                advancedParticleProperties.PosOffset[2].var = entity.SelectionBox.ZSize / 2f;
                advancedParticleProperties.Velocity[0].avg = (float)entity.Pos.Motion.X * 10f;
                advancedParticleProperties.Velocity[1].avg = (float)entity.Pos.Motion.Y * 5f;
                advancedParticleProperties.Velocity[2].avg = (float)entity.Pos.Motion.Z * 10f;
                advancedParticleProperties.Quantity.avg = GameMath.Sqrt(advancedParticleProperties.PosOffset[0].var + advancedParticleProperties.PosOffset[1].var + advancedParticleProperties.PosOffset[2].var) * num switch
                {
                    1 => 3f,
                    0 => 0.5f,
                    _ => 1.25f,
                };
                for (int i = 0; i <= power; i++)
                {
                    capi.World.SpawnParticles(advancedParticleProperties);
                }
            }
        }
        #endregion
    }
}