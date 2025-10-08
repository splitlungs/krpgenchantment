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

namespace KRPGLib.Enchantment
{
    public class EnchantmentEntityBehavior : EntityBehavior
    {
        public override string PropertyName() { return "EnchantmentEntityBehavior"; }
        public ICoreAPI Api;
        public ICoreClientAPI cApi;
        public ICoreServerAPI sApi;
        public int TickTime = 250;

        private EntityAgent agent;
        private IServerPlayer player = null;
        
        public  IInventory gearInventory = null;
        public  IInventory hotbarInventory = null;
        
        public Dictionary<int, ActiveEnchantCache> GearEnchantCache = null;
        public Dictionary<int, ActiveEnchantCache> HotbarEnchantCache = null;
        public Dictionary<string, EnchantTick> TickRegistry;
        public bool IsPlayer { get { if (player != null) return true; return false; } }
        public EnchantmentEntityBehavior(Entity entity, int tickTime) : base(entity)
        {
            Api = entity.Api;
            sApi = entity.Api as ICoreServerAPI;
            agent = entity as EntityAgent;
            TickRegistry = new Dictionary<string, EnchantTick>();
            
            ConfigParticles();

            TickTime = tickTime;
            Api.World.RegisterGameTickListener(OnTick, TickTime);
        }
        public void RegisterPlayer(IServerPlayer byPlayer)
        {
            if (!(Api is ICoreServerAPI sapi)) return;

            // Save the IServerPlayer
            player = byPlayer;

            // Register Gear inventory
            gearInventory = player.InventoryManager.GetOwnInventory("character");
            if (gearInventory != null)
            {
                GearEnchantCache = new Dictionary<int, ActiveEnchantCache>();
                foreach (ItemSlot slot in gearInventory)
                {
                    // Generate Cache
                    ActiveEnchantCache cache = new ActiveEnchantCache();
                    int slotId = gearInventory.GetSlotId(slot);
                    if (slot.Empty != true)
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
            // Register Hotbar inventory
            hotbarInventory = player.InventoryManager.GetHotbarInventory();
            if (hotbarInventory != null)
            {
                HotbarEnchantCache = new Dictionary<int, ActiveEnchantCache>();
                foreach (ItemSlot slot in hotbarInventory)
                {
                    int slotId = hotbarInventory.GetSlotId(slot);
                    if (slot.Empty != true)
                    {
                        // Trigger OnEquip since we just logged in
                        EnchantModifiers parameters = new EnchantModifiers() { { "IsHotbar", false } };
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
            TickRegistry.Clear();
        }
        public override void OnEntityDespawn(EntityDespawnData despawn)
        {
            if (entity?.World.Side != EnumAppSide.Server) return;

            if (gearInventory != null)
            {
                gearInventory.SlotModified -= OnGearModified;
            }
            if (hotbarInventory != null)
            {
                hotbarInventory.SlotModified -= OnHotbarModified;
            }
            if (IsPlayer == true)
            {
                entity.GetBehavior<EntityBehaviorHealth>().onDamaged += OnHit;
            }
            base.OnEntityDespawn(despawn);
        }
        public void GenerateGearEnchantCache(int slotId)
        {
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
            if (entity?.Alive != true || entity?.World.Side != EnumAppSide.Server) return;
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
                        TickRegistry.Remove(pair.Key);
                    }
                }
                // Update the cache
                GenerateGearEnchantCache(slotId);
                // Don't trigger OnEquip
                return;
            }

            // 2. Item is still equipped
            if (gearInventory?[slotId]?.Itemstack?.Id == GearEnchantCache[slotId]?.ItemId) return;

            // OBSOLETE
            // foreach (KeyValuePair<string, EnchantTick> pair in TickRegistry)
            // {
            //     string eCode = pair.Key;
            //     if (pair.Key.Contains(":"))
            //     {
            //         eCode = eCode.Split(":")?[2];
            //         // Don't trigger OnEquip if matching
            //         if (eCode == gearInventory?[slotId]?.Itemstack?.Id.ToString()) return;
            //     }
            // }

            // Armor slots, probably
            // 12.Head 13.Body 14.Legs
            // int[] wearableSlots = new int[3] { 12, 13, 14 };

            // 3. Trigger OnEquip for any Enchantments
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] Player {0} gear modified slot {1}. Attempting to trigger OnEquip enchantments.", player.PlayerUID, slotId);
            // ItemSlot slot = gearInventory[slotId];
            EnchantModifiers parameters = new EnchantModifiers() { { "IsHotbar", false } };
            bool didEnchants = sApi.EnchantAccessor().TryEnchantments(gearInventory[slotId], "OnEquip", entity, entity, ref parameters);
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
            // Sanity Check
            if (entity?.Alive != true || entity?.World.Side != EnumAppSide.Server) return;
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
                        TickRegistry.Remove(pair.Key);
                    }
                }
                // Update the cache
                GenerateHotbarEnchantCache(slotId);
                // Don't trigger OnEquip
                return;
            }

