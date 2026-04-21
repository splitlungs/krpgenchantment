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
using Vintagestory.API.Util;

namespace KRPGLib.Enchantment
{
    public class HealingEnchantment : Enchantment
    {
        EnumDamageType DamageType { get { return EnumDamageType.Heal; } }
        float MinDamage { get { return Modifiers.GetFloat("MinDamage"); } }
        float MaxDamage { get { return Modifiers.GetFloat("MaxDamage"); } }
        int MaxDamageRolls { get { return Modifiers.GetInt("MaxDamageRolls"); } }
        float PowerMulltiplier { get { return Modifiers.GetFloat("PowerMulltiplier"); } }
        public HealingEnchantment(ICoreAPI api) : base(api)
        {
            // Setup the default config
            Enabled = true;
            Code = "healing";
            Category = "HealTarget";
            LoreCode = "enchantment-healing";
            LoreChapterID = 5;
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
                "Wand",
                "vanillaarmory:Club"
            };
            Modifiers = new EnchantModifiers()
            {
                {"MinDamage", 1.0f }, { "MaxDamage", 3.0f }, { "MaxDamageRolls", 5 }, { "PowerMulltiplier", 0.02f }
            };
            Version = 1.04f;
        }
        public override void OnAttacked(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            ICoreServerAPI sApi = Api as ICoreServerAPI;

            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] {0} is being affected by a {1} enchantment.", enchant.TargetEntity.GetName(), Code);

            // Check if it has HP first, since we have to address this directly.
            EntityBehaviorHealth hp = enchant.TargetEntity.GetBehavior<EntityBehaviorHealth>();
            if (hp == null) return;

            // Nullify base damage
            parameters["damage"] = 0;

            // Configure Damage
            DamageSource source = enchant.ToDamageSource();
            source.DamageTier = enchant.Power;
            source.Type = DamageType;
            float dmg = GetDamage(enchant.Power);

            // Apply Damage
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] Dealing {0} {1} damage.", dmg, source.Type.ToString());

            // Disabled because there is something stopping this from happening in rapid succession.
            // Some kind of timer is locking damage, and must be calculated manually here, instead.
            // bool didDamage = entity.ReceiveDamage(source, dmg);
            // if (didDamage != true)
            //     Api.Logger.Error("[KRPGEnchantment] Tried to deal {0} damage to {1}, but failed!", dmg, entity.GetName());

            hp.OnEntityReceiveDamage(source, ref dmg);

            // Set Handling to PreventDefault (2)
            parameters["handled"] = 2;

            // Particle if damaged
            if (EnchantingConfigLoader.Config?.Debug == true)
                sApi.Logger.Event("[KRPGEnchantment] Particle-ing the target after Enchantment Damage.");
            ParticlePacket packet = new ParticlePacket() { Amount = dmg, DamageType = DamageType };
            byte[] data = SerializerUtil.Serialize(packet);
            sApi.Network.BroadcastEntityPacket(enchant.TargetEntity.EntityId, 1616, data);
        }
        /// <summary>
        /// Returns the total damage that should be dealt, before armor/resist is applied.
        /// </summary>
        /// <param name="power"></param>
        /// <returns></returns>
        public float GetDamage(int power)
        {
            float dmg = 0;
            int rolls = Math.Min(power, MaxDamageRolls);
            for (int i = 1; i <= rolls; i++)
            {
                float diff = MaxDamage - MinDamage;
                double mul = Api.World.Rand.NextDouble();
                diff = diff * (float)mul;
                dmg += MaxDamage - diff;
            }
            float pmul = power * PowerMulltiplier;
            dmg *= pmul + 1;
            return dmg;
        }
    }
}
