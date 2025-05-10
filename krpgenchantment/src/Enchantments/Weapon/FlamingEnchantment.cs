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

namespace KRPGLib.Enchantment
{
    public class FlamingEnchantment : Enchantment
    {
        EnumDamageType DamageType { get { return EnumDamageType.Fire; } }
        // string DamageResist { get { return Attributes.GetString("DamageResist", "resistfire"); } }
        // int MaxDamage { get { return Attributes.GetInt("MaxDamage", 3); } }
        // float PowerMultiplier { get { return Attributes.GetFloat("PowerMultiplier", 0.1f); } }
        string DamageResist { get { return (string)Modifiers.GetValueOrDefault("DamageResist", "resistfire"); } }
        int MaxDamage { get { return (int)(long)Modifiers.GetValueOrDefault("MaxDamage", 3); } }
        float PowerMultiplier { get { return (float)(double)Modifiers.GetValueOrDefault("PowerMultiplier", 0.10f); } }

        public FlamingEnchantment(ICoreAPI api) : base(api)
        {
            // Setup the default config
            Enabled = true;
            Code = "flaming";
            Category = "Weapon";
            LoreCode = "enchantment-flaming";
            LoreChapterID = 2;
            MaxTier = 5;
            // Attributes = new TreeAttribute();
            // Attributes.SetString("DamageResist", "resistfire");
            // Attributes.SetInt("MaxDamage", 3);
            // Attributes.SetFloat("PowerMultiplier", 0.1f);
            Modifiers = new Dictionary<string, object>() 
            {
                { "DamageResist", "resistfire" }, { "MaxDamage", 3 }, {"PowerMultiplier", 0.10f }
            };
        }
        public override void OnAttack(EnchantmentSource enchant, ref Dictionary<string, object> parameters)
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] {0} is being affected by a damage enchantment.", enchant.TargetEntity.GetName());

            // Configure Damage
            // EntityBehaviorHealth hp = entity.GetBehavior<EntityBehaviorHealth>();

            DamageSource source = enchant.ToDamageSource();
            source.Type = DamageType;

            // if (byEntity != null)
            // {
            //     source.CauseEntity = byEntity;
            //     source.SourceEntity = byEntity;
            // }
            // if (stack != null)
            //     source.DamageTier = stack.Collectible.ToolTier;

            float dmg = 0;

            for (int i = 1; i <= enchant.Power; i++)
            {
                dmg += Api.World.Rand.Next(MaxDamage +1);
                dmg += Api.World.Rand.NextSingle();
                dmg += enchant.Power * PowerMultiplier;
            }
            
            // Apply Defenses
            if (enchant.TargetEntity is IPlayer player)
            {
                // Api.Logger.Event("Damage enchant is affecting a player!");
                IInventory inv = player.Entity.GetBehavior<EntityBehaviorPlayerInventory>()?.Inventory;
                if (inv != null)
                {
                    if (EnchantingConfigLoader.Config?.Debug == true)
                        Api.Logger.Event("[KRPGEnchantment] Player's inventory detected when receiving a damage enchant.");
                    float resist = 0f;
                    int[] wearableSlots = new int[3] { 12, 13, 14 };

                    foreach (int i in wearableSlots)
                    {
                        if (!inv[i].Empty)
                        {
                            Dictionary<string, int> enchants = Api.EnchantAccessor().GetEnchantments(inv[i].Itemstack);
                            int rPower = enchants.GetValueOrDefault(DamageResist, 0);
                            resist += rPower * 0.1f;
                        }
                    }
                    resist = 1 - resist;
                    dmg = Math.Max(0f, dmg * resist);
                }
                // IInventory inv = player.InventoryManager.GetOwnInventory("character");
                // IInventory inv = agent?.GearInventory;
            }

            ICoreServerAPI sApi = Api as ICoreServerAPI;
            // Apply Damage
            if (enchant.TargetEntity.ShouldReceiveDamage(source, dmg))
            {
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] Dealing {0} {1} damage.", dmg, source.Type.ToString());

                // Disabled because there is something stopping this from happening in rapid succession.
                // Some kind of timer is locking damage, and must be calculated manually here, instead.
                bool didDamage = enchant.TargetEntity.ReceiveDamage(source, dmg);
                if (didDamage != true)
                    Api.Logger.Error("[KRPGEnchantment] Tried to deal {0} damage to {1}, but failed!", dmg, enchant.TargetEntity.GetName());

                // hp.OnEntityReceiveDamage(source, ref dmg);
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] Particle-ing the target after Enchantment Damage.");
                GenerateParticles(enchant.TargetEntity, source.Type, dmg);
            }
            else if (!sApi.Server.Config.AllowPvP && source.Type == EnumDamageType.Heal)
            {
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] Trying to heal while PvP is disabled. Dealing damage anyway.");

                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] Dealing {0} {1} damage.", dmg, source.Type.ToString());

                // Disabled because there is something stopping this from happening in rapid succession.
                // Some kind of timer is locking damage, and must be calculated manually here, instead.
                enchant.TargetEntity.GetBehavior<EntityBehaviorHealth>().OnEntityReceiveDamage(source, ref dmg);
                // if (didDamage != true)
                //     Api.Logger.Error("[KRPGEnchantment] Tried to deal {0} damage to {1}, but failed!", dmg, entity.GetName());

                // hp.OnEntityReceiveDamage(source, ref dmg);
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] Particle-ing the target after Enchantment Damage.");
                GenerateParticles(enchant.TargetEntity, source.Type, dmg);
            }
            else
            {
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Warning("[KRPGEnchantment] Tried to deal {0} damage to {1}, but it should not receive damage!", dmg, enchant.TargetEntity.GetName());
            }

        }
    }
}
