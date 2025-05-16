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
    public class IgnitingEnchantment : Enchantment
    {
        int TickMultiplier { get { return Modifiers.GetInt("TickMultiplier"); } }
        long TickDuration { get { return Modifiers.GetLong("TickDuration"); } }
        int TickFrequency { get { return Modifiers.GetInt("TickFrequency"); } }
        public IgnitingEnchantment(ICoreAPI api) : base(api)
        {
            // Setup the default config
            Enabled = true;
            Code = "igniting";
            Category = "DamageTick";
            LoreCode = "enchantment-igniting";
            LoreChapterID = 6;
            MaxTier = 5;
            Modifiers = new EnchantModifiers()
            {
                {"TickMultiplier", 1 }, {"TickDuration", 12500 }, {"TickFrequency", 500 }
            };
            Api.World.RegisterGameTickListener(IgniteTick, TickFrequency);
        }
        public override void Initialize(EnchantmentProperties properties)
        {
            base.Initialize(properties);
            // We let the config initialize before registering the Tick Listener
            Api.World.RegisterGameTickListener(IgniteTick, TickFrequency);
        }
        public override void OnAttack(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] {0} is being affected by an Igniting enchantment.", enchant.TargetEntity.GetName());

            int tickMax = enchant.Power * TickMultiplier;
            if (TickRegistry.ContainsKey(enchant.TargetEntity.EntityId))
            {
                TickRegistry[enchant.TargetEntity.EntityId].TicksRemaining = tickMax;
                TickRegistry[enchant.TargetEntity.EntityId].Source = enchant.Clone();
            }
            else if (tickMax > 1)
            {
                EnchantTick eTick =
                    new EnchantTick() { TicksRemaining = tickMax, Source = enchant.Clone(), LastTickTime = Api.World.ElapsedMilliseconds };
                TickRegistry.Add(enchant.TargetEntity.EntityId, eTick);
                enchant.TargetEntity.Ignite();
            }
            else
                enchant.TargetEntity.Ignite();
        }
        /// <summary>
        /// Attempt to set the target on fire. Power multiplies number of 12s refreshes.
        /// </summary>
        public void IgniteTick(float dt)
        {
            foreach (KeyValuePair<long, EnchantTick> pair in TickRegistry) 
            {
                long curDur = Api.World.ElapsedMilliseconds - pair.Value.LastTickTime;
                if (pair.Value.TicksRemaining > 0 && curDur >= TickDuration)
                {
                    if (EnchantingConfigLoader.Config?.Debug == true)
                        Api.Logger.Event("[KRPGEnchantment] Igniting enchantment is performing an Ignite Tick on {0}.", pair.Key);
                    Entity entity = Api.World.GetEntityById(pair.Key);
                    if (entity == null)
                    {
                        if (EnchantingConfigLoader.Config?.Debug == true)
                            Api.Logger.Event("[KRPGEnchantment] Igniting enchantment Ticked a null entity. Removing from TickRegistry.");
                        TickRegistry[pair.Key].Dispose();
                        TickRegistry.Remove(pair.Key);
                        continue;
                    }
                    entity.Ignite();
                    TickRegistry[pair.Key].TicksRemaining = TickRegistry[pair.Key].TicksRemaining--;
                    pair.Value.LastTickTime = Api.World.ElapsedMilliseconds;
                }
                else if (pair.Value.TicksRemaining <= 0)
                {
                    if (EnchantingConfigLoader.Config?.Debug == true)
                        Api.Logger.Event("[KRPGEnchantment] Igniting enchantment finished Ticking for {0}.", pair.Key);
                    TickRegistry[pair.Key].Dispose();
                    TickRegistry.Remove(pair.Key);
                }
            }
        }
    }
}
