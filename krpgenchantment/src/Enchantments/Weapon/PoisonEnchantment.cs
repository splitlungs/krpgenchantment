using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using KRPGLib.Enchantment.API;
using Newtonsoft.Json.Linq;

namespace KRPGLib.Enchantment
{
    public class PoisonEnchantment : Enchantment
    {
        int TickMultiplier { get { return Modifiers.GetInt("TickMultiplier"); } }
        long TickDuration { get { return Modifiers.GetLong("TickDuration"); } }
        float DamageMultiplier { get { return Modifiers.GetFloat("DamageMultiplier"); } }
        public PoisonEnchantment(ICoreAPI api) : base(api)
        {
            // Setup the default config
            Enabled = true;
            Code = "poison";
            Category = "DamageTick";
            LoreCode = "enchantment-poison";
            LoreChapterID = 18;
            MaxTier = 5;
            ValidToolTypes = new List<string>() {
                "Knife", "Axe",
                "Club", "Sword",
                "Spear",
                "Bow", "Sling",
                "Drill",
                "Halberd", "Mace", "Pike", "Polearm", "Poleaxe", "Staff", "Warhammer",
                "Javelin",
                "Crossbow", "Firearm",
                "Wand" };
            Modifiers = new EnchantModifiers()
            {
                {"TickMultiplier", 6 }, {"TickDuration", 1000 }, {"DamageMultiplier", 0.1}
            };
        }
        public override void Initialize(EnchantmentProperties properties)
        {
            base.Initialize(properties);
            // We let the config initialize before registering the Tick Listener
            // Api.World.RegisterGameTickListener(PoisonTick, TickFrequency);
        }
        public override void ConfigParticles()
        {
            ParticleProps = new AdvancedParticleProperties[3];
            ParticleProps[0] = new AdvancedParticleProperties
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
            ParticleProps[1] = new AdvancedParticleProperties
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
            ParticleProps[2] = new AdvancedParticleProperties
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
        }
        public override void OnAttack(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] {0} is being affected by an Poison enchantment.", enchant.TargetEntity.GetName());

