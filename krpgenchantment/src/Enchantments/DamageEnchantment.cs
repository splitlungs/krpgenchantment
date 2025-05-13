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
    public class DamageEnchantment : Enchantment
    {
        EnumDamageType DamageType { get { return EnumDamageType.BluntAttack; } }
        string DamageResist { get { return Modifiers.GetString("DamageResist"); } }
        int MaxDamage { get { return Modifiers.GetInt("MaxDamage"); } }
        float PowerMultiplier { get { return Modifiers.GetInt("PowerMultiplier"); } }

        public DamageEnchantment(ICoreAPI api) : base(api)
        {
            // Setup the default config
            Enabled = true;
            Code = "damage";
            Category = "Damage";
            LoreCode = "enchantment-damage";
            LoreChapterID = -1;
            MaxTier = 5;
            Modifiers = new EnchantModifiers()
            {
                { "DamageResist", "resistfire" }, { "MaxDamage", 3 }, {"PowerMultiplier", 0.1f }
            };
        }
        public override void OnAttack(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            ICoreServerAPI sApi = Api as ICoreServerAPI;

            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] {0} is being affected by a damage enchantment.", enchant.TargetEntity.GetName());

            // Check if it has HP first, since we have to address this directly.
            EntityBehaviorHealth hp = enchant.TargetEntity.GetBehavior<EntityBehaviorHealth>();
            if (hp == null) return;

            // Configure Damage
            DamageSource source = enchant.ToDamageSource();
            source.Type = DamageType;
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
                            Dictionary<string, int> enchants = sApi.EnchantAccessor().GetEnchantments(inv[i].Itemstack);
                            int rPower = enchants.GetValueOrDefault(DamageResist, 0);
                            resist += rPower * 0.1f;
                        }
                    }
                    resist = 1 - resist;
                    dmg = Math.Max(0f, dmg * resist);
                }
            }
            
            // Apply Damage
            if (enchant.TargetEntity.ShouldReceiveDamage(source, dmg))
            {
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] Dealing {0} {1} damage.", dmg, source.Type.ToString());

                // Disabled because there is something stopping this from happening in rapid succession.
                // Some kind of timer is locking damage, and must be calculated manually here, instead.
                hp.OnEntityReceiveDamage(source, ref dmg);
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
                hp.OnEntityReceiveDamage(source, ref dmg);
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
