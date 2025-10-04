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
        int EffectRadius { get { return Modifiers.GetInt("EffectRadius"); } }
        ICoreServerAPI sApi;
        WeatherSystemServer weatherSystem;
        public LightningEnchantment(ICoreAPI api) : base(api)
        {
            // Setup the default config
            Enabled = true;
            Code = "lightning";
            Category = "DamageArea";
            LoreCode = "enchantment-lightning";
            LoreChapterID = 8;
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
                {"Delay", 500 }, {"PowerMultiplier", 0.5 }, {"MaxBonusStrikes", 1 }, {"EffectRadius", 4 }
            };
            Version = 1.00f;

            sApi = Api as ICoreServerAPI;
            weatherSystem = sApi.ModLoader.GetModSystem<WeatherSystemServer>();
        }
        public override void OnAttack(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] {0} is being affected by an Lightning enchantment.", enchant.TargetEntity.GetName());
            EnchantmentEntityBehavior eeb = enchant.TargetEntity.GetBehavior<EnchantmentEntityBehavior>();
            if (eeb != null)
            {
                // Refresh ticks if needed
                if (eeb.TickRegistry.ContainsKey(Code))
                {
                    int mul = (int)Math.Abs(enchant.Power * PowerMultiplier);
                    int roll = Api.World.Rand.Next(enchant.Power - mul, enchant.Power + MaxBonusStrikes);
                    eeb.TickRegistry[Code].TicksRemaining = roll;
                    eeb.TickRegistry[Code].Source = enchant.Clone();
                }
                else if (enchant.Power == 1)
                {
                    EnchantTick tick =
                        new EnchantTick() { LastTickTime = Api.World.ElapsedMilliseconds, Source = enchant.Clone(), TicksRemaining = enchant.Power };
                    eeb.TickRegistry.Add(Code, tick);
                }
                else if (enchant.Power > 1)
                {
                    int mul = (int)Math.Abs(enchant.Power * PowerMultiplier);
                    int roll = Api.World.Rand.Next(enchant.Power - mul, enchant.Power + MaxBonusStrikes);
                    EnchantTick tick = new EnchantTick() { LastTickTime = Api.World.ElapsedMilliseconds, Source = enchant.Clone(), TicksRemaining = roll };
                    eeb.TickRegistry.Add(Code, tick);
                }
                else
                    Api.Logger.Error("[KRPGEnchantment] Call Lightning was registered against {0} with Power 0 or less!", enchant.TargetEntity.EntityId);
            }
        }
        public override void OnTick(ref EnchantTick eTick)
        {
            long curDur = Api.World.ElapsedMilliseconds - eTick.LastTickTime;
            int tr = eTick.TicksRemaining;

            if (tr > 0 && curDur >= Delay)
            {
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] Lightning enchantment is performing a Lightning Tick on {0}.", eTick.Source.TargetEntity.GetName());
                Entity entity = eTick.Source.TargetEntity;
                if (entity == null)
                {
                    if (EnchantingConfigLoader.Config?.Debug == true)
                        Api.Logger.Event("[KRPGEnchantment] Lightning enchantment Ticked a null entity. Removing from TickRegistry.");
                    eTick.Dispose();
                    return;
                }
                SpawnLightning(eTick.Source.TargetEntity.SidedPos.XYZ);
                eTick.TicksRemaining = tr - 1;
                eTick.LastTickTime = Api.World.ElapsedMilliseconds;
            }
            else if (tr <= 0)
            {
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] Lightning enchantment finished Ticking for {0}.", Code);
                eTick.Dispose();
            }
        }
        private void SpawnLightning(Vec3d pos)
        {
            if (weatherSystem == null)
                return;

            Vec3d offSet =
                    new Vec3d(Api.World.Rand.Next(-EffectRadius, EffectRadius + 1) + Api.World.Rand.NextDouble(), 0,
                    Api.World.Rand.Next(-EffectRadius, EffectRadius + 1) + Api.World.Rand.NextDouble());
            weatherSystem.SpawnLightningFlash(pos + offSet);
        }
    }
}
