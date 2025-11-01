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
using CombatOverhaul.Armor;
using System.Reflection.Metadata;
using Vintagestory.API.Util;
using Cairo.Freetype;
using System.Numerics;
using Vintagestory.API.Datastructures;
using static System.Net.Mime.MediaTypeNames;
using System.Data;
using CombatOverhaul.MeleeSystems;
using System.Collections;
using Cairo;

namespace KRPGLib.Enchantment
{
    public class EnchantmentEntityBehavior : EntityBehavior
    {
        public override string PropertyName() { return "EnchantmentEntityBehavior"; }
        public ICoreAPI Api;
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
                
                return player.InventoryManager.GetOwnInventory("character");
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
        public EnchantmentEntityBehavior(Entity entity) : base(entity)
        {
            Api = entity.Api;

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
        public override void OnEntityDeath(DamageSource damageSourceForDeath)
        {
            base.OnEntityDeath(damageSourceForDeath);
            // TODO: Make a way to check if the ticks should be cleared on death or not.
            // TickRegistry?.Clear();
        }
        public override void OnEntityDespawn(EntityDespawnData despawn)
        {
            base.OnEntitySpawn();

            if (!(Api is ICoreServerAPI sapi)) return;

            if (IsPlayer)
            {
                entity.GetBehavior<EntityBehaviorHealth>().onDamaged -= OnHit;
                gearInventory.SlotModified -= OnGearModified;
                hotbarInventory.SlotModified -= OnHotbarModified;
                sapi.World.UnregisterGameTickListener(onTickID);
            }
        }
        public void GenerateGearEnchantCache(int slotId)
        {
            if (!(Api is ICoreServerAPI sapi)) return;
            // Don't generate cache for null slots or inventories
            if (gearInventory?[slotId] == null) return;
            ItemSlot slot = gearInventory[slotId];
            // Generate Cache if it worked
            ActiveEnchantCache cache = new ActiveEnchantCache();
            cache.Enchantments = Api.EnchantAccessor().GetActiveEnchantments(slot.Itemstack);
            cache.LastCheckTime = Api.World.ElapsedMilliseconds;
            if (gearInventory[slotId].Empty == true)
                cache.ItemId = -1;
            else
                cache.ItemId = gearInventory[slotId].Itemstack.Id;
            // Set the cache
            if (GearEnchantCache.ContainsKey(slotId) == true)
                GearEnchantCache[slotId] = cache;
            else
                GearEnchantCache.Add(slotId, cache);
        }
        public void GenerateHotbarEnchantCache(int slotId)
        {
            if (!(Api is ICoreServerAPI sapi)) return;
            // Don't generate cache for null slots or inventories
            if (hotbarInventory?[slotId] == null) return;
            ItemSlot slot = hotbarInventory[slotId];
            // Generate Cache if it worked
            ActiveEnchantCache cache = new ActiveEnchantCache();
            cache.Enchantments = Api.EnchantAccessor().GetActiveEnchantments(slot.Itemstack);
            cache.LastCheckTime = Api.World.ElapsedMilliseconds;
            if (hotbarInventory[slotId].Empty == true)
                cache.ItemId = -1;
            else
                cache.ItemId = hotbarInventory[slotId].Itemstack.Id;
            // Set the cache
            if (HotbarEnchantCache.ContainsKey(slotId) == true)
                HotbarEnchantCache[slotId] = cache;
            else
                HotbarEnchantCache.Add(slotId, cache);
        }
        #region Triggers
        public void OnGearModified(int slotId)
        {
            // Sanity Checks
            if (!(Api is ICoreServerAPI sapi)) return;
            if (entity?.Alive != true) return;
            if (gearInventory?[slotId] == null) return;

            // 1. If Slot is empty, Remove any ticks registered to the slot
            if (gearInventory?[slotId]?.Empty == true)
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
                // Update the cache
                GenerateGearEnchantCache(slotId);
                // Don't trigger OnEquip
                return;
            }

            // 2. Item is still equipped
            if (gearInventory?[slotId]?.Itemstack?.Id == GearEnchantCache[slotId]?.ItemId) return;

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
                // 4. Update the cache
                GenerateGearEnchantCache(slotId);
            }
        }
        public void OnHotbarModified(int slotId)
        {
            // 0. Sanity Check
            if (!(Api is ICoreServerAPI sapi)) return;
            if (entity?.Alive != true) return;
            if (hotbarInventory?[slotId] == null) return;

            // 1. If Slot is empty, Remove any ticks registered to the slot
            if (hotbarInventory?[slotId]?.Empty == true)
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
                // Update the cache
                GenerateHotbarEnchantCache(slotId);
                // Don't trigger OnEquip
                return;
            }

