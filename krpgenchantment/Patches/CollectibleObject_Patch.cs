using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.ServerMods.WorldEdit;

namespace KRPGLib.Enchantment
{
    [HarmonyPatch(typeof(CollectibleObject))]
    internal class CollectibleObject_Patch
    {
        [HarmonyPatch("OnAttackingWith")]
        public static void Postfix(CollectibleObject __instance, IWorldAccessor world, Entity byEntity, Entity attackedEntity, ItemSlot itemslot)
        {
            // Alternate Effects
            var power = 0;
            // Chilling
            power = itemslot.Itemstack.Attributes.GetInt("chilling", 0);
            if (power > 0)
            {
                EntityBehaviorBodyTemperature ebbt = attackedEntity.GetBehavior<EntityBehaviorBodyTemperature>();
                if (ebbt == null)
                    return;

                ebbt.CurBodyTemperature = power * -10f;
            }
            // Igniting
            power = itemslot.Itemstack.Attributes.GetInt("igniting", 0);
            if (power > 0)
            {
                attackedEntity.IsOnFire = true;
            }
            // Knockback
            power = itemslot.Itemstack.Attributes.GetInt("knockback", 0);
            if (power > 0)
            {
                EntityPos pos = byEntity.SidedPos;
                float kbresist = attackedEntity.Properties.KnockbackResistance;
                attackedEntity.SidedPos.Motion.Add(kbresist * pos.Motion.X * power, kbresist * pos.Motion.Y * power, kbresist * pos.Motion.Z * power);
            }
            // Call Lightning
            power = itemslot.Itemstack.Attributes.GetInt("lightning", 0);
            if (power > 0)
            {
                WeatherSystemServer weatherSystem = world.Api.ModLoader.GetModSystem<WeatherSystemServer>();
                // It should default to 0f. Stun should stop at 0.5. Absorbtion should start at 1f.
                if (weatherSystem != null)
                    weatherSystem.SpawnLightningFlash(attackedEntity.SidedPos.XYZ);
                else
                    world.Api.Logger.Debug("Could not find Weather System!");
            }
            // Create Pit
            power = itemslot.Itemstack.Attributes.GetInt("pit", 0);
            if (power > 0)
            {
                BlockPos bpos = attackedEntity.ServerPos.AsBlockPos;
                List<Vec3d> pitArea = new List<Vec3d>();

                for (int x = 0; x <= power; x++)
                {
                    for (int y = 0; y <= power; y++)
                    {
                        for (int z = 0; z <= power; z++)
                        {
                            pitArea.Add(new Vec3d(bpos.X + x, bpos.Y - y, bpos.Z + z));
                            pitArea.Add(new Vec3d(bpos.X - x, bpos.Y - y, bpos.Z - z));
                            pitArea.Add(new Vec3d(bpos.X + x, bpos.Y - y, bpos.Z - z));
                            pitArea.Add(new Vec3d(bpos.X - x, bpos.Y - y, bpos.Z + z));
                        }
                    }
                }

                for (int i = 0; i < pitArea.Count; i++)
                {
                    BlockPos ipos = bpos;
                    ipos.Set(pitArea[i]);
                    Block block = byEntity.World.BlockAccessor.GetBlock(ipos);

                    if (block != null)
                    {
                        string blockCode = block.Code.ToString();
                        if (blockCode.Contains("soil") || blockCode.Contains("sand") || blockCode.Contains("gravel"))
                            byEntity.World.BlockAccessor.BreakBlock(ipos, byEntity as IPlayer);
                    }
                }
            }
        }
    }
}
