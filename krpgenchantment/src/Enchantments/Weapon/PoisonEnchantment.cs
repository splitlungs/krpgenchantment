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
using CompactExifLib;
using Vintagestory.API.Util;

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
            Version = 1.00f;
        }
        public override void OnAttack(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] {0} is being affected by an Poison enchantment.", enchant.TargetEntity.GetName());

            int tickMax = enchant.Power * TickMultiplier;
            EnchantmentEntityBehavior eeb = enchant.TargetEntity?.GetBehavior<EnchantmentEntityBehavior>();
            if (eeb != null)
            {
                // TODO: Make this better?
                if (eeb.TickRegistry.ContainsKey(Code))
                {
                    eeb.TickRegistry[Code].TicksRemaining = tickMax;
                    eeb.TickRegistry[Code].Source = enchant.Clone();
                }
                else if (tickMax > 1)
                {
                    EnchantTick eTick =
                        new EnchantTick() { TicksRemaining = tickMax, Source = enchant.Clone(), LastTickTime = 0 };
                    eeb.TickRegistry.Add(Code, eTick);
                    // DealPoison(enchant.TargetEntity, enchant.CauseEntity, enchant.Power);
                }
                else
                    DealPoison(enchant.TargetEntity, enchant.CauseEntity, enchant.Power);
            }
        }
        /// <summary>
        /// Deal Poison damage to entity HP and Saturation.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="byEntity"></param>
        /// <param name="power"></param>
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
            // I think OnEntityReceiveDamage is good enough and we don't need to bypass it?
            // hp.Health -= dmg;
            // hp.MarkDirty();
            // Particle if damaged
            ICoreServerAPI sApi = Api as ICoreServerAPI;
            if (EnchantingConfigLoader.Config?.Debug == true)
                sApi.Logger.Event("[KRPGEnchantment] Particle-ing the target after Enchantment Damage.");
            ParticlePacket packet = new ParticlePacket() { Amount = dmg, DamageType = EnumDamageType.Poison };
            byte[] data = SerializerUtil.Serialize(packet);
            sApi.Network.BroadcastEntityPacket(entity.EntityId, 1616, data);
            // entity.ReceiveDamage(source, dmg);
            // Hunger damage
            EntityBehaviorHunger hungy = entity.GetBehavior<EntityBehaviorHunger>();
            if (hungy == null) return;
            hungy.ConsumeSaturation(dmg * 100);

        }
        /// <summary>
        /// Attempt to resist, then deal Poison effect. Power multiplies number of 1s refreshes.
        /// </summary>
        public override void OnTick(ref EnchantTick eTick)
        {
            long curDur = Api.World.ElapsedMilliseconds - eTick.LastTickTime;
            int tr = eTick.TicksRemaining;

            if (tr > 0 && curDur >= TickDuration)
            {
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] Poison enchantment is performing a Poison Tick on {0}.", eTick.Source.TargetEntity.GetName());
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
                return;
            }
        }
    }
}