            int tickMax = (enchant.Power * TickMultiplier) -1;
            EnchantmentEntityBehavior eeb = enchant.TargetEntity.GetBehavior<EnchantmentEntityBehavior>();
            if (eeb != null)
            {
                if (eeb.TickRegistry.ContainsKey(Code))
                {
                    eeb.TickRegistry[Code].TicksRemaining = tickMax;
                    eeb.TickRegistry[Code].Source = enchant.Clone();
                }
                else if (tickMax > 1)
                {
                    EnchantTick eTick =
                        new EnchantTick() { TicksRemaining = tickMax, Source = enchant.Clone(), LastTickTime = Api.World.ElapsedMilliseconds };
                    eeb.TickRegistry.Add(Code, eTick);
                    DealPoison(enchant.TargetEntity, enchant.CauseEntity, enchant.Power);
                }
                else
                    DealPoison(enchant.TargetEntity, enchant.CauseEntity, enchant.Power);
            }
        }
        public void DealPoison(Entity entity, Entity byEntity, int power)
        {
            if (entity == null) return;

            // Get Resistance
            float resist = 0f;
            if (entity is IPlayer player)
            {
                IInventory inv = player.Entity.GetBehavior<EntityBehaviorPlayerInventory>()?.Inventory;
                if (inv != null)
                {
                    if (EnchantingConfigLoader.Config?.Debug == true)
                        Api.Logger.Event("[KRPGEnchantment] Player's inventory detected when receiving a poison enchant.");
                    
                    int[] wearableSlots = new int[3] { 12, 13, 14 };
                    foreach (int i in wearableSlots)
                    {
                        if (!inv[i].Empty)
                        {
                            Dictionary<string, int> enchants = Api.EnchantAccessor().GetActiveEnchantments(inv[i].Itemstack);
                            int rPower = enchants.GetValueOrDefault("resistpoison", 0);
                            resist += rPower * 0.1f;
                        }
                    }
                }
            }
            resist = 1 - resist;
            // Health Damage
            EntityBehaviorHealth hp = entity.GetBehavior<EntityBehaviorHealth>();
            if (hp == null) return;
            double roll = Api.World.Rand.NextDouble();
            float dmg = ((float)roll * resist) + (power * DamageMultiplier);
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] Dealing {0} {1} damage.", dmg, EnumDamageType.Injury.ToString());
            DamageSource source = new DamageSource()
            {
                DamageTier = power +1,
                CauseEntity = byEntity,
                Source = EnumDamageSource.Entity,
                SourceEntity = byEntity,
                Type = EnumDamageType.Injury
            };
            hp.OnEntityReceiveDamage(source, ref dmg);
            // Particle
            GenerateParticles(entity, EnumDamageType.Injury, dmg + 1);
            // entity.ReceiveDamage(source, dmg);
            // Hunger damage
            EntityBehaviorHunger hungy = entity.GetBehavior<EntityBehaviorHunger>();
            if (hungy == null) return;
            hungy.ConsumeSaturation(dmg * 100);

        }
        /// <summary>
        /// Attempt to resist, then deal Poison effect. Power multiplies number of 1s refreshes.
        /// </summary>
        public override void OnTick(float deltaTime, ref EnchantTick eTick)
        {
            long curDur = Api.World.ElapsedMilliseconds - eTick.LastTickTime;
            int tr = eTick.TicksRemaining;

            if (tr > 0 && curDur >= TickDuration)
            {
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] Poison enchantment is performing an Poison Tick on {0}.", eTick.Source.TargetEntity.GetName());
                Entity entity = eTick.Source.TargetEntity;
                if (entity == null)
                {
                    if (EnchantingConfigLoader.Config?.Debug == true)
                        Api.Logger.Event("[KRPGEnchantment] Poison enchantment Ticked a null entity. Removing from TickRegistry.");
                    eTick.Dispose();
                    return;
                }
                DealPoison(entity, eTick.Source.CauseEntity, eTick.Source.Power);
                eTick.TicksRemaining = tr - 1;
                eTick.LastTickTime = Api.World.ElapsedMilliseconds;
            }
            else if (tr <= 0)
            {
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] Poison enchantment finished Ticking for {0}.", Code);
                eTick.Dispose();
            }
        }

        // public void PoisonTick(float dt)
        // {
        //     foreach (KeyValuePair<long, EnchantTick> pair in TickRegistry) 
        //     {
        //         Entity entity = Api.World.GetEntityById(pair.Key);
        //         long curDur = Api.World.ElapsedMilliseconds - pair.Value.LastTickTime;
        //         if (pair.Value.TicksRemaining > 0 && curDur >= TickDuration)
        //         {
        //             if (EnchantingConfigLoader.Config?.Debug == true)
        //                 Api.Logger.Event("[KRPGEnchantment] Poison enchantment is performing an Poison Tick on {0}.", pair.Key);
        //             
        //             if (entity == null)
        //             {
        //                 if (EnchantingConfigLoader.Config?.Debug == true)
        //                     Api.Logger.Event("[KRPGEnchantment] Poison enchantment Ticked a null entity. Removing from TickRegistry.");
        //                 TickRegistry[pair.Key].Dispose();
        //                 TickRegistry.Remove(pair.Key);
        //                 continue;
        //             }
        // 
        //             DealPoison(entity, pair.Value.Source.CauseEntity, pair.Value.Source.Power);
        //             TickRegistry[pair.Key].TicksRemaining = pair.Value.TicksRemaining--;
        //             pair.Value.LastTickTime = Api.World.ElapsedMilliseconds;
        //         }
        //         else if (pair.Value.TicksRemaining <= 0)
        //         {
        //             if (EnchantingConfigLoader.Config?.Debug == true)
        //                 Api.Logger.Event("[KRPGEnchantment] Poison enchantment finished Ticking for {0}.", pair.Key);
        //             TickRegistry[pair.Key].Dispose();
        //             TickRegistry.Remove(pair.Key);
        //         }
        //     }
        // }

    }
}
