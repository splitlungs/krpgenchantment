using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using KRPGLib.Enchantment.API;
using Vintagestory.GameContent;

namespace KRPGLib.Enchantment
{
    public class LightningEnchantment : Enchantment
    {
        long Delay { get { return Modifiers.GetLong("Delay"); } }
        float PowerMultiplier { get { return Modifiers.GetFloat("PowerMultiplier"); } }
        int MaxBonusStrikes { get { return Modifiers.GetInt("MaxBonusStrikes"); } }
        ICoreServerAPI sApi;
        WeatherSystemServer weatherSystem;
        public LightningEnchantment(ICoreAPI api) : base(api)
        {
            // Setup the default config
            Enabled = true;
            Code = "lightning";
            Category = "Tick";
            LoreCode = "enchantment-lightning";
            LoreChapterID = 8;
            MaxTier = 5;
            Modifiers = new EnchantModifiers()
            { 
                {"Delay", 500 }, {"PowerMultiplier", 0.5 }, {"MaxBonusStrikes", 1 }
            };

            sApi = Api as ICoreServerAPI;
            weatherSystem = sApi.ModLoader.GetModSystem<WeatherSystemServer>();
            Api.World.RegisterGameTickListener(SpawnLightning, 500);
        }
        public override void OnAttack(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] {0} is being affected by an Lightning enchantment.", enchant.TargetEntity.GetName());

            // Refresh ticks if needed
            if (TickRegistry.ContainsKey(enchant.TargetEntity.EntityId))
            {
                TickRegistry[enchant.TargetEntity.EntityId].TicksRemaining = enchant.Power;
            }
            else if (enchant.Power == 1)
            {
                EnchantTick tick = new EnchantTick() { LastTickTime = Api.World.ElapsedMilliseconds, TicksRemaining = enchant.Power };
                TickRegistry.Add(enchant.TargetEntity.EntityId, tick);
            }
            else if (enchant.Power > 1)
            {
                int mul = (int)Math.Abs(enchant.Power * PowerMultiplier);
                int roll = Api.World.Rand.Next(enchant.Power - mul, enchant.Power + MaxBonusStrikes);
                EnchantTick tick = new EnchantTick() { LastTickTime = Api.World.ElapsedMilliseconds, TicksRemaining = roll };
                TickRegistry.Add(enchant.TargetEntity.EntityId, tick);
            }
            else
                Api.Logger.Error("[KRPGEnchantment] Call Lightning was registered against {0} with Power 0 or less!", enchant.TargetEntity.EntityId);
        }
        private void SpawnLightning(float dt)
        {
            if (weatherSystem == null)
                return;

            foreach (KeyValuePair<long, EnchantTick> pair in TickRegistry)
            {
                long curDur = Api.World.ElapsedMilliseconds - pair.Value.LastTickTime;
                if (pair.Value.TicksRemaining > 0 && curDur >= Delay)
                {
                    if (EnchantingConfigLoader.Config?.Debug == true)
                        Api.Logger.Event("[KRPGEnchantment] Lightning enchantment is performing an Lightning Tick on {0}.", pair.Key);
                    Entity entity = Api.World.GetEntityById(pair.Key);
                    if (entity == null)
                    {
                        if (EnchantingConfigLoader.Config?.Debug == true)
                            Api.Logger.Event("[KRPGEnchantment] Lightning enchantment Ticked a null entity. Removing from TickRegistry.");
                        TickRegistry[pair.Key].Dispose();
                        TickRegistry.Remove(pair.Key);
                        continue;
                    }
                    // double xDelta = Api.World.Rand.Next(0, 5) + Api.World.Rand.NextDouble();
                    // double zDelta = Api.World.Rand.Next(0, 5) + Api.World.Rand.NextDouble();
                    Vec3d offSet = 
                        new Vec3d(Api.World.Rand.Next(-4, 5) + Api.World.Rand.NextDouble(), 0, Api.World.Rand.Next(-4, 5) + Api.World.Rand.NextDouble());
                    weatherSystem.SpawnLightningFlash(entity.SidedPos.XYZ + offSet);
                    TickRegistry[pair.Key].TicksRemaining--;
                }
                else if (pair.Value.TicksRemaining <= 0)
                {
                    if (EnchantingConfigLoader.Config?.Debug == true)
                        Api.Logger.Event("[KRPGEnchantment] Lightning enchantment finished Ticking for {0}.", pair.Key);
                    TickRegistry[pair.Key].Dispose();
                    TickRegistry.Remove(pair.Key);
                }
            }
        }
    }
}