            // 2. Item is still equipped
            if (hotbarInventory?[slotId]?.Itemstack?.Id == HotbarEnchantCache[slotId]?.ItemId) return;

            // 11. Offhand, probably
            // int activeSlot = player.InventoryManager.ActiveHotbarSlotNumber;
            // if (activeSlot != (slotId | 11)) return;

            // 3. Check if this is the Offhand slot
            bool isOffhand = false;
            if (player.InventoryManager.OffhandHotbarSlot?.Itemstack?.Id == hotbarInventory[slotId].Itemstack.Id)
                isOffhand = true;

            // 4. Trigger OnEquip for any Enchantments
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] Player {0} modified hotbar slot {1}. Attempting to trigger OnEquip enchantments.", player.PlayerUID, slotId);

            EnchantModifiers parameters = new EnchantModifiers() { { "IsHotbar", true }, { "IsOffhand", isOffhand } };
            bool didEnchants = sapi.EnchantAccessor().TryEnchantments(hotbarInventory[slotId], "OnEquip", entity, entity, ref parameters);
            if (didEnchants == true)
            {
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] {0} is generating an ActiveEnchantCache for {1}.", entity.GetName(), hotbarInventory[slotId].Itemstack?.GetName());
            }

            // 5. Update the cache
            GenerateHotbarEnchantCache(slotId);
        }
        public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
        {
            if (mode != EnumInteractMode.Attack || itemslot.Empty == true || entity?.World.Side != EnumAppSide.Server)
                return;

            // Get Enchantments
            handled = EnumHandling.Handled;
            Dictionary<string, int> enchants = Api.EnchantAccessor().GetActiveEnchantments(itemslot.Itemstack);
            if (enchants != null)
            {
                if (EnchantingConfigLoader.Config?.Debug == true)
                    byEntity.Api.Logger.Event("[KRPGEnchantment] {0} was attacked by an enchanted weapon.", entity.GetName());

                ICoreServerAPI sapi = Api as ICoreServerAPI;
                // Translate Handling through Int value in Enchantments.
                // PassThrough = 0, Handled = 1, PreventDefault = 2, PreventSubsequent = 3
                int eHandled = (int)handled;
                EnchantModifiers parameters = new EnchantModifiers() { { "handled", eHandled } };
                bool didEnchants = sapi.EnchantAccessor().TryEnchantments(itemslot, "OnAttack", byEntity, entity, enchants, ref parameters);
                if (didEnchants)
                {
                    eHandled = parameters.GetInt("handled");
                    handled = (EnumHandling)eHandled;
                }
            }
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
        public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
        {
            // Only living players should actually have OnDamaged triggers
            if (!(Api is ICoreServerAPI sapi)) return;
            if (!IsPlayer || !entity.Alive) return;

            float dmg = damage;
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
                    dmg = parameters.GetFloat("damage");
            }
        }

        // public delegate void EnchantTickDelegate(Entity byEntity, EnchantTick eTick);
        // public event EnchantTickDelegate? OnEnchantTick;
        private long lastTickMs = 0;
        public override void OnGameTick(float dt)
        {
            // 0. Safety check
            if (!(Api is ICoreServerAPI sapi)) return;
            if (TickRegistry?.Count <= 0) return;
            // if (dt < TickTime) return;
            long curTime = sapi.World.ElapsedMilliseconds;
            if (!((curTime - lastTickMs) >= TickTime)) return;

            // Can we use WatchedAttributes? 
            // ITreeAttribute enchantTicks = entity.WatchedAttributes.GetOrAddTreeAttribute("EnchantTicks");
            // if (enchantTicks.Values.Length <= 0) return;
            // if (EnchantingConfigLoader.Config?.Debug == true)
            //     Api.Logger.Event("[KRPGEnchantment] {0} is attempting to tick over Tick Registry.", entity.GetName());

            // 1. Prepare Garbage & time
            List<string> tickBin = new List<string>();
            long tickStart = sapi.World.ElapsedMilliseconds;
            // 2. Loop TickRegistry
            // foreach (IAttribute attribute in enchantTicks.Values)
            foreach (KeyValuePair<string, EnchantTick> pair in TickRegistry)
            {
                
                // 2a. Trash Checks
                // If marked to be removed
                if (pair.Value.IsTrash == true)
                {
                    tickBin.Add(pair.Key);
                    continue;
                }
                // 2b. Hotbar Checks
                // Saving this here for FYI
                // string invId = "hotbar-" + this.player.PlayerUID;
                // Don't run if it's on hotbar, but unselected & not in the offhand
                if (pair.Value.IsHotbar == true || pair.Value.IsOffhand == true)
                {
                    if (pair.Value.ItemID != player.InventoryManager.ActiveHotbarSlot.Itemstack?.Id
                    && pair.Value.ItemID != player.InventoryManager.OffhandHotbarSlot.Itemstack?.Id)
                    continue;
                }
                // 2c. Duration Checks
                long curDur = tickStart - pair.Value.LastTickTime;
                if (!(curDur >= pair.Value.TickDuration)) continue;

                if (EnchantingConfigLoader.Config?.Debug == true)
                    sapi.Logger.Event("[KRPGEnchantment] {0} is being ticked.", pair.Key);

                // 2c. Process the tick if it meets run conditions
                // Handle OnTick() or remove from the registry if expired.
                // Be sure to handle your EnchantTick updates (LastTickTime, TicksRemaining, etc. in OnTick())
                if (pair.Value.TicksRemaining > 0 || pair.Value.Persistent == true)
                {
                    // 2c1. Mark the tick as trash if we cannot resolve the Enchantment
                    IEnchantment enchant = sapi.EnchantAccessor().GetEnchantment(pair.Value.Code);
                    if (enchant == null)
                    {
                        sapi.Logger.Error("[KRPGEnchantment] Failed to get the required IEnchantment. Removing this tick.");
                        pair.Value.Dispose();
                        tickBin.Add(pair.Key);
                        continue;
                    }
                    // enchant.Api = sapi;
                    // 2c2. Trigger OnTick
                    if (EnchantingConfigLoader.Config?.Debug == true)
                        sapi.Logger.Event("[KRPGEnchantment] {0} is being triggered.", pair.Value.Code);
                    EnchantTick eTick = pair.Value;
                    enchant.OnTick(ref eTick);
                    TickRegistry[pair.Key] = eTick;
                }
                // 2d. Mark the tick as trash if it does not meet run conditions
                else
                {
                    if (EnchantingConfigLoader.Config?.Debug == true)
                        sapi.Logger.Event("[KRPGEnchantment] Enchantment finished Ticking for {0}.", pair.Value.Code);
                    pair.Value.Dispose();
                    tickBin.Add(pair.Key);
                    continue;
                }
            }
            // 3. Take out the trash
            foreach (string s in tickBin)
            {
                TickRegistry.Remove(s);
                // enchantTicks.RemoveAttribute(s);
            }
            // 4. Write back to the entity
            // entity.WatchedAttributes.MergeTree(enchantTicks);
        }
        #endregion
        #region Particles
        public override void OnReceivedServerPacket(int packetid, byte[] data, ref EnumHandling handled)
        {
            if (packetid == 1616)
            {
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] Received Server packet 1616. Attempting to Particle");
                ParticlePacket packet = SerializerUtil.Deserialize<ParticlePacket>(data);
                float amount = packet.Amount;
                EnumDamageType type = packet.DamageType;
                GenerateParticles(type, amount);
            }
        }
        public void GenerateParticles(EnumDamageType damageType, float damage)
        {
            if (!(Api is ICoreClientAPI api)) return;

            if (EnchantingConfigLoader.Config?.Debug == true)
                api?.Logger.Event("[KRPGEnchantment] Enchantment is generating particles for entity {0}.", entity.EntityId);

            KRPGEnchantmentSystem eSys = api.ModLoader.GetModSystem<KRPGEnchantmentSystem>();

            int power = (int)MathF.Ceiling(damage);

            if (damageType == EnumDamageType.Fire)
            {
                int r = api.World.Rand.Next(eSys.FireParticleProps.Length + 1);
                int num = Math.Min(eSys.FireParticleProps.Length - 1, r);
                AdvancedParticleProperties advancedParticleProperties = eSys.FireParticleProps[num];
                advancedParticleProperties.basePos.Set(entity.SidedPos.X, entity.SidedPos.Y + (double)(entity.SelectionBox.YSize / 2f), entity.Pos.Z);
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
                    api.World.SpawnParticles(advancedParticleProperties);
                }
            }
            if (damageType == EnumDamageType.Frost)
            {
                int r = api.World.Rand.Next(eSys.FrostParticleProps.Length + 1);
                int num = Math.Min(eSys.FrostParticleProps.Length - 1, r);
                AdvancedParticleProperties advancedParticleProperties = eSys.FrostParticleProps[num];
                advancedParticleProperties.basePos.Set(entity.SidedPos.X, entity.SidedPos.Y + (double)(entity.SelectionBox.YSize / 2f), entity.Pos.Z);
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
                    api.World.SpawnParticles(advancedParticleProperties);
                }
            }
            if (damageType == EnumDamageType.Electricity)
            {
                int r = api.World.Rand.Next(eSys.ElectricityParticleProps.Length + 1);
                int num = Math.Min(eSys.ElectricityParticleProps.Length - 1, r);
                AdvancedParticleProperties advancedParticleProperties = eSys.ElectricityParticleProps[num];
                advancedParticleProperties.basePos.Set(entity.SidedPos.X, entity.SidedPos.Y + (double)(entity.SelectionBox.YSize / 2f), entity.Pos.Z);
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
                    api.World.SpawnParticles(advancedParticleProperties);
                }
            }
            if (damageType == EnumDamageType.Heal)
            {
                int r = api.World.Rand.Next(eSys.HealParticleProps.Length + 1);
                int num = Math.Min(eSys.HealParticleProps.Length - 1, r);
                AdvancedParticleProperties advancedParticleProperties = eSys.HealParticleProps[num];
                advancedParticleProperties.basePos.Set(entity.SidedPos.X, entity.SidedPos.Y + (double)(entity.SelectionBox.YSize / 2f), entity.Pos.Z);
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
                    api.World.SpawnParticles(advancedParticleProperties);
                }
            }
            if (damageType == EnumDamageType.Injury)
            {
                int r = api.World.Rand.Next(eSys.InjuryParticleProps.Length + 1);
                int num = Math.Min(eSys.InjuryParticleProps.Length - 1, r);
                AdvancedParticleProperties advancedParticleProperties = eSys.InjuryParticleProps[num];
                advancedParticleProperties.basePos.Set(entity.SidedPos.X, entity.SidedPos.Y + (double)(entity.SelectionBox.YSize / 2f), entity.Pos.Z);
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
                    api.World.SpawnParticles(advancedParticleProperties);
                }
            }
            if (damageType == EnumDamageType.Poison)
            {
                int r = api.World.Rand.Next(eSys.PoisonParticleProps.Length + 1);
                int num = Math.Min(eSys.PoisonParticleProps.Length - 1, r);
                AdvancedParticleProperties advancedParticleProperties = eSys.PoisonParticleProps[num];
                advancedParticleProperties.basePos.Set(entity.SidedPos.X, entity.SidedPos.Y + (double)(entity.SelectionBox.YSize / 2f), entity.Pos.Z);
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
                    api.World.SpawnParticles(advancedParticleProperties);
                }
            }
        }
#endregion
    }
}