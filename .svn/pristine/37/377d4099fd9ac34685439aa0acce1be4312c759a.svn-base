using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace KRPGLib.Enchantment
{
    public class EnchantmentEntityBehavior : EntityBehavior
    {
        public ICoreAPI Api;
        public override string PropertyName() { return "EnchantmentEntityBehavior"; }
        public AdvancedParticleProperties[] ParticleProperties;
        public EnchantmentEntityBehavior(Entity entity) : base(entity)
        {
            Api = entity.Api as ICoreAPI;
        }
        public override void OnEntityLoaded()
        {
            base.OnEntityLoaded();
        }
        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);

            Api = entity.World.Api;
            Api.Logger.Event("Initialized an EnchantmentEntityBehavior");
        }
        public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
        {
            base.OnEntityReceiveDamage(damageSource, ref damage);

            if (damageSource.Type == EnumDamageType.Fire && Api.Side == EnumAppSide.Client)
                SpawnFireHitParticles();
        }
        public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
        {
            Api.Logger.Event("Attempting to intercept OnInteract");

            if (mode == EnumInteractMode.Attack)
            {
                int power = 0;
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
                    handled = EnumHandling.PassThrough;
                }
                power = itemslot.Itemstack.Attributes.GetInt("frost", 0);
                if (power > 0)
                {
                    DamageEntity(byEntity, EnumEnchantments.frost, power);
                    handled = EnumHandling.PassThrough;
                }
                power = itemslot.Itemstack.Attributes.GetInt("harming", 0);
                if (power > 0)
                {
                    DamageEntity(byEntity, EnumEnchantments.harming, power);
                    handled = EnumHandling.PassThrough;
                }
                power = itemslot.Itemstack.Attributes.GetInt("shocking", 0);
                if (power > 0)
                {
                    DamageEntity(byEntity, EnumEnchantments.shocking, power);
                    handled = EnumHandling.PassThrough;
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
        public override void OnReceivedServerPos(bool isTeleport, ref EnumHandling handled)
        {
            base.OnReceivedServerPos(isTeleport, ref handled);
        }
        #region Effects
        /// <summary>
        /// Apply Enchantment damage to an Entity
        /// </summary>
        /// <param name="byEntity"></param>
        /// <param name="toEntity"></param>
        /// <param name="enchant"></param>
        /// <param name="power"></param>
        public void DamageEntity(Entity byEntity, EnumEnchantments enchant, int power)
        {
            Api.Logger.Event("{0} attempted to use Enchantment DamageEntity-{1} on {2}", byEntity.GetName(), enchant.ToString(), entity.GetName());
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
        protected virtual void SpawnFireHitParticles()
        {
            Api.Logger.Event("Spawning Fire Particles after Damage");
            ICoreClientAPI coreClientAPI = Api as ICoreClientAPI;
            EntityPos entityPos = ((coreClientAPI.World.Player.Entity.EntityId == entity.EntityId) ? entity.Pos : entity.ServerPos);
            float num = (float)Math.Sqrt(entity.Pos.Motion.X * entity.Pos.Motion.X + entity.Pos.Motion.Z * entity.Pos.Motion.Z);
            if (Api.World.Rand.NextDouble() < (double)(10f * num))
            {
                Random rand = coreClientAPI.World.Rand;
                Vec3f velocity = new Vec3f(1f - 2f * (float)rand.NextDouble() + GameMath.Clamp((float)entity.Pos.Motion.X * 15f, -5f, 5f), 0.5f + 3.5f * (float)rand.NextDouble(), 1f - 2f * (float)rand.NextDouble() + GameMath.Clamp((float)entity.Pos.Motion.Z * 15f, -5f, 5f));
                float radius = Math.Min(entity.SelectionBox.XSize, entity.SelectionBox.ZSize) * 0.9f;
                entity.World.SpawnParticles(20f, 1, entityPos.XYZ, entityPos.XYZ + radius, velocity, velocity, 2 + (int)(rand.NextDouble() * (double)num * 5.0), 0.5f + (float)rand.NextDouble() * 0.5f);
            }
        }
        #endregion
    }
}