            // 2. Item is still equipped
            if (hotbarInventory?[slotId]?.Itemstack?.Id == HotbarEnchantCache[slotId]?.ItemId) return;

            // 3. Process ticks - OBSOLETE?
            // foreach (KeyValuePair<string, EnchantTick> pair in TickRegistry)
            // {
            //     string eCode = pair.Key;
            //     if (pair.Key.Contains(":"))
            //     {
            //         eCode = eCode.Split(":")?[2];
            //         // Don't trigger OnEquip if matching
            //         if (eCode == hotbarInventory?[slotId]?.Itemstack?.Id.ToString()) return;
            //     }
            // }

            // 11. Offhand, probably
            // int activeSlot = player.InventoryManager.ActiveHotbarSlotNumber;
            // if (activeSlot != (slotId | 11)) return;

            // 4. Trigger OnEquip for any Enchantments
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] Player {0} modified hotbar slot {1}. Attempting to trigger OnEquip enchantments.", player.PlayerUID, slotId);

            EnchantModifiers parameters = new EnchantModifiers() { { "IsHotbar", true } };
            bool didEnchants = sApi.EnchantAccessor().TryEnchantments(hotbarInventory[slotId], "OnEquip", entity, entity, ref parameters);
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
            if (mode == EnumInteractMode.Attack && itemslot.Itemstack != null && entity.Api.Side == EnumAppSide.Server)
            {
                if (EnchantingConfigLoader.Config?.Debug == true)
                    byEntity.Api.Logger.Event("[KRPGEnchantment] {0} was attacked by an enchanted weapon.", entity.GetName());

                // Creative check
                // if (player?.WorldData?.CurrentGameMode != EnumGameMode.Survival)
                // {
                //     handled = EnumHandling.Handled;
                //     return;
                // }

                // Get Enchantments
                Dictionary<string, int> enchants = byEntity.Api.EnchantAccessor().GetActiveEnchantments(itemslot.Itemstack);
                if (enchants != null)
                {
                    ICoreServerAPI sapi = Api as ICoreServerAPI;

                    // Should avoid default during healing
                    List<string> cats = sapi.EnchantAccessor().GetEnchantmentCategories();
                    bool preventDefault = false;
                    foreach (string cat in cats) 
                    {
                        if (cat.ToLower().Contains("heal"))
                            preventDefault = true;
                    }
                    if (preventDefault == true)
                        handled = EnumHandling.PreventDefault;
                    else
                        handled = EnumHandling.Handled;
                    
                    EnchantModifiers parameters = new EnchantModifiers();
                    bool didEnchants = sapi.EnchantAccessor().TryEnchantments(itemslot, "OnAttack", byEntity, entity, ref parameters);
                }
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
            if (!IsPlayer || !entity.Alive || entity.World?.Side != EnumAppSide.Server) return damage;

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
                    sApi.EnchantAccessor().TryEnchantments(gearInventory[pair.Key]?.Itemstack, "OnHit", damageSource.CauseEntity, entity, pair.Value.Enchantments, ref parameters);
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
            if (!IsPlayer || !entity.Alive || entity.World?.Side != EnumAppSide.Server) return;

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
                    sApi.EnchantAccessor().TryEnchantments(gearInventory[pair.Key], "OnDamaged", damageSource.CauseEntity, entity, pair.Value.Enchantments);
                if (didEnchants)
                    dmg = parameters.GetFloat("damage");
            }
                base.OnEntityReceiveDamage(damageSource, ref dmg);
        }
        public void OnTick(float deltaTime)
        {
            if (entity?.World?.Side != EnumAppSide.Server || TickRegistry?.Count == 0) return;
            
            // if (EnchantingConfigLoader.Config?.Debug == true)
            //     Api.Logger.Event("[KRPGEnchantment] {0} is attempting to tick over Tick Registry.", entity.GetName());

            foreach (KeyValuePair<string, EnchantTick> pair in TickRegistry)
            {
                // Don't run if it's on hotbar, but unselected & not in the offhand
                if (pair.Value?.IsHotbar == true
                    && pair.Value?.Source?.SourceStack?.Id != player?.InventoryManager?.ActiveHotbarSlot?.Itemstack?.Id
                    && pair.Value?.Source?.SourceSlot?.StorageType != EnumItemStorageFlags.Offhand)
                    continue;

                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] {0} is being ticked.", pair.Key);

                // Handle OnTick() or remove from the registry if expired.
                // Be sure to handle your EnchantTick updates (LastTickTime, TicksRemaining, etc. in OnTick())
                string eCode = pair.Key;
                int tr = pair.Value.TicksRemaining;
                if (tr > 0 || pair.Value.Persistent == true)
                {
                    if (pair.Key.Contains(":")) eCode = eCode.Split(":")?[0];
                    IEnchantment enchant = sApi.EnchantAccessor().GetEnchantment(eCode);
                    if (enchant == null) continue;

                    if (EnchantingConfigLoader.Config?.Debug == true)
                        Api.Logger.Event("[KRPGEnchantment] {0} is being triggered.", eCode);
                    EnchantTick eTick = pair.Value;
                    enchant.OnTick(ref eTick);
                    TickRegistry[pair.Key] = eTick;
                }
                else
                {
                    if (EnchantingConfigLoader.Config?.Debug == true)
                        Api.Logger.Event("[KRPGEnchantment] Enchantment finished Ticking for {0}.", eCode);
                    pair.Value.Dispose();
                    TickRegistry.Remove(eCode);
                    continue;
                }
            }
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
            base.OnReceivedServerPacket(packetid, data, ref handled);
        }

        protected AdvancedParticleProperties[] ParticleProps;
        protected static AdvancedParticleProperties[] FireParticleProps;
        protected static AdvancedParticleProperties[] FrostParticleProps;
        protected static AdvancedParticleProperties[] ElectricityParticleProps;
        protected static AdvancedParticleProperties[] HealParticleProps;
        protected static AdvancedParticleProperties[] InjuryParticleProps;
        protected static AdvancedParticleProperties[] PoisonParticleProps;
        public virtual void ConfigParticles()
        {
            ParticleProps = new AdvancedParticleProperties[3];
            ParticleProps[0] = new AdvancedParticleProperties
            {
                HsvaColor = new NatFloat[4]
            {
                NatFloat.createUniform(30f, 20f),
                NatFloat.createUniform(255f, 50f),
                NatFloat.createUniform(255f, 50f),
                NatFloat.createUniform(255f, 0f)
            },
                GravityEffect = NatFloat.createUniform(0f, 0f),
                Velocity = new NatFloat[3]
            {
                NatFloat.createUniform(0.2f, 0.05f),
                NatFloat.createUniform(0.5f, 0.1f),
                NatFloat.createUniform(0.2f, 0.05f)
            },
                Size = NatFloat.createUniform(0.25f, 0f),
                Quantity = NatFloat.createUniform(0.25f, 0f),
                VertexFlags = 128,
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -0.5f),
                SelfPropelled = true
            };
            ParticleProps[1] = new AdvancedParticleProperties
            {
                HsvaColor = new NatFloat[4]
                {
                NatFloat.createUniform(30f, 20f),
                NatFloat.createUniform(255f, 50f),
                NatFloat.createUniform(255f, 50f),
                NatFloat.createUniform(255f, 0f)
                },
                OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f),
                GravityEffect = NatFloat.createUniform(0f, 0f),
                Velocity = new NatFloat[3]
                {
                NatFloat.createUniform(0f, 0.02f),
                NatFloat.createUniform(0f, 0.02f),
                NatFloat.createUniform(0f, 0.02f)
                },
                Size = NatFloat.createUniform(0.3f, 0.05f),
                Quantity = NatFloat.createUniform(0.25f, 0f),
                VertexFlags = 128,
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 1f),
                LifeLength = NatFloat.createUniform(0.5f, 0f),
                ParticleModel = EnumParticleModel.Quad
            };
            ParticleProps[2] = new AdvancedParticleProperties
            {
                HsvaColor = new NatFloat[4]
                {
                NatFloat.createUniform(0f, 0f),
                NatFloat.createUniform(0f, 0f),
                NatFloat.createUniform(40f, 30f),
                NatFloat.createUniform(220f, 50f)
                },
                OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f),
                GravityEffect = NatFloat.createUniform(0f, 0f),
                Velocity = new NatFloat[3]
                {
                NatFloat.createUniform(0f, 0.05f),
                NatFloat.createUniform(0.2f, 0.3f),
                NatFloat.createUniform(0f, 0.05f)
                },
                Size = NatFloat.createUniform(0.3f, 0.05f),
                Quantity = NatFloat.createUniform(0.25f, 0f),
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 1.5f),
                LifeLength = NatFloat.createUniform(1.5f, 0f),
                ParticleModel = EnumParticleModel.Quad,
                SelfPropelled = true
            };

            FireParticleProps = new AdvancedParticleProperties[3];
            FireParticleProps[0] = new AdvancedParticleProperties
            {
                HsvaColor = new NatFloat[4]
            {
                NatFloat.createUniform(30f, 20f),
                NatFloat.createUniform(255f, 50f),
                NatFloat.createUniform(255f, 50f),
                NatFloat.createUniform(255f, 0f)
            },
                GravityEffect = NatFloat.createUniform(0f, 0f),
                Velocity = new NatFloat[3]
            {
                NatFloat.createUniform(0.2f, 0.05f),
                NatFloat.createUniform(0.5f, 0.1f),
                NatFloat.createUniform(0.2f, 0.05f)
            },
                Size = NatFloat.createUniform(0.25f, 0f),
                Quantity = NatFloat.createUniform(0.25f, 0f),
                VertexFlags = 128,
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -0.5f),
                SelfPropelled = true
            };
            FireParticleProps[1] = new AdvancedParticleProperties
            {
                HsvaColor = new NatFloat[4]
                {
                NatFloat.createUniform(30f, 20f),
                NatFloat.createUniform(255f, 50f),
                NatFloat.createUniform(255f, 50f),
                NatFloat.createUniform(255f, 0f)
                },
                OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f),
                GravityEffect = NatFloat.createUniform(0f, 0f),
                Velocity = new NatFloat[3]
                {
                NatFloat.createUniform(0f, 0.02f),
                NatFloat.createUniform(0f, 0.02f),
                NatFloat.createUniform(0f, 0.02f)
                },
                Size = NatFloat.createUniform(0.3f, 0.05f),
                Quantity = NatFloat.createUniform(0.25f, 0f),
                VertexFlags = 128,
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 1f),
                LifeLength = NatFloat.createUniform(0.5f, 0f),
                ParticleModel = EnumParticleModel.Quad
            };
            FireParticleProps[2] = new AdvancedParticleProperties
            {
                HsvaColor = new NatFloat[4]
                {
                NatFloat.createUniform(0f, 0f),
                NatFloat.createUniform(0f, 0f),
                NatFloat.createUniform(40f, 30f),
                NatFloat.createUniform(220f, 50f)
                },
                OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f),
                GravityEffect = NatFloat.createUniform(0f, 0f),
                Velocity = new NatFloat[3]
                {
                NatFloat.createUniform(0f, 0.05f),
                NatFloat.createUniform(0.2f, 0.3f),
                NatFloat.createUniform(0f, 0.05f)
                },
                Size = NatFloat.createUniform(0.3f, 0.05f),
                Quantity = NatFloat.createUniform(0.25f, 0f),
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 1.5f),
                LifeLength = NatFloat.createUniform(1.5f, 0f),
                ParticleModel = EnumParticleModel.Quad,
                SelfPropelled = true
            };

            FrostParticleProps = new AdvancedParticleProperties[3];
            FrostParticleProps[0] = new AdvancedParticleProperties
            {
                HsvaColor = new NatFloat[4]
            {
                NatFloat.createUniform(128f, 0f),
                NatFloat.createUniform(128f, 20f),
                NatFloat.createUniform(255f, 50f),
                NatFloat.createUniform(255f, 0f)
            },
                GravityEffect = NatFloat.createUniform(0f, 0f),
                Velocity = new NatFloat[3]
            {
                NatFloat.createUniform(0.2f, 0.05f),
                NatFloat.createUniform(0.5f, 0.1f),
                NatFloat.createUniform(0.2f, 0.05f)
            },
                Size = NatFloat.createUniform(0.25f, 0f),
                Quantity = NatFloat.createUniform(0.25f, 0f),
                VertexFlags = 128,
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -0.5f),
                SelfPropelled = true
            };
            FrostParticleProps[1] = new AdvancedParticleProperties
            {
                HsvaColor = new NatFloat[4]
            {
                NatFloat.createUniform(128f, 0f),
                NatFloat.createUniform(128f, 20f),
                NatFloat.createUniform(255f, 50f),
                NatFloat.createUniform(255f, 0f)
            },
                OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f),
                GravityEffect = NatFloat.createUniform(0f, 0f),
                Velocity = new NatFloat[3]
                {
                NatFloat.createUniform(0f, 0.02f),
                NatFloat.createUniform(0f, 0.02f),
                NatFloat.createUniform(0f, 0.02f)
                },
                Size = NatFloat.createUniform(0.3f, 0.05f),
                Quantity = NatFloat.createUniform(0.25f, 0f),
                VertexFlags = 128,
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 1f),
                LifeLength = NatFloat.createUniform(0.5f, 0f),
                ParticleModel = EnumParticleModel.Quad
            };
            FrostParticleProps[2] = new AdvancedParticleProperties
            {
                HsvaColor = new NatFloat[4]
                {
                NatFloat.createUniform(128f, 0f),
                NatFloat.createUniform(128f, 20f),
                NatFloat.createUniform(255f, 50f),
                NatFloat.createUniform(255f, 0f)
                },
                OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f),
                GravityEffect = NatFloat.createUniform(0f, 0f),
                Velocity = new NatFloat[3]
                {
                NatFloat.createUniform(0f, 0.05f),
                NatFloat.createUniform(0.2f, 0.3f),
                NatFloat.createUniform(0f, 0.05f)
                },
                Size = NatFloat.createUniform(0.3f, 0.05f),
                Quantity = NatFloat.createUniform(0.25f, 0f),
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 1.5f),
                LifeLength = NatFloat.createUniform(1.5f, 0f),
                ParticleModel = EnumParticleModel.Quad,
                SelfPropelled = true
            };

            ElectricityParticleProps = new AdvancedParticleProperties[3];
            ElectricityParticleProps[0] = new AdvancedParticleProperties
            {
                HsvaColor = new NatFloat[4]
            {
                NatFloat.createUniform(45f, 0f),
                NatFloat.createUniform(0f, 20f),
                NatFloat.createUniform(255f, 50f),
                NatFloat.createUniform(255f, 0f)
            },
                GravityEffect = NatFloat.createUniform(0f, 0f),
                Velocity = new NatFloat[3]
            {
                NatFloat.createUniform(0.5f, 0.05f),
                NatFloat.createUniform(0.75f, 0.1f),
                NatFloat.createUniform(0.5f, 0.05f)
            },
                Size = NatFloat.createUniform(0.25f, 0f),
                Quantity = NatFloat.createUniform(0.25f, 0f),
                VertexFlags = 128,
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -0.5f),
                SelfPropelled = true
            };
            ElectricityParticleProps[1] = new AdvancedParticleProperties
            {
                HsvaColor = new NatFloat[4]
            {
                NatFloat.createUniform(45f, 0f),
                NatFloat.createUniform(0f, 20f),
                NatFloat.createUniform(255f, 50f),
                NatFloat.createUniform(255f, 0f)
            },
                OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f),
                GravityEffect = NatFloat.createUniform(0f, 0f),
                Velocity = new NatFloat[3]
                {
                NatFloat.createUniform(0f, 0.02f),
                NatFloat.createUniform(0f, 0.02f),
                NatFloat.createUniform(0f, 0.02f)
                },
                Size = NatFloat.createUniform(0.3f, 0.05f),
                Quantity = NatFloat.createUniform(0.25f, 0f),
                VertexFlags = 128,
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 1f),
                LifeLength = NatFloat.createUniform(0.5f, 0f),
                ParticleModel = EnumParticleModel.Quad
            };
            ElectricityParticleProps[2] = new AdvancedParticleProperties
            {
                HsvaColor = new NatFloat[4]
                {
                NatFloat.createUniform(45f, 0f),
                NatFloat.createUniform(0f, 20f),
                NatFloat.createUniform(255f, 50f),
                NatFloat.createUniform(255f, 0f)
                },
                OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f),
                GravityEffect = NatFloat.createUniform(0f, 0f),
                Velocity = new NatFloat[3]
                {
                NatFloat.createUniform(0.2f, 0.05f),
                NatFloat.createUniform(0.5f, 0.3f),
                NatFloat.createUniform(0.2f, 0.05f)
                },
                Size = NatFloat.createUniform(0.3f, 0.05f),
                Quantity = NatFloat.createUniform(0.25f, 0f),
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 1.5f),
                LifeLength = NatFloat.createUniform(1.5f, 0f),
                ParticleModel = EnumParticleModel.Quad,
                SelfPropelled = true
            };

            HealParticleProps = new AdvancedParticleProperties[3];
            HealParticleProps[0] = new AdvancedParticleProperties
            {
                HsvaColor = new NatFloat[4]
            {
                NatFloat.createUniform(36f, 0f),
                NatFloat.createUniform(255f, 10f),
                NatFloat.createUniform(200f, 20f),
                NatFloat.createUniform(255f, 0f)
            },
                GravityEffect = NatFloat.createUniform(0f, 0f),
                Velocity = new NatFloat[3]
            {
                NatFloat.createUniform(0.2f, 0.05f),
                NatFloat.createUniform(0.5f, 0.1f),
                NatFloat.createUniform(0.2f, 0.05f)
            },
                Size = NatFloat.createUniform(0.25f, 0f),
                Quantity = NatFloat.createUniform(0.25f, 0f),
                VertexFlags = 128,
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -0.5f),
                SelfPropelled = true
            };
            HealParticleProps[1] = new AdvancedParticleProperties
            {
                HsvaColor = new NatFloat[4]
            {
                NatFloat.createUniform(36f, 0f),
                NatFloat.createUniform(255f, 10f),
                NatFloat.createUniform(200f, 20f),
                NatFloat.createUniform(255f, 0f)
            },
                OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f),
                GravityEffect = NatFloat.createUniform(0f, 0f),
                Velocity = new NatFloat[3]
                {
                NatFloat.createUniform(0f, 0.02f),
                NatFloat.createUniform(0f, 0.02f),
                NatFloat.createUniform(0f, 0.02f)
                },
                Size = NatFloat.createUniform(0.3f, 0.05f),
                Quantity = NatFloat.createUniform(0.25f, 0f),
                VertexFlags = 128,
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 1f),
                LifeLength = NatFloat.createUniform(0.5f, 0f),
                ParticleModel = EnumParticleModel.Quad
            };
            HealParticleProps[2] = new AdvancedParticleProperties
            {
                HsvaColor = new NatFloat[4]
                {
                NatFloat.createUniform(36f, 0f),
                NatFloat.createUniform(255f, 00f),
                NatFloat.createUniform(255f, 20f),
                NatFloat.createUniform(255f, 0f)
                },
                OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f),
                GravityEffect = NatFloat.createUniform(0f, 0f),
                Velocity = new NatFloat[3]
                {
                NatFloat.createUniform(0f, 0.05f),
                NatFloat.createUniform(0.2f, 0.3f),
                NatFloat.createUniform(0f, 0.05f)
                },
                Size = NatFloat.createUniform(0.3f, 0.05f),
                Quantity = NatFloat.createUniform(0.25f, 0f),
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 1.5f),
                LifeLength = NatFloat.createUniform(1.5f, 0f),
                ParticleModel = EnumParticleModel.Quad,
                SelfPropelled = true
            };

            InjuryParticleProps = new AdvancedParticleProperties[3];
            InjuryParticleProps[0] = new AdvancedParticleProperties
            {
                HsvaColor = new NatFloat[4]
            {
                NatFloat.createUniform(0f, 0f),
                NatFloat.createUniform(0f, 0f),
                NatFloat.createUniform(0f, 50f),
                NatFloat.createUniform(255f, 0f)
            },
                GravityEffect = NatFloat.createUniform(0f, 0f),
                Velocity = new NatFloat[3]
            {
                NatFloat.createUniform(0.2f, 0.05f),
                NatFloat.createUniform(0.5f, 0.1f),
                NatFloat.createUniform(0.2f, 0.05f)
            },
                Size = NatFloat.createUniform(0.25f, 0f),
                Quantity = NatFloat.createUniform(0.25f, 0f),
                VertexFlags = 128,
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -0.5f),
                SelfPropelled = true
            };
            InjuryParticleProps[1] = new AdvancedParticleProperties
            {
                HsvaColor = new NatFloat[4]
            {
                NatFloat.createUniform(0f, 0f),
                NatFloat.createUniform(0f, 0f),
                NatFloat.createUniform(0f, 50f),
                NatFloat.createUniform(255f, 0f)
            },
                OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f),
                GravityEffect = NatFloat.createUniform(0f, 0f),
                Velocity = new NatFloat[3]
                {
                NatFloat.createUniform(0f, 0.02f),
                NatFloat.createUniform(0f, 0.02f),
                NatFloat.createUniform(0f, 0.02f)
                },
                Size = NatFloat.createUniform(0.3f, 0.05f),
                Quantity = NatFloat.createUniform(0.25f, 0f),
                VertexFlags = 128,
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 1f),
                LifeLength = NatFloat.createUniform(0.5f, 0f),
                ParticleModel = EnumParticleModel.Quad
            };
            InjuryParticleProps[2] = new AdvancedParticleProperties
            {
                HsvaColor = new NatFloat[4]
                {
                NatFloat.createUniform(0f, 0f),
                NatFloat.createUniform(0f, 0f),
                NatFloat.createUniform(0f, 50f),
                NatFloat.createUniform(255f, 0f)
                },
                OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f),
                GravityEffect = NatFloat.createUniform(0f, 0f),
                Velocity = new NatFloat[3]
                {
                NatFloat.createUniform(0f, 0.05f),
                NatFloat.createUniform(0.2f, 0.3f),
                NatFloat.createUniform(0f, 0.05f)
                },
                Size = NatFloat.createUniform(0.3f, 0.05f),
                Quantity = NatFloat.createUniform(0.25f, 0f),
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 1.5f),
                LifeLength = NatFloat.createUniform(1.5f, 0f),
                ParticleModel = EnumParticleModel.Quad,
                SelfPropelled = true
            };

            PoisonParticleProps = new AdvancedParticleProperties[3];
            PoisonParticleProps[0] = new AdvancedParticleProperties
            {
                HsvaColor = new NatFloat[4]
            {
                NatFloat.createUniform(188f, 0f),
                NatFloat.createUniform(255f, 0f),
                NatFloat.createUniform(200f, 50f),
                NatFloat.createUniform(255f, 0f)
            },
                GravityEffect = NatFloat.createUniform(0f, 0f),
                Velocity = new NatFloat[3]
            {
                NatFloat.createUniform(0.2f, 0.05f),
                NatFloat.createUniform(0.5f, 0.1f),
                NatFloat.createUniform(0.2f, 0.05f)
            },
                Size = NatFloat.createUniform(0.25f, 0f),
                Quantity = NatFloat.createUniform(0.25f, 0f),
                VertexFlags = 128,
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -0.5f),
                SelfPropelled = true
            };
            PoisonParticleProps[1] = new AdvancedParticleProperties
            {
                HsvaColor = new NatFloat[4]
            {
                NatFloat.createUniform(188f, 0f),
                NatFloat.createUniform(255f, 0f),
                NatFloat.createUniform(200f, 50f),
                NatFloat.createUniform(255f, 0f)
            },
                OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f),
                GravityEffect = NatFloat.createUniform(0f, 0f),
                Velocity = new NatFloat[3]
                {
                NatFloat.createUniform(0f, 0.02f),
                NatFloat.createUniform(0f, 0.02f),
                NatFloat.createUniform(0f, 0.02f)
                },
                Size = NatFloat.createUniform(0.3f, 0.05f),
                Quantity = NatFloat.createUniform(0.25f, 0f),
                VertexFlags = 128,
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 1f),
                LifeLength = NatFloat.createUniform(0.5f, 0f),
                ParticleModel = EnumParticleModel.Quad
            };
            PoisonParticleProps[2] = new AdvancedParticleProperties
            {
                HsvaColor = new NatFloat[4]
                {
                NatFloat.createUniform(188f, 0f),
                NatFloat.createUniform(255f, 0f),
                NatFloat.createUniform(200f, 50f),
                NatFloat.createUniform(255f, 0f)
                },
                OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f),
                GravityEffect = NatFloat.createUniform(0f, 0f),
                Velocity = new NatFloat[3]
                {
                NatFloat.createUniform(0f, 0.05f),
                NatFloat.createUniform(0.2f, 0.3f),
                NatFloat.createUniform(0f, 0.05f)
                },
                Size = NatFloat.createUniform(0.3f, 0.05f),
                Quantity = NatFloat.createUniform(0.25f, 0f),
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 1.5f),
                LifeLength = NatFloat.createUniform(1.5f, 0f),
                ParticleModel = EnumParticleModel.Quad,
                SelfPropelled = true
            };
        }
        public void GenerateParticles(EnumDamageType damageType, float damage)
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api?.Logger.Event("[KRPGEnchantment] Enchantment is generating particles for entity {0}.", entity.EntityId);

            int power = (int)MathF.Ceiling(damage);

            if (damageType == EnumDamageType.Fire)
            {
                int r = Api.World.Rand.Next(FireParticleProps.Length + 1);
                int num = Math.Min(FireParticleProps.Length - 1, r);
                AdvancedParticleProperties advancedParticleProperties = FireParticleProps[num];
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
                    Api.World.SpawnParticles(advancedParticleProperties);
                }
            }
            if (damageType == EnumDamageType.Frost)
            {
                int r = Api.World.Rand.Next(FrostParticleProps.Length + 1);
                int num = Math.Min(FrostParticleProps.Length - 1, r);
                AdvancedParticleProperties advancedParticleProperties = FrostParticleProps[num];
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
                    Api.World.SpawnParticles(advancedParticleProperties);
                }
            }
            if (damageType == EnumDamageType.Electricity)
            {
                int r = Api.World.Rand.Next(ElectricityParticleProps.Length + 1);
                int num = Math.Min(ElectricityParticleProps.Length - 1, r);
                AdvancedParticleProperties advancedParticleProperties = ElectricityParticleProps[num];
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
                    Api.World.SpawnParticles(advancedParticleProperties);
                }
            }
            if (damageType == EnumDamageType.Heal)
            {
                int r = Api.World.Rand.Next(HealParticleProps.Length + 1);
                int num = Math.Min(HealParticleProps.Length - 1, r);
                AdvancedParticleProperties advancedParticleProperties = HealParticleProps[num];
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
                    Api.World.SpawnParticles(advancedParticleProperties);
                }
            }
            if (damageType == EnumDamageType.Injury)
            {
                int r = Api.World.Rand.Next(InjuryParticleProps.Length + 1);
                int num = Math.Min(InjuryParticleProps.Length - 1, r);
                AdvancedParticleProperties advancedParticleProperties = InjuryParticleProps[num];
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
                    Api.World.SpawnParticles(advancedParticleProperties);
                }
            }
            if (damageType == EnumDamageType.Poison)
            {
                int r = Api.World.Rand.Next(PoisonParticleProps.Length + 1);
                int num = Math.Min(PoisonParticleProps.Length - 1, r);
                AdvancedParticleProperties advancedParticleProperties = PoisonParticleProps[num];
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
                    Api.World.SpawnParticles(advancedParticleProperties);
                }
            }
        }
        #endregion
    }
}