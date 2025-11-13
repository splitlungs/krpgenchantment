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
        public IgnitingEnchantment(ICoreAPI api) : base(api)
        {
            // Setup the default config
            Enabled = true;
            Code = "igniting";
            Category = "DamageTick";
            LoreCode = "enchantment-igniting";
            LoreChapterID = 6;
            MaxTier = 5;
            ValidToolTypes = new List<string>() {
                "Knife", "Axe",
                "Club", "Sword",
                "Spear",
                "Bow", "Sling",
                "Drill",
                "Halberd", "Mace", "Pike", "Polearm", "Poleaxe", "Quarterstaff", "Sabre", "Staff", "Warhammer",
                "Javelin",
                "Crossbow", "Firearm",
                "Wand" };
            Modifiers = new EnchantModifiers()
            {
                {"TickMultiplier", 1 }, {"TickDuration", 12500 }
            };
            Version = 1.01f;
        }
        public override void OnAttack(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] {0} is being affected by an Igniting enchantment.", enchant.TargetEntity.GetName());

            EnchantmentEntityBehavior eeb = enchant.TargetEntity.GetBehavior<EnchantmentEntityBehavior>();
            if (eeb != null)
            {
                EnchantTick eTick = enchant.ToEnchantTick();
                int tickMax = (enchant.Power * TickMultiplier) - 1;
                eTick.TicksRemaining = tickMax;
                eTick.TickDuration = TickDuration;
                if (eeb.TickRegistry.ContainsKey(Code))
                {
                    eeb.TickRegistry[Code] = eTick;
                }
                else if (tickMax > 1)
                {
                    eTick.LastTickTime = Api.World.ElapsedMilliseconds;
                    eeb.TickRegistry.Add(enchant.Code, eTick);
                    enchant.TargetEntity.Ignite();
                }
                else
                    enchant.TargetEntity.Ignite();
            }
        }
        public override void OnTick(ref EnchantTick eTick)
        {
            long curDur = Api.World.ElapsedMilliseconds - eTick.LastTickTime;
            int tr = eTick.TicksRemaining;

            if (tr > 0 && curDur >= TickDuration)
            {
                // if (EnchantingConfigLoader.Config?.Debug == true)
                //     Api.Logger.Event("[KRPGEnchantment] Igniting enchantment is performing an Ignite Tick on {0}.", eTick.Source.TargetEntity.GetName());
                Entity entity = Api.World.GetEntityById(eTick.TargetEntityID);
                if (entity == null)
                {
                    if (EnchantingConfigLoader.Config?.Debug == true)
                        Api.Logger.Event("[KRPGEnchantment] Igniting enchantment Ticked a null entity. Removing from TickRegistry.");
                    eTick.Dispose();
                    return;
                }
                entity.Ignite();
                eTick.TicksRemaining = tr - 1;
                eTick.LastTickTime = Api.World.ElapsedMilliseconds;
            }
            else if (tr <= 0)
            {
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] Igniting enchantment finished Ticking for {0}.", Code);
                eTick.Dispose();
            }
            else
                eTick.LastTickTime = Api.World.ElapsedMilliseconds;
        }
        /// <summary>
        /// Attempt to set the target on fire. Power multiplies number of 12s refreshes.
        /// </summary>
        // [Obsolete]
        // public void IgniteTick(float dt)
        // {
        //     foreach (KeyValuePair<long, EnchantTick> pair in TickRegistry) 
        //     {
        //         long curDur = Api.World.ElapsedMilliseconds - pair.Value.LastTickTime;
        //         int tr = pair.Value.TicksRemaining;
        // 
        //         if (tr > 0 && curDur >= TickDuration)
        //         {
        //             if (EnchantingConfigLoader.Config?.Debug == true)
        //                 Api.Logger.Event("[KRPGEnchantment] Igniting enchantment is performing an Ignite Tick on {0}.", pair.Key);
        //             Entity entity = Api.World.GetEntityById(pair.Key);
        //             if (entity == null)
        //             {
        //                 if (EnchantingConfigLoader.Config?.Debug == true)
        //                     Api.Logger.Event("[KRPGEnchantment] Igniting enchantment Ticked a null entity. Removing from TickRegistry.");
        //                 TickRegistry[pair.Key].Dispose();
        //                 TickRegistry.Remove(pair.Key);
        //                 continue;
        //             }
        //             entity.Ignite();
        //             TickRegistry[pair.Key].TicksRemaining = tr -1;
        //             pair.Value.LastTickTime = Api.World.ElapsedMilliseconds;
        //         }
        //         else if (tr <= 0)
        //         {
        //             if (EnchantingConfigLoader.Config?.Debug == true)
        //                 Api.Logger.Event("[KRPGEnchantment] Igniting enchantment finished Ticking for {0}.", pair.Key);
        //             TickRegistry[pair.Key].Dispose();
        //             TickRegistry.Remove(pair.Key);
        //         }
        //     }
    }
}
