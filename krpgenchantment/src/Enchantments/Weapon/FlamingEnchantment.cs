using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace KRPGLib.Enchantment
{
    public class FlamingEnchantment : Enchantment
    {
        protected override void OnAttack(EnchantmentSource enchant, ItemSlot slot, ref float damage)
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] {0} is being affected by a damage enchantment.", enchant.TargetEntity.GetName());
            // Configure Damage
            // EntityBehaviorHealth hp = entity.GetBehavior<EntityBehaviorHealth>();

            DamageSource source = enchant.ToDamageSource();
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
                dmg += Api.World.Rand.Next(1, 4);
                dmg += Api.World.Rand.NextSingle();
                dmg += enchant.Power * 0.1f;
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
                            int rPower = 0;
                            if (source.Type == EnumDamageType.Fire)
                                rPower += enchants.GetValueOrDefault(EnumEnchantments.resistfire.ToString(), 0);
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
                GenerateParticles(enchant.TargetEntity, enchant.Type, dmg);
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
                GenerateParticles(enchant.TargetEntity, enchant.Type, dmg);
            }
            else
            {
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Warning("[KRPGEnchantment] Tried to deal {0} damage to {1}, but it should not receive damage!", dmg, enchant.TargetEntity.GetName());
            }

        }
    }
}
