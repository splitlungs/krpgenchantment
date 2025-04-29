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
        int Delay { get { return (int)Modifiers.GetValueOrDefault("Delay", 500); } }
        float PowerMultiplier { get { return (float)Modifiers.GetValueOrDefault("PowerMultiplier", 0.5f); } }
        int MaxBonusStrikes { get { return (int)Modifiers.GetValueOrDefault("MaxBonusStrikes", 1); } }
        ICoreServerAPI sApi;
        WeatherSystemServer weatherSystem;
        public LightningEnchantment(ICoreAPI api) : base(api)
        {
            // Setup the default config
            Enabled = true;
            Code = "lightning";
            Category = "Weapon";
            LoreCode = "enchantment-lightning";
            LoreChapterID = 8;
            MaxTier = 5;
            Modifiers = new Dictionary<string, object>()
            { 
                {"Delay", 500 }, {"PowerMultiplier", 0.5 }, {"MaxBonusStrikes", 1 }
            };

            sApi = (ICoreServerAPI)Api;
            weatherSystem = sApi.ModLoader.GetModSystem<WeatherSystemServer>();
            TickRegistry = new Dictionary<long, EnchantTick>();

            Api.World.RegisterGameTickListener(SpawnLightning, Delay);
        }
        public override void OnAttack(EnchantmentSource enchant, ItemSlot slot, ref float? damage)
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
                EnchantTick tick = new EnchantTick() { LastTickTime = 0, TicksRemaining = enchant.Power, TickTimeStart = Api.World.Calendar.ElapsedSeconds };
                TickRegistry.Add(enchant.TargetEntity.EntityId, tick);
            }
            else if (enchant.Power > 1)
            {
                int mul = (int)Math.Abs(enchant.Power * PowerMultiplier);
                int roll = Api.World.Rand.Next(enchant.Power - mul, enchant.Power + MaxBonusStrikes);
                EnchantTick tick = new EnchantTick() { LastTickTime = 0, TicksRemaining = roll, TickTimeStart = Api.World.Calendar.ElapsedSeconds };
                TickRegistry.Add(enchant.TargetEntity.EntityId, tick);
            }
            else
                Api.Logger.Error("[KRPGEnchantment] Call Lightning was registered against {0} with Power 0 or less!", enchant.TargetEntity.EntityId);


            if (EnchantingConfigLoader.Config.Debug == true)
                Api.Logger.VerboseDebug("[KRPGEnchantment] Durable Enchantment processed with {0} damage to item {1}.", damage, slot.Itemstack.GetName());
        }
        private void SpawnLightning(float dt)
        {
            if (weatherSystem == null)
                return;

            foreach (KeyValuePair<long, EnchantTick> keyValuePair in TickRegistry)
            {
                if (keyValuePair.Value.TicksRemaining > 0)
                {
                    Entity entity = sApi.World.GetEntityById(keyValuePair.Key);
                    // double xDelta = Api.World.Rand.Next(0, 5) + Api.World.Rand.NextDouble();
                    // double zDelta = Api.World.Rand.Next(0, 5) + Api.World.Rand.NextDouble();
                    Vec3d offSet = new Vec3d(Api.World.Rand.Next(-4, 5) + Api.World.Rand.NextDouble(), 0, Api.World.Rand.Next(-4, 5) + Api.World.Rand.NextDouble());
                    weatherSystem.SpawnLightningFlash(entity.SidedPos.XYZ + offSet);
                    TickRegistry[keyValuePair.Key].TicksRemaining--;
                }
                else
                {
                    TickRegistry.Remove(keyValuePair.Key);
                }
            }
        }
    }
}
