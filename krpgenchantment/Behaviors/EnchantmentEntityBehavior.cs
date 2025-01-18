using KRPGLib.Enchantment;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Net;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using static System.Net.Mime.MediaTypeNames;

namespace KRPGLib.Enchantment
{
    public class EnchantmentEntityBehavior : EntityBehavior
    {
        public override string PropertyName() { return "EnchantmentEntityBehavior"; }
        public ICoreAPI Api;
        public ICoreClientAPI cApi;
        public ICoreServerAPI sApi;

        private EntityAgent agent;
        private IServerPlayer player = null;

        public bool IsPlayer { get { if (player != null) return true; return false; } }

        public EnchantmentEntityBehavior(Entity entity) : base(entity)
        {
            agent = entity as EntityAgent;
        }
        #region Events
        public void RegisterPlayer(IServerPlayer byPlayer)
        {
            player = byPlayer;
            // We'll probably use this later
            // Edit: We won't in 1.20
            // player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName).SlotModified += OnGearModified;
        }
        public void OnGearModified(int slotId)
        {
            if (!IsPlayer)
            {
                Api.Logger.Event("Player {0} modified slot {1}", player.PlayerUID, slotId);
                IInventory ownInventory = player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName);
                if (ownInventory != null)
                {
                    if (ownInventory[slotId].Empty)
                        Api.Logger.Event("Modified slot {0} was empty!", slotId);
                    else
                    {
                        int power = ownInventory[slotId].Itemstack.Attributes.GetInt(EnumEnchantments.protection.ToString(), 0);
                        Api.Logger.Event("Modified slot {0} as Protection {1}", slotId, power);
                    }
                }
            }
        }
        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);
            
            Api = entity.Api as ICoreAPI;
            sApi = Api.ModLoader.GetModSystem<KRPGEnchantmentSystem>().sApi;
            
            ConfigParticles();
        }
        /*
        public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
        {
            if (shouldParticle)
            {
                int power = (int)Math.Ceiling(damage);

                if (damageSource.Type == EnumDamageType.Fire)
                {
                    int num = Math.Min(FireParticleProps.Length - 1, Api.World.Rand.Next(FireParticleProps.Length + 1));
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
                if (damageSource.Type == EnumDamageType.Frost)
                {
                    int num = Math.Min(FrostParticleProps.Length - 1, Api.World.Rand.Next(FrostParticleProps.Length + 1));
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
                if (damageSource.Type == EnumDamageType.Electricity)
                {
                    int num = Math.Min(ElectricParticleProps.Length - 1, Api.World.Rand.Next(ElectricParticleProps.Length + 1));
                    AdvancedParticleProperties advancedParticleProperties = ElectricParticleProps[num];
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
                if (damageSource.Type == EnumDamageType.Heal)
                {
                    int num = Math.Min(HealParticleProps.Length - 1, Api.World.Rand.Next(HealParticleProps.Length + 1));
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
                if (damageSource.Type == EnumDamageType.Injury)
                {
                    int num = Math.Min(InjuryParticleProps.Length - 1, Api.World.Rand.Next(InjuryParticleProps.Length + 1));
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
                if (damageSource.Type == EnumDamageType.Poison)
                {
                    int num = Math.Min(PoisonParticleProps.Length - 1, Api.World.Rand.Next(PoisonParticleProps.Length + 1));
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

                // shouldParticle = false;
            }
            base.OnEntityReceiveDamage(damageSource, ref damage);
        }
        */
        public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
        {
            if (mode == EnumInteractMode.Attack && itemslot.Itemstack != null && entity.Api.Side == EnumAppSide.Server)
            {
                Api.Logger.Event("{0} was attacked by an enchanted weapon.", entity.GetName());
                // Get Enchantments
                Dictionary<string, int> enchants = new Dictionary<string, int>();
                foreach (var val in Enum.GetValues(typeof(EnumEnchantments)))
                {
                    int ePower = itemslot.Itemstack.Attributes.GetInt(val.ToString(), 0);
                    if (ePower > 0) { enchants.Add(val.ToString(), ePower); }
                }

                // Should avoid default during healing
                if (enchants.ContainsKey(EnumEnchantments.healing.ToString()))
                    handled = EnumHandling.PreventDefault;
                else
                    handled = EnumHandling.Handled;

                TryEnchantments(byEntity, itemslot.Itemstack, enchants);
            }
            else
            {
                base.OnInteract(byEntity, itemslot, hitPosition, mode, ref handled);
            }
            base.OnInteract(byEntity, itemslot, hitPosition, mode, ref handled);
        }
        /// <summary>
        /// Generic Enchantment processing.
        /// </summary>
        /// <param name="byEntity"></param>
        /// <param name="enchants"></param>
        public void TryEnchantments(EntityAgent byEntity, ItemStack stack, Dictionary<string, int> enchants)
        {
            foreach (KeyValuePair<string, int> pair in enchants)
                TryEnchantment(byEntity, stack, pair.Key, pair.Value);
        }
        /// <summary>
        /// Generic Enchantment processing.
        /// </summary>
        /// <param name="byEntity"></param>
        /// <param name="enchant"></param>
        /// <param name="power"></param>
        /// <returns></returns>
        public void TryEnchantment(EntityAgent byEntity, ItemStack stack, string enchant, int power)
        {
            // Alt Damage
            if (enchant == EnumEnchantments.healing.ToString())
                DamageEntity(byEntity, stack, EnumEnchantments.healing, power);
            else if (enchant == EnumEnchantments.flaming.ToString())
                DamageEntity(byEntity, stack, EnumEnchantments.flaming, power);
            else if (enchant == EnumEnchantments.frost.ToString())
                DamageEntity(byEntity, stack, EnumEnchantments.frost, power);
            else if (enchant == EnumEnchantments.harming.ToString())
                DamageEntity(byEntity, stack, EnumEnchantments.harming, power);
            else if (enchant == EnumEnchantments.shocking.ToString())
                DamageEntity(byEntity, stack, EnumEnchantments.shocking, power);
            // Alt Effects
            else if (enchant == EnumEnchantments.chilling.ToString())
                ChillEntity(power);
            else if (enchant == EnumEnchantments.igniting.ToString())
                IgniteEntity(power);
            else if (enchant == EnumEnchantments.knockback.ToString())
                KnockbackEntity(power);
            else if (enchant == EnumEnchantments.lightning.ToString())
                CallLightning(power);
            else if (enchant == EnumEnchantments.pit.ToString())
                CreatePit(byEntity, power);
        }
        #endregion
        #region Alt Damage
        /// <summary>
        /// Apply Enchantment damage to an Entity
        /// </summary>
        /// <param name="byEntity"></param>
        /// <param name="enchant"></param>
        /// <param name="power"></param>
        public void DamageEntity(Entity byEntity, ItemStack stack, EnumEnchantments enchant, int power)
        {
            // Configure Damage
            EntityBehaviorHealth hp = entity.GetBehavior<EntityBehaviorHealth>();
            DamageSource source = new DamageSource();
            source.SourceEntity = byEntity;
            source.DamageTier = stack.Collectible.ToolTier;
            
            float dmg = 0;

            for (int i = 1; i <= power; i++)
            {
                dmg += Api.World.Rand.Next(1, 4);
                dmg += Api.World.Rand.NextSingle();
                dmg += power * 0.1f;
            }

            if (enchant == EnumEnchantments.healing)
                source.Type = EnumDamageType.Heal;
            else if (enchant == EnumEnchantments.flaming)
                source.Type = EnumDamageType.Fire;
            else if (enchant == EnumEnchantments.frost)
                source.Type = EnumDamageType.Frost;
            else if (enchant == EnumEnchantments.harming)
                source.Type = EnumDamageType.Injury;
            else if (enchant == EnumEnchantments.shocking)
                source.Type = EnumDamageType.Electricity;

            // Apply Defenses
            if (IsPlayer)
            {
                IInventory inv = player.InventoryManager.GetOwnInventory("character");
                // IInventory inv = agent?.GearInventory;
                float resist = 0f;
                int[] wearableSlots = new int[3] { 12, 13, 14 };

                foreach (int i in wearableSlots)
                {
                    if (!inv[i].Empty)
                    {
                        int rPower = 0;
                        if (source.Type == EnumDamageType.Electricity)
                            rPower += inv[i].Itemstack.Attributes.GetInt(EnumEnchantments.resistelectric.ToString(), 0);
                        else if (source.Type == EnumDamageType.Fire)
                            rPower += inv[i].Itemstack.Attributes.GetInt(EnumEnchantments.resistfire.ToString(), 0);
                        else if (source.Type == EnumDamageType.Frost)
                            rPower += inv[i].Itemstack.Attributes.GetInt(EnumEnchantments.resistfrost.ToString(), 0);
                        else if (source.Type == EnumDamageType.Heal)
                            rPower += inv[i].Itemstack.Attributes.GetInt(EnumEnchantments.resistheal.ToString(), 0);
                        else if (source.Type == EnumDamageType.Injury)
                            rPower += inv[i].Itemstack.Attributes.GetInt(EnumEnchantments.resistinjury.ToString(), 0);
                        resist += rPower * 0.1f;
                    }
                }
                resist = 1 - resist;
                dmg = Math.Max(0f, dmg * resist);
            }

            // Apply Damage
            if (entity.ShouldReceiveDamage(source, dmg))
            {
                // Disabled because there is something stopping this from happening in rapid succession.
                // Some kind of timer is locking damage, and must be calculated manually here, instead.
                //
                // entity.ReceiveDamage(source, dmg);
                // Api.Logger.Event("Dealing {0} {1} damage.", dmg, source.Type.ToString());
                hp.OnEntityReceiveDamage(source, ref dmg);
                GenerateParticles(source, dmg);
            }
        }
        #endregion
        #region Effects
        /// <summary>
        /// Reduce the Entity's BodyTemperature to -10, multiplied by Power
        /// </summary>
        /// <param name="power"></param>
        public void ChillEntity(int power)
        {
            EntityBehaviorBodyTemperature ebbt = entity.GetBehavior<EntityBehaviorBodyTemperature>();
            if (ebbt != null)
                ebbt.CurBodyTemperature = power * -10f;
        }
        /// <summary>
        /// Push the Entity back, multiplied by Power
        /// </summary>
        /// <param name="power"></param>
        public void KnockbackEntity(int power)
        {
            double weightedPower = power * 100;
            entity.SidedPos.Motion.Mul(-weightedPower, 1, -weightedPower);
        }
        /// <summary>
        /// Creates a 1x1x1 pit under the target Entity multiplied by Power. Only works only Soil, Sand, or Gravel
        /// </summary>
        /// <param name="byEntity"></param>
        /// <param name="power"></param>
        public void CreatePit(Entity byEntity, int power)
        {
            BlockPos bpos = entity.SidedPos.AsBlockPos;
            List<Vec3d> pitArea = new List<Vec3d>();

            for (int x = 0; x <= power; x++)
            {
                for (int y = 0; y <= power; y++)
                {
                    for (int z = 0; z <= power; z++)
                    {
                        pitArea.Add(new Vec3d(bpos.X + x, bpos.Y - y, bpos.Z + z));
                        pitArea.Add(new Vec3d(bpos.X - x, bpos.Y - y, bpos.Z - z));
                        pitArea.Add(new Vec3d(bpos.X + x, bpos.Y - y, bpos.Z - z));
                        pitArea.Add(new Vec3d(bpos.X - x, bpos.Y - y, bpos.Z + z));
                    }
                }
            }

            for (int i = 0; i < pitArea.Count; i++)
            {
                BlockPos ipos = bpos;
                ipos.Set(pitArea[i]);
                Block block = byEntity.World.BlockAccessor.GetBlock(ipos);

                if (block != null)
                {
                    string blockCode = block.Code.ToString();
                    if (blockCode.Contains("soil") || blockCode.Contains("sand") || blockCode.Contains("gravel"))
                        byEntity.World.BlockAccessor.BreakBlock(ipos, byEntity as IPlayer);
                }
            }
        }
        /// <summary>
        /// Attempts to slay the target instantly. Chance of success is multiplied by Power
        /// </summary>
        /// <param name="byEntity"></param>
        /// <param name="power"></param>
        public void DeathPoison(DamageSource byEntity, int power)
        {
            Api.Event.TriggerEntityDeath(entity, byEntity);
        }
        /// <summary>
        /// Attempt to set the target on fire. Power multiplies number of 12s refreshes.
        /// </summary>
        /// <param name="power"></param>
        public void IgniteEntity(int power)
        {
            if (igniteTicksRemaining > 0)
            {
                igniteTicksRemaining = power;
            }
            else 
            {
                if (power > 1)
                {
                    igniteTicksRemaining = power;
                    igniteID = Api.World.RegisterGameTickListener(IgniteTick, 12500);
                }
                entity.Ignite();
            }
        }
        long igniteID;
        int igniteTicksRemaining = 0;
        private void IgniteTick(float dt)
        {
            if (igniteTicksRemaining > 0)
            {
                entity.Ignite();
                igniteTicksRemaining--;
            }
            else
            {
                Api.World.UnregisterGameTickListener(igniteID);
            }

        }
        /// <summary>
        /// Create a lightning strike at Pos. Power multiplies the number of lightning strikes.
        /// </summary>
        /// <param name="world"></param>
        /// <param name="pos"></param>
        public void CallLightning(int power)
        {
            WeatherSystemServer weatherSystem = Api.ModLoader.GetModSystem<WeatherSystemServer>();
            if (weatherSystem != null)
            {
                for (int i = 0; i < power; i++)
                {
                    weatherSystem.SpawnLightningFlash(entity.SidedPos.XYZ);
                }
            }
            else
                Api.Logger.Debug("Could not find Weather System!");
        }
        /// <summary>
        /// Attempt to poison the entity. Power multiplies number of 12s refreshes.
        /// </summary>
        /// <param name="power"></param>
        public void PoisonEntity(int power)
        {
            if (poisonTicksRemaining > 0)
                return;

            poisonTicksRemaining = power;
            poisonID = Api.World.RegisterGameTickListener(PoisonTick, 6333);
        }
        public bool IsPoisoned
        {
            get
            {
                return entity.WatchedAttributes.GetBool("isPoisoned");
            }
            set
            {
                entity.WatchedAttributes.SetBool("isPoisoned", value);
            }
        }
        protected void updatePoisoned()
        {
            bool isPoisoned = IsPoisoned;
            if (isPoisoned)
            {
                OnPoisonBeginTotalMs = Api.World.ElapsedMilliseconds;
            }

            if (isPoisoned && entity.LightHsv == null)
            {
                entity.LightHsv = new byte[3] { 5, 7, 10 };
                resetLightHsv = true;
            }

            if (!isPoisoned && resetLightHsv)
            {
                entity.LightHsv = null;
            }
        }
        long poisonID;
        long OnPoisonBeginTotalMs;
        int poisonTicksRemaining = 0;
        private void PoisonTick(float dt)
        {
            if (poisonTicksRemaining > 0)
            {
                entity.IsOnFire = true;
                poisonTicksRemaining--;
            }
            else
            {
                Api.World.UnregisterGameTickListener(poisonID);
            }

        }
        #endregion
        #region Particle Effects
        protected static AdvancedParticleProperties[] HealParticleProps;
        protected static AdvancedParticleProperties[] FireParticleProps;
        protected static AdvancedParticleProperties[] FrostParticleProps;
        protected static AdvancedParticleProperties[] InjuryParticleProps;
        protected static AdvancedParticleProperties[] ElectricParticleProps;
        protected static AdvancedParticleProperties[] PoisonParticleProps;
        protected bool resetLightHsv;

        private void ConfigParticles()
        {
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

            ElectricParticleProps = new AdvancedParticleProperties[3];
            ElectricParticleProps[0] = new AdvancedParticleProperties
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
            ElectricParticleProps[1] = new AdvancedParticleProperties
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
            ElectricParticleProps[2] = new AdvancedParticleProperties
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
        private void GenerateParticles(DamageSource damageSource, float damage)
        {
            int power = (int)Math.Ceiling(damage);

            if (damageSource.Type == EnumDamageType.Fire)
            {
                int num = Math.Min(FireParticleProps.Length - 1, Api.World.Rand.Next(FireParticleProps.Length + 1));
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
            if (damageSource.Type == EnumDamageType.Frost)
            {
                int num = Math.Min(FrostParticleProps.Length - 1, Api.World.Rand.Next(FrostParticleProps.Length + 1));
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
            if (damageSource.Type == EnumDamageType.Electricity)
            {
                int num = Math.Min(ElectricParticleProps.Length - 1, Api.World.Rand.Next(ElectricParticleProps.Length + 1));
                AdvancedParticleProperties advancedParticleProperties = ElectricParticleProps[num];
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
            if (damageSource.Type == EnumDamageType.Heal)
            {
                int num = Math.Min(HealParticleProps.Length - 1, Api.World.Rand.Next(HealParticleProps.Length + 1));
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
            if (damageSource.Type == EnumDamageType.Injury)
            {
                int num = Math.Min(InjuryParticleProps.Length - 1, Api.World.Rand.Next(InjuryParticleProps.Length + 1));
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
            if (damageSource.Type == EnumDamageType.Poison)
            {
                int num = Math.Min(PoisonParticleProps.Length - 1, Api.World.Rand.Next(PoisonParticleProps.Length + 1));
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