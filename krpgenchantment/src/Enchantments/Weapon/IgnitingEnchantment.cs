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
        int TickMultiplier { get { return (int)Modifiers[0]; } }
        int TickDuration { get { return (int)Modifiers[1]; } }

        public IgnitingEnchantment(ICoreAPI api) : base(api)
        {
            Enabled = true;
            Code = "igniting";
            LoreCode = "enchantment-igniting";
            LoreChapterID = 6;
            MaxTier = 5;
            Modifiers = new object[2] { 1, 12500 };

            TickRegistry = new Dictionary<long, EnchantTick>();

            Api.World.RegisterGameTickListener(IgniteTick, 1000);
        }
        public override void OnAttack(EnchantmentSource enchant, ItemSlot slot, ref float? damage)
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
