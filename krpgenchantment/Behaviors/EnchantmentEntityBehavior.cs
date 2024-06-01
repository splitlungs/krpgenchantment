using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace KRPGLib.Enchantment
{
    public class EnchantmentEntityBehavior : EntityBehavior
    {
        public ICoreAPI Api;
        // public ICoreServerAPI sApi;
        public override string PropertyName() { return "EnchantmentEntityBehavior"; }
        public AdvancedParticleProperties[] ParticleProperties;
        public static AdvancedParticleProperties[] FireParticleProps;
        public EnchantmentEntityBehavior(Entity entity) : base(entity)
        {
            Api = entity.Api as ICoreAPI;
        }
        public override void OnEntityLoaded()
        {
            base.OnEntityLoaded();
            Api = entity.Api;
            // sApi = entity.Api as ICoreServerAPI;
        }
        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);

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
        }
        public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
        {
            // if (entity.Api.Side != EnumAppSide.Client)
            // return;
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

            }
            if (damageSource.Type == EnumDamageType.Electricity)
            {
            }
            if (damageSource.Type == EnumDamageType.Heal)
            {
            }
            if (damageSource.Type == EnumDamageType.Injury)
            {
            }
            if (damageSource.Type == EnumDamageType.Poison)
            {
            }

            base.OnEntityReceiveDamage(damageSource, ref damage);
        }
        public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
        {
            if (mode == EnumInteractMode.Attack && entity.Api.Side == EnumAppSide.Server)
            {
                int power = 0;
                // Alternate Damage
                power = itemslot.Itemstack.Attributes.GetInt("healing", 0);
                if (power > 0)
                {
                    DamageEntity(byEntity, EnumEnchantments.healing, power);
                    handled = EnumHandling.PreventSubsequent;
                }
                power = itemslot.Itemstack.Attributes.GetInt("flaming", 0);
                if (power > 0)
                {
                    DamageEntity(byEntity, EnumEnchantments.flaming, power);
                    handled = EnumHandling.Handled;
                }
                power = itemslot.Itemstack.Attributes.GetInt("frost", 0);
                if (power > 0)
                {
                    DamageEntity(byEntity, EnumEnchantments.frost, power);
                    handled = EnumHandling.Handled;
                }
                power = itemslot.Itemstack.Attributes.GetInt("harming", 0);
                if (power > 0)
                {
                    DamageEntity(byEntity, EnumEnchantments.harming, power);
                    handled = EnumHandling.Handled;
                }
                power = itemslot.Itemstack.Attributes.GetInt("shocking", 0);
                if (power > 0)
                {
                    DamageEntity(byEntity, EnumEnchantments.shocking, power);
                    handled = EnumHandling.Handled;
                }
                // Effects
                power = itemslot.Itemstack.Attributes.GetInt("chilling", 0);
                if (power > 0)
                {
                    ChillEntity(power);
                    handled = EnumHandling.Handled;
                }
                power = itemslot.Itemstack.Attributes.GetInt("igniting", 0);
                if (power > 0)
                {
                    IgniteEntity(power);
                    handled = EnumHandling.Handled;
                }
                power = itemslot.Itemstack.Attributes.GetInt("lightning", 0);
                if (power > 0)
                {
                    CallLightning(power);
                    handled = EnumHandling.Handled;
                }
                power = itemslot.Itemstack.Attributes.GetInt("pit", 0);
                if (power > 0)
                {
                    CreatePit(byEntity, power);
                    handled = EnumHandling.Handled;
                }
            }
            else
                handled = EnumHandling.PassThrough;

            base.OnInteract(byEntity, itemslot, hitPosition, mode, ref handled);
        }
        public override void OnReceivedClientPacket(IServerPlayer player, int packetid, byte[] data, ref EnumHandling handled)
        {
            base.OnReceivedClientPacket(player, packetid, data, ref handled);
        }
        public override void OnReceivedServerPacket(int packetid, byte[] data, ref EnumHandling handled)
        {
            if (packetid == 9666)
            {
                SpawnFireHitParticles(10, this.Api.World as IClientWorldAccessor);
            }
            handled = EnumHandling.Handled;

            base.OnReceivedServerPacket(packetid, data, ref handled);
        }
        #region Alt Damage
        /// <summary>
        /// Apply Enchantment damage to an Entity
        /// </summary>
        /// <param name="byEntity"></param>
        /// <param name="enchant"></param>
        /// <param name="power"></param>
        public void DamageEntity(Entity byEntity, EnumEnchantments enchant, int power)
        {
            switch (enchant)
            {
                case EnumEnchantments.healing:
                    {
                        if (power > 0)
                        {
                            DamageSource source = new DamageSource();
                            source.SourceEntity = byEntity;
                            source.Type = EnumDamageType.Heal;
                            source.DamageTier = power;
                            float dmg = Api.World.Rand.Next(1, 6) + power;
                            entity.ReceiveDamage(source, dmg);
                        }
                        return;
                    }
                case EnumEnchantments.flaming:
                    {
                        if (power > 0)
                        {
                            DamageSource source = new DamageSource();
                            source.SourceEntity = byEntity;
                            source.Type = EnumDamageType.Fire;
                            source.DamageTier = power;
                            float dmg = Api.World.Rand.Next(1, 6) + power;
                            entity.ReceiveDamage(source, dmg);
                        }
                        return;
                    }
                case EnumEnchantments.frost:
                    {
                        if (power > 0)
                        {
                            DamageSource source = new DamageSource();
                            source.SourceEntity = byEntity;
                            source.Type = EnumDamageType.Frost;
                            source.DamageTier = power;
                            float dmg = Api.World.Rand.Next(1, 6) + power;
                            entity.ReceiveDamage(source, dmg);
                        }
                        return;
                    }
                case EnumEnchantments.harming:
                    {
                        if (power > 0)
                        {
                            DamageSource source = new DamageSource();
                            source.SourceEntity = byEntity;
                            source.Type = EnumDamageType.Injury;
                            source.DamageTier = power;
                            float dmg = Api.World.Rand.Next(1, 6) + power;
                            entity.ReceiveDamage(source, dmg);
                        }
                        return;
                    }
                case EnumEnchantments.shocking:
                    {
                        if (power > 0)
                        {
                            DamageSource source = new DamageSource();
                            source.SourceEntity = byEntity;
                            source.Type = EnumDamageType.Electricity;
                            source.DamageTier = power;
                            float dmg = Api.World.Rand.Next(1, 6) + power;
                            entity.ReceiveDamage(source, dmg);
                        }
                        return;
                    }
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
        /// Attempt to set the target on fire. Power not doing anything currently.
        /// </summary>
        /// <param name="power"></param>
        public void IgniteEntity(int power)
        {
            if (igniteTicksRemaining > 0)
                return;

            igniteTicksRemaining = power;
            listenerID = Api.World.RegisterGameTickListener(IgniteTick, 12500);
            entity.IsOnFire = true;
        }
        long listenerID;
        int igniteTicksRemaining = 0;
        private void IgniteTick(float dt)
        {
            if (igniteTicksRemaining > 0)
            {
                entity.IsOnFire = true;
                igniteTicksRemaining--;
            }
            else
            {
                Api.World.UnregisterGameTickListener(listenerID);
            }

        }
        /// <summary>
        /// Create a lightning strike at Pos. Power not doing anything currently
        /// </summary>
        /// <param name="world"></param>
        /// <param name="pos"></param>
        public void CallLightning(int power)
        {
            WeatherSystemServer weatherSystem = Api.ModLoader.GetModSystem<WeatherSystemServer>();
            // It should default to 0f. Stun should stop at 0.5. Absorbtion should start at 1f.
            if (weatherSystem != null)
                weatherSystem.SpawnLightningFlash(entity.SidedPos.XYZ);
            else
                Api.Logger.Debug("Could not find Weather System!");
        }
        #endregion
        #region Particle Effects
        private void SpawnFireHitParticles(int power, IClientWorldAccessor world)
        {
            Api.Logger.Event("Spawning Fire Particles after Damage");

            int num = Math.Min(FireParticleProps.Length - 1, world.Rand.Next(FireParticleProps.Length + 1));
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
                world.SpawnParticles(advancedParticleProperties);
                Api.Logger.Event("Spawned Fire Particles.");
            }
        }
        #endregion
    }
}
