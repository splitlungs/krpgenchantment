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
        // int TickMultiplier { get { return Attributes.GetInt("TickMultiplier", 1); } }
        // int TickDuration { get { return Attributes.GetInt("TickDuration", 12500); } }
        int TickMultiplier { get { return Convert.ToInt32(Modifiers.GetValueOrDefault("TickMultiplier", 1)); } }
        int TickDuration { get { return Convert.ToInt32(Modifiers.GetValueOrDefault("TickDuration", 12500)); } }
        public IgnitingEnchantment(ICoreAPI api) : base(api)
        {
            // Setup the default config
            Enabled = true;
            Code = "igniting";
            Category = "Weapon";
            LoreCode = "enchantment-igniting";
            LoreChapterID = 6;
            MaxTier = 5;
            Modifiers = new Dictionary<string, object>()
            {
                {"TickMultiplier", 1 }, {"TickDuration", 12500 }
            };
            // Attributes = new TreeAttribute();
            // Attributes.SetInt("TickMultiplier", 1);
            // Attributes.SetInt("TickDuration", 12500);

            Api.World.RegisterGameTickListener(IgniteTick, 1000);
        }
        public override void OnAttack(EnchantmentSource enchant, ref Dictionary<string, object> parameters)
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] {0} is being affected by an Igniting enchantment.", enchant.TargetEntity.GetName());

            int tickMax = enchant.Power * TickMultiplier;
            if (TickRegistry.ContainsKey(enchant.TargetEntity.EntityId))
                TickRegistry[enchant.TargetEntity.EntityId].TicksRemaining = tickMax;
            else
            {
                EnchantTick eTick = new EnchantTick() { TicksRemaining = tickMax, TickTimeStart = Api.World.Calendar.ElapsedSeconds, LastTickTime = 0 };
                TickRegistry.Add(enchant.TargetEntity.EntityId, eTick);
            }
        }

        /// <summary>
        /// Attempt to set the target on fire. Power multiplies number of 12s refreshes.
        /// </summary>
        /// <param name="power"></param>
        private void IgniteTick(float dt)
        {
            foreach (KeyValuePair<long, EnchantTick> keyValuePair in TickRegistry) 
            {
                if (keyValuePair.Value.TicksRemaining > 0 && (keyValuePair.Value.LastTickTime - keyValuePair.Value.TickTimeStart) >= TickDuration)
                {
                    Entity entity = Api.World.GetEntityById(keyValuePair.Key);
                    entity.Ignite();
                    int ticks = keyValuePair.Value.TicksRemaining - 1;
                    TickRegistry[keyValuePair.Key].TicksRemaining = ticks;
                    keyValuePair.Value.LastTickTime = Api.World.ElapsedMilliseconds;
                }
                else
                {
                    TickRegistry.Remove(keyValuePair.Key);
                }
            }
        }
    }
}
