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
        int MaxDamage { get { return Modifiers.GetInt("MaxDamage"); } }
        float PowerMultiplier { get { return Modifiers.GetFloat("PowerMultiplier"); } }
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
                "Halberd", "Mace", "Pike", "Polearm", "Poleaxe", "Staff", "Warhammer",
                "Javelin",
                "Crossbow", "Firearm",
                "Wand" };
            Modifiers = new EnchantModifiers()
            {
                { "MaxDamage", 3 }, {"PowerMultiplier", 0.10f }
            };
            Version = 1.01f;
        }
        public override void OnAttack(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            ICoreServerAPI sApi = Api as ICoreServerAPI;

            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] {0} is being affected by a {1} enchantment.", enchant.TargetEntity.GetName(), Code);

            // Check if it has HP first, since we have to address this directly.
            EntityBehaviorHealth hp = enchant.TargetEntity.GetBehavior<EntityBehaviorHealth>();
            if (hp == null) return;

            // Configure Damage
            DamageSource source = enchant.ToDamageSource();
            source.Type = DamageType;
            float dmg = 0;
            for (int i = 1; i <= enchant.Power; i++)
            {
                dmg += Api.World.Rand.Next(MaxDamage + 1);
                dmg += Api.World.Rand.NextSingle();
                dmg += enchant.Power * PowerMultiplier;
            }

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

    }
}
