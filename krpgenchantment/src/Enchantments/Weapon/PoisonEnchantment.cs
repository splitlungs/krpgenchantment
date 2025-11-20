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
using System.Data;

namespace KRPGLib.Enchantment
{
    public class PoisonEnchantment : Enchantment
    {
        string DamageResist { get { return Modifiers.GetString("DamageResist"); } }
        int TickMultiplier { get { return Modifiers.GetInt("TickMultiplier"); } }
        long TickDuration { get { return Modifiers.GetLong("TickDuration"); } }
        float DamageMultiplier { get { return Modifiers.GetFloat("DamageMultiplier"); } }
        float HungerMultiplier { get { return Modifiers.GetFloat("HungerMultiplier"); } }
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
                "Halberd", "Mace", "Pike", "Polearm", "Poleaxe", "Quarterstaff", "Sabre", "Staff", "Warhammer",
                "Javelin",
                "Crossbow", "Firearm",
                "Wand" };
            Modifiers = new EnchantModifiers()
            {
                { "DamageResist", "resistpoison" }, {"TickMultiplier", 6 }, {"TickDuration", 1000 }, {"DamageMultiplier", 0.1}, {"HungerMultiplier", 100}
            };
            Version = 1.02f;
        }
        public override void OnAttack(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] {0} is being affected by an Poison enchantment.", enchant.TargetEntity.GetName());

            int tickMax = enchant.Power * TickMultiplier;
            EnchantmentEntityBehavior eeb = enchant.TargetEntity?.GetBehavior<EnchantmentEntityBehavior>();
            if (eeb != null)
            {
                EnchantTick eTick = enchant.ToEnchantTick();
                eTick.TicksRemaining = tickMax;
                // TODO: Make this better?
                if (eeb.TickRegistry.ContainsKey(Code))
                {
                    eeb.TickRegistry[Code] = eTick;
                }
                else if (tickMax > 1)
                {
                    eeb.TickRegistry.Add(Code, eTick);
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

            double roll = Api.World.Rand.NextDouble();
            float dmg = ((power * DamageMultiplier) + (float)roll);

            // Health Damage
            EntityBehaviorHealth hp = entity.GetBehavior<EntityBehaviorHealth>();
            if (hp == null) return;

            DamageSource source = new DamageSource()
            {
                DamageTier = power +1,
                CauseEntity = byEntity,
                Source = EnumDamageSource.Entity,
                SourceEntity = byEntity,
                Type = EnumDamageType.Injury
            };
            // Bail if we shouldn't deal the damage.
            if (!entity.ShouldReceiveDamage(source, dmg)) return;

            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] Dealing {0} {1} damage.", dmg, EnumDamageType.Injury.ToString());

            hp.OnEntityReceiveDamage(source, ref dmg);

            // Particle if damaged
            ICoreServerAPI sApi = Api as ICoreServerAPI;
            if (EnchantingConfigLoader.Config?.Debug == true)
                sApi.Logger.Event("[KRPGEnchantment] Particle-ing the target after Enchantment Damage.");
            ParticlePacket packet = new ParticlePacket() { Amount = dmg, DamageType = EnumDamageType.Poison };
            byte[] data = SerializerUtil.Serialize(packet);
            sApi.Network.BroadcastEntityPacket(entity.EntityId, 1616, data);
            
            // Hunger damage
            EntityBehaviorHunger hungy = entity.GetBehavior<EntityBehaviorHunger>();
            if (hungy == null) return;
            // Setup Hunger
            float amount = dmg * HungerMultiplier;
            
            // TODO: Optimize how resist works
            // Resistances
            EnchantmentEntityBehavior eeb = entity.GetBehavior<EnchantmentEntityBehavior>();
            if (eeb?.IsPlayer == true)
            {
                // Push OnHit to each cached Gear enchants
                foreach (KeyValuePair<int, ActiveEnchantCache> pair in eeb.GearEnchantCache)
                {
                    if (pair.Value.Enchantments?.ContainsKey("resistpoison") != true) continue;

                    EnchantModifiers parameters = new EnchantModifiers() { { "damage", amount }, { "type", "poison" } };
                    EnchantmentSource enchant = new EnchantmentSource()
                    {
                        Trigger = "OnHit",
                        Code = "resistpoison",
                        Power = power,
                        SourceEntity = byEntity,
                        CauseEntity = byEntity,
                        TargetEntity = entity
                    };
                    bool didEnchant = sApi.EnchantAccessor().TryEnchantment(enchant, ref parameters);

                    // bool didEnchants =
                    //     eeb.sApi.EnchantAccessor().TryEnchantments(eeb.gearInventory[pair.Key]?.Itemstack, "OnHit", byEntity, entity, pair.Value.Enchantments, ref parameters);
                    if (didEnchant)
                    {
                        amount = parameters.GetFloat("damage");
                    }
                }
                // Apply hunger
                hungy.ConsumeSaturation(amount);
            }
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
                Entity entity = Api.World.GetEntityById(eTick.TargetEntityID);
                Entity byEntity = Api.World.GetEntityById(eTick.CauseEntityID);
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] Poison enchantment is performing a Poison Tick on {0}.", entity.GetName());

                if (entity == null)
                {
                    if (EnchantingConfigLoader.Config?.Debug == true)
                        Api.Logger.Event("[KRPGEnchantment] Poison enchantment Ticked a null entity. Removing from TickRegistry.");
                    eTick.Dispose();
                    return;
                }
                DealPoison(entity, byEntity, eTick.Power);
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
