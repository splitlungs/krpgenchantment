using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace KRPGLib.Enchantment
{
    public class EnchantedEntityProjectile : Entity
    {
        private bool beforeCollided;
        private bool stuck;
        private long msLaunch;
        private long msCollide;
        private Vec3d motionBeforeCollide = new Vec3d();
        private CollisionTester collTester = new CollisionTester();
        public Entity FiredBy;
        public float Weight = 0.1f;
        public float Damage;
        public ItemStack ProjectileStack;
        public float DropOnImpactChance;
        public bool DamageStackOnImpact;
        private Cuboidf collisionTestBox;
        private EntityPartitioning ep;
        public override bool ApplyGravity => !stuck;
        public override bool IsInteractable => false;

        public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
        {
            base.Initialize(properties, api, InChunkIndex3d);
            msLaunch = World.ElapsedMilliseconds;
            collisionTestBox = SelectionBox.Clone().OmniGrowBy(0.05f);
            GetBehavior<EntityBehaviorPassivePhysics>().OnPhysicsTickCallback = onPhysicsTickCallback;
            ep = api.ModLoader.GetModSystem<EntityPartitioning>();
            GetBehavior<EntityBehaviorPassivePhysics>().collisionYExtra = 0f;
        }

        private void onPhysicsTickCallback(float dtFac)
        {
            if (ShouldDespawn || !Alive || World.ElapsedMilliseconds <= msCollide + 500)
            {
                return;
            }

            EntityPos sidedPos = base.SidedPos;
            if (sidedPos.Motion.X == 0.0 && sidedPos.Motion.Y == 0.0 && sidedPos.Motion.Z == 0.0)
            {
                return;
            }

            Cuboidd projectileBox = SelectionBox.ToDouble().Translate(sidedPos.X, sidedPos.Y, sidedPos.Z);
            if (sidedPos.Motion.X < 0.0)
            {
                projectileBox.X1 += sidedPos.Motion.X * (double)dtFac;
            }
            else
            {
                projectileBox.X2 += sidedPos.Motion.X * (double)dtFac;
            }

            if (sidedPos.Motion.Y < 0.0)
            {
                projectileBox.Y1 += sidedPos.Motion.Y * (double)dtFac;
            }
            else
            {
                projectileBox.Y2 += sidedPos.Motion.Y * (double)dtFac;
            }

            if (sidedPos.Motion.Z < 0.0)
            {
                projectileBox.Z1 += sidedPos.Motion.Z * (double)dtFac;
            }
            else
            {
                projectileBox.Z2 += sidedPos.Motion.Z * (double)dtFac;
            }

            ep.WalkEntities(sidedPos.XYZ, 5.0, delegate (Entity e)
            {
                if (e.EntityId == EntityId || (FiredBy != null && e.EntityId == FiredBy.EntityId && World.ElapsedMilliseconds - msLaunch < 500) || !e.IsInteractable)
                {
                    return true;
                }

                if (e.SelectionBox.ToDouble().Translate(e.ServerPos.X, e.ServerPos.Y, e.ServerPos.Z).IntersectsOrTouches(projectileBox))
                {
                    impactOnEntity(e);
                    return false;
                }

                return true;
            }, EnumEntitySearchType.Creatures);
        }

        public override void OnGameTick(float dt)
        {
            base.OnGameTick(dt);
            if (ShouldDespawn)
            {
                return;
            }

            EntityPos sidedPos = base.SidedPos;
            stuck = base.Collided || collTester.IsColliding(World.BlockAccessor, collisionTestBox, sidedPos.XYZ) || WatchedAttributes.GetBool("stuck");
            if (Api.Side == EnumAppSide.Server)
            {
                WatchedAttributes.SetBool("stuck", stuck);
            }

            double impactSpeed = Math.Max(motionBeforeCollide.Length(), sidedPos.Motion.Length());
            if (stuck)
            {
                if (Api.Side == EnumAppSide.Client)
                {
                    ServerPos.SetFrom(Pos);
                }

                IsColliding(sidedPos, impactSpeed);
                return;
            }

            SetRotation();
            if (!TryAttackEntity(impactSpeed))
            {
                beforeCollided = false;
                motionBeforeCollide.Set(sidedPos.Motion.X, sidedPos.Motion.Y, sidedPos.Motion.Z);
            }
        }

        public override void OnCollided()
        {
            EntityPos sidedPos = base.SidedPos;
            IsColliding(base.SidedPos, Math.Max(motionBeforeCollide.Length(), sidedPos.Motion.Length()));
            motionBeforeCollide.Set(sidedPos.Motion.X, sidedPos.Motion.Y, sidedPos.Motion.Z);
        }

        private void IsColliding(EntityPos pos, double impactSpeed)
        {
            pos.Motion.Set(0.0, 0.0, 0.0);
            if (beforeCollided || !(World is IServerWorldAccessor) || World.ElapsedMilliseconds <= msCollide + 500)
            {
                return;
            }

            if (impactSpeed >= 0.07)
            {
                World.PlaySoundAt(new AssetLocation("sounds/arrow-impact"), this, null, randomizePitch: false);
                WatchedAttributes.MarkAllDirty();
                if (DamageStackOnImpact)
                {
                    ProjectileStack.Collectible.DamageItem(World, this, new DummySlot(ProjectileStack));
                    if (((ProjectileStack == null || ProjectileStack.Collectible.GetRemainingDurability(ProjectileStack) != 0) ? 1 : 0) <= (false ? 1 : 0))
                    {
                        Die();
                    }
                }
            }

            TryAttackEntity(impactSpeed);
            msCollide = World.ElapsedMilliseconds;
            beforeCollided = true;
        }

        private bool TryAttackEntity(double impactSpeed)
        {
            if (World is IClientWorldAccessor || World.ElapsedMilliseconds <= msCollide + 250)
            {
                return false;
            }

            if (impactSpeed <= 0.01)
            {
                return false;
            }

            _ = base.SidedPos;
            Cuboidd projectileBox = SelectionBox.ToDouble().Translate(ServerPos.X, ServerPos.Y, ServerPos.Z);
            if (ServerPos.Motion.X < 0.0)
            {
                projectileBox.X1 += 1.5 * ServerPos.Motion.X;
            }
            else
            {
                projectileBox.X2 += 1.5 * ServerPos.Motion.X;
            }

            if (ServerPos.Motion.Y < 0.0)
            {
                projectileBox.Y1 += 1.5 * ServerPos.Motion.Y;
            }
            else
            {
                projectileBox.Y2 += 1.5 * ServerPos.Motion.Y;
            }

            if (ServerPos.Motion.Z < 0.0)
            {
                projectileBox.Z1 += 1.5 * ServerPos.Motion.Z;
            }
            else
            {
                projectileBox.Z2 += 1.5 * ServerPos.Motion.Z;
            }

            Entity nearestEntity = World.GetNearestEntity(ServerPos.XYZ, 5f, 5f, delegate (Entity e)
            {
                if (e.EntityId == EntityId || !e.IsInteractable)
                {
                    return false;
                }

                return (FiredBy == null || e.EntityId != FiredBy.EntityId || World.ElapsedMilliseconds - msLaunch >= 500) && e.SelectionBox.ToDouble().Translate(e.ServerPos.X, e.ServerPos.Y, e.ServerPos.Z).IntersectsOrTouches(projectileBox);
            });
            if (nearestEntity != null)
            {
                impactOnEntity(nearestEntity);
                return true;
            }

            return false;
        }

        private void impactOnEntity(Entity entity)
        {
            if (!Alive)
            {
                return;
            }

            EntityPos sidedPos = base.SidedPos;
            IServerPlayer serverPlayer = null;
            if (FiredBy is EntityPlayer)
            {
                serverPlayer = (FiredBy as EntityPlayer).Player as IServerPlayer;
            }

            bool flag = entity is EntityPlayer;
            bool flag2 = entity is EntityAgent;
            bool flag3 = true;
            ICoreServerAPI coreServerAPI = World.Api as ICoreServerAPI;
            if (serverPlayer != null)
            {
                if (flag && (!coreServerAPI.Server.Config.AllowPvP || !serverPlayer.HasPrivilege("attackplayers")))
                {
                    flag3 = false;
                }

                if (flag2 && !serverPlayer.HasPrivilege("attackcreatures"))
                {
                    flag3 = false;
                }
            }

            msCollide = World.ElapsedMilliseconds;
            sidedPos.Motion.Set(0.0, 0.0, 0.0);
            if (flag3 && World.Side == EnumAppSide.Server)
            {
                World.PlaySoundAt(new AssetLocation("sounds/arrow-impact"), this, null, randomizePitch: false, 24f);
                float num = Damage;
                if (FiredBy != null)
                {
                    num *= FiredBy.Stats.GetBlended("rangedWeaponsDamage");
                }

                // Alternate Damage
                int flaming = this.WatchedAttributes.GetInt("flaming", 0);
                int frost = this.WatchedAttributes.GetInt("frost", 0);
                int harming = this.WatchedAttributes.GetInt("harming", 0);
                int healing = this.WatchedAttributes.GetInt("healing", 0);
                int shocking = this.WatchedAttributes.GetInt("shocking", 0);

                bool didDamage = false;
                // Healing
                if (healing > 0)
                {
                    DamageSource dSource = new DamageSource();
                    dSource.Source = EnumDamageSource.Entity;
                    dSource.SourceEntity = this.FiredBy == null ? this : this.FiredBy;
                    dSource.Type = EnumDamageType.Heal;
                    float dmg = this.World.Rand.Next(1, 6) + healing;
                    didDamage = entity.ReceiveDamage(dSource, dmg);
                }
                // Base
                else
                {
                    DamageSource dSource = new DamageSource();
                    dSource.Source = EnumDamageSource.Entity;
                    dSource.SourceEntity = this.FiredBy == null ? this : this.FiredBy;
                    float dmg = this.Damage;
                    if (this.FiredBy != null) dmg *= this.FiredBy.Stats.GetBlended("rangedWeaponsDamage");
                    didDamage = entity.ReceiveDamage(dSource, dmg);
                }

                // Flaming
                if (flaming > 0)
                {
                    DamageSource dSource = new DamageSource();
                    dSource.Source = EnumDamageSource.Entity;
                    dSource.SourceEntity = this.FiredBy == null ? this : this.FiredBy;
                    dSource.Type = EnumDamageType.Fire;
                    float dmg = this.World.Rand.Next(1, 6) + flaming;
                    didDamage = entity.ReceiveDamage(dSource, dmg);
                }
                // Frost
                if (frost > 0)
                {
                    DamageSource dSource = new DamageSource();
                    dSource.Source = EnumDamageSource.Entity;
                    dSource.SourceEntity = this.FiredBy == null ? this : this.FiredBy;
                    dSource.Type = EnumDamageType.Frost;
                    float dmg = this.World.Rand.Next(1, 6) + frost;
                    didDamage = entity.ReceiveDamage(dSource, dmg);
                }
                // Harming
                if (harming > 0)
                {
                    DamageSource dSource = new DamageSource();
                    dSource.Source = EnumDamageSource.Entity;
                    dSource.SourceEntity = this.FiredBy == null ? this : this.FiredBy;
                    dSource.Type = EnumDamageType.Injury;
                    float dmg = this.World.Rand.Next(1, 6) + harming;
                    didDamage = entity.ReceiveDamage(dSource, dmg);
                }
                // Shocking
                if (shocking > 0)
                {
                    DamageSource dSource = new DamageSource();
                    dSource.Source = EnumDamageSource.Entity;
                    dSource.SourceEntity = this.FiredBy == null ? this : this.FiredBy;
                    dSource.Type = EnumDamageType.Electricity;
                    float dmg = this.World.Rand.Next(1, 6) + shocking;
                    didDamage = entity.ReceiveDamage(dSource, dmg);
                }

                // Base Knockback
                float knockbackResistance = entity.Properties.KnockbackResistance;
                entity.SidedPos.Motion.Add((double)knockbackResistance * sidedPos.Motion.X * (double)Weight, (double)knockbackResistance * sidedPos.Motion.Y * (double)Weight, (double)knockbackResistance * sidedPos.Motion.Z * (double)Weight);
                int power = 0;
                // Enchantment Knockback
                power = this.WatchedAttributes.GetInt("knockback", 0);
                if (power > 0)
                {
                    double weightedPower = this.Weight + power * 100;
                    entity.SidedPos.Motion.Mul(-weightedPower, 1, -weightedPower);
                }
                // Enchantment Effects
                power = this.WatchedAttributes.GetInt("chilling", 0);
                if (power > 0) { ChillEntity(power, entity); }
                power = this.WatchedAttributes.GetInt("igniting", 0);
                if (power > 0) { IgniteEntity(entity, power); }
                power = this.WatchedAttributes.GetInt("lightning", 0);
                if (power > 0) { CallLightning(entity, power); }
                power = this.WatchedAttributes.GetInt("pit", 0);
                if (power > 0) { CreatePit(entity, power); }



                int num2 = 1;
                if (DamageStackOnImpact)
                {
                    ProjectileStack.Collectible.DamageItem(entity.World, entity, new DummySlot(ProjectileStack));
                    num2 = ((ProjectileStack == null) ? 1 : ProjectileStack.Collectible.GetRemainingDurability(ProjectileStack));
                }

                if (!(World.Rand.NextDouble() < (double)DropOnImpactChance) || num2 <= 0)
                {
                    Die();
                }

                if (FiredBy is EntityPlayer && didDamage)
                {
                    World.PlaySoundFor(new AssetLocation("sounds/player/projectilehit"), (FiredBy as EntityPlayer).Player, randomizePitch: false, 24f);
                }
            }
        }

        public virtual void SetRotation()
        {
            EntityPos entityPos = ((World is IServerWorldAccessor) ? ServerPos : Pos);
            double num = entityPos.Motion.Length();
            if (num > 0.01)
            {
                entityPos.Pitch = 0f;
                entityPos.Yaw = MathF.PI + (float)Math.Atan2(entityPos.Motion.X / num, entityPos.Motion.Z / num) + GameMath.Cos((float)(World.ElapsedMilliseconds - msLaunch) / 200f) * 0.03f;
                entityPos.Roll = 0f - (float)Math.Asin(GameMath.Clamp((0.0 - entityPos.Motion.Y) / num, -1.0, 1.0)) + GameMath.Sin((float)(World.ElapsedMilliseconds - msLaunch) / 200f) * 0.03f;
            }
        }

        public override bool CanCollect(Entity byEntity)
        {
            if (Alive && World.ElapsedMilliseconds - msLaunch > 1000)
            {
                return ServerPos.Motion.Length() < 0.01;
            }

            return false;
        }

        public override ItemStack OnCollected(Entity byEntity)
        {
            ProjectileStack.ResolveBlockOrItem(World);
            return ProjectileStack;
        }

        public override void OnCollideWithLiquid()
        {
            base.OnCollideWithLiquid();
        }

        public override void ToBytes(BinaryWriter writer, bool forClient)
        {
            base.ToBytes(writer, forClient);
            writer.Write(beforeCollided);
            ProjectileStack.ToBytes(writer);
        }

        public override void FromBytes(BinaryReader reader, bool fromServer)
        {
            base.FromBytes(reader, fromServer);
            beforeCollided = reader.ReadBoolean();
            ProjectileStack = new ItemStack(reader);
        }

        #region Alt Damage
        /// <summary>
        /// Apply Enchantment damage to an Entity
        /// </summary>
        /// <param name="byEntity"></param>
        /// <param name="enchant"></param>
        /// <param name="power"></param>
        public void DamageEntity(Entity byEntity, Entity toEntity, EnumEnchantments enchant, int power)
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
                            toEntity.ReceiveDamage(source, dmg);
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
                            toEntity.ReceiveDamage(source, dmg);
                            // ICoreServerAPI sapi = entity.World.Api as ICoreServerAPI;
                            // sapi.Network.SendEntityPacket(entity.Api as IServerPlayer, entity.EntityId, 9666);
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
                            toEntity.ReceiveDamage(source, dmg);
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
                            toEntity.ReceiveDamage(source, dmg);
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
                            toEntity.ReceiveDamage(source, dmg);
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
        public void ChillEntity(int power, Entity toEntity)
        {
            EntityBehaviorBodyTemperature ebbt = toEntity.GetBehavior<EntityBehaviorBodyTemperature>();

            // If we encounter something without one, bail
            if (ebbt == null)
                return;

            ebbt.CurBodyTemperature = power * -10f;
        }
        /// <summary>
        /// Creates a 1x1x1 pit under the target Entity multiplied by Power. Only works only Soil, Sand, or Gravel
        /// </summary>
        /// <param name="byEntity"></param>
        /// <param name="power"></param>
        public void CreatePit(Entity toEntity, int power)
        {
            BlockPos bpos = toEntity.SidedPos.AsBlockPos;
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
                Block block = toEntity.World.BlockAccessor.GetBlock(ipos);

                if (block != null)
                {
                    string blockCode = block.Code.ToString();
                    if (blockCode.Contains("soil") || blockCode.Contains("sand") || blockCode.Contains("gravel"))
                        toEntity.World.BlockAccessor.BreakBlock(ipos, toEntity as IPlayer);
                }
            }
        }
        /// <summary>
        /// Attempts to slay the target instantly. Chance of success is multiplied by Power
        /// </summary>
        /// <param name="byEntity"></param>
        /// <param name="power"></param>
        public void DeathPoison(DamageSource byEntity, Entity toEntity, int power)
        {
            Api.Event.TriggerEntityDeath(toEntity, byEntity);
        }
        /// <summary>
        /// Attempt to set the target on fire. Power not doing anything currently.
        /// </summary>
        /// <param name="power"></param>
        public void IgniteEntity(Entity toEntity, int power)
        {
            toEntity.IsOnFire = true;
            listenerID = Api.World.RegisterGameTickListener(IgniteTick, 12000);
        }
        long listenerID;
        int igniteTicksRemaining = 0;
        private void IgniteTick(float dt)
        {
            if (igniteTicksRemaining > 0)
            {
                // toEntity.IsOnFire = true;
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
        public void CallLightning(Entity toEntity, int power)
        {
            WeatherSystemServer weatherSystem = Api.ModLoader.GetModSystem<WeatherSystemServer>();
            // It should default to 0f. Stun should stop at 0.5. Absorbtion should start at 1f.
            if (weatherSystem != null)
                weatherSystem.SpawnLightningFlash(toEntity.SidedPos.XYZ);
            else
                Api.Logger.Debug("Could not find Weather System!");
        }
        #endregion
    }
}