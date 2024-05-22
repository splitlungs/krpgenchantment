using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using static System.Net.Mime.MediaTypeNames;

namespace KRPGLib.Enchantment
{
    [HarmonyPatch(typeof(EntityProjectile))]
    internal class EntityProjectile_Patch
    {
        [HarmonyPatch("impactOnEntity")]
        public static bool Prefix(EntityProjectile __instance, Entity entity)
        {
            if (!entity.Alive) return true;

            if (__instance != null)
            {
                var msCollide = (double)Traverse.Create(__instance).Field("msCollide").GetValue();

                EntityPos pos = __instance.SidedPos;

                IServerPlayer fromPlayer = null;
                if (__instance.FiredBy is EntityPlayer)
                {
                    fromPlayer = (__instance.FiredBy as EntityPlayer).Player as IServerPlayer;
                }

                bool targetIsPlayer = entity is EntityPlayer;
                bool targetIsCreature = entity is EntityAgent;
                bool canDamage = true;

                ICoreServerAPI sapi = __instance.World.Api as ICoreServerAPI;
                if (fromPlayer != null)
                {
                    if (targetIsPlayer && (!sapi.Server.Config.AllowPvP || !fromPlayer.HasPrivilege("attackplayers"))) canDamage = false;
                    if (targetIsCreature && !fromPlayer.HasPrivilege("attackcreatures")) canDamage = false;
                }

                msCollide = __instance.World.ElapsedMilliseconds;

                pos.Motion.Set(0, 0, 0);
                if (canDamage && __instance.World.Side == EnumAppSide.Server)
                {
                    __instance.World.PlaySoundAt(new AssetLocation("game:sounds/arrow-impact"), __instance, null, false, 24);

                    // Alternate Damage
                    int flaming = __instance.WatchedAttributes.GetInt("flaming", 0);
                    int frost = __instance.WatchedAttributes.GetInt("frost", 0);
                    int harming = __instance.WatchedAttributes.GetInt("harming", 0);
                    int healing = __instance.WatchedAttributes.GetInt("healing", 0);
                    int shocking = __instance.WatchedAttributes.GetInt("shocking", 0);

                    bool didDamage = false;
                    // Healing
                    if (healing > 0)
                    {
                        DamageSource dSource = new DamageSource();
                        dSource.Source = EnumDamageSource.Entity;
                        dSource.SourceEntity = __instance.FiredBy == null ? __instance : __instance.FiredBy;
                        dSource.Type = EnumDamageType.Heal;
                        float dmg = __instance.World.Rand.Next(1, 6) + healing;
                        didDamage = entity.ReceiveDamage(dSource, dmg);
                    }
                    // Base
                    else
                    {
                        DamageSource dSource = new DamageSource();
                        dSource.Source = EnumDamageSource.Entity;
                        dSource.SourceEntity = __instance.FiredBy == null ? __instance : __instance.FiredBy;
                        float dmg = __instance.Damage;
                        if (__instance.FiredBy != null) dmg *= __instance.FiredBy.Stats.GetBlended("rangedWeaponsDamage");
                        didDamage = entity.ReceiveDamage(dSource, dmg);
                    }
                    // Flaming
                    if (flaming > 0)
                    {
                        DamageSource dSource = new DamageSource();
                        dSource.Source = EnumDamageSource.Entity;
                        dSource.SourceEntity = __instance.FiredBy == null ? __instance : __instance.FiredBy;
                        dSource.Type = EnumDamageType.Fire;
                        float dmg = __instance.World.Rand.Next(1, 6) + flaming;
                        didDamage = entity.ReceiveDamage(dSource, dmg);
                    }
                    // Frost
                    if (frost > 0)
                    {
                        DamageSource dSource = new DamageSource();
                        dSource.Source = EnumDamageSource.Entity;
                        dSource.SourceEntity = __instance.FiredBy == null ? __instance : __instance.FiredBy;
                        dSource.Type = EnumDamageType.Frost;
                        float dmg = __instance.World.Rand.Next(1, 6) + frost;
                        didDamage = entity.ReceiveDamage(dSource, dmg);
                    }
                    // Harming
                    if (harming > 0)
                    {
                        DamageSource dSource = new DamageSource();
                        dSource.Source = EnumDamageSource.Entity;
                        dSource.SourceEntity = __instance.FiredBy == null ? __instance : __instance.FiredBy;
                        dSource.Type = EnumDamageType.Injury;
                        float dmg = __instance.World.Rand.Next(1, 6) + harming;
                        didDamage = entity.ReceiveDamage(dSource, dmg);
                    }
                    // Shocking
                    if (shocking > 0)
                    {
                        DamageSource dSource = new DamageSource();
                        dSource.Source = EnumDamageSource.Entity;
                        dSource.SourceEntity = __instance.FiredBy == null ? __instance : __instance.FiredBy;
                        dSource.Type = EnumDamageType.Electricity;
                        float dmg = __instance.World.Rand.Next(1, 6) + shocking;
                        didDamage = entity.ReceiveDamage(dSource, dmg);
                    }
                    // Base Knockback
                    float kbresist = entity.Properties.KnockbackResistance;
                    entity.SidedPos.Motion.Add(kbresist * pos.Motion.X * __instance.Weight, kbresist * pos.Motion.Y * __instance.Weight, kbresist * pos.Motion.Z * __instance.Weight);

                    int power = 0;
                    // Chilling
                    power = __instance.Attributes.GetInt("chilling", 0);
                    if (power > 0)
                    {
                        EntityBehaviorBodyTemperature ebbt = entity.GetBehavior<EntityBehaviorBodyTemperature>();

                        // If we encounter something without one, bail
                        if (ebbt == null)
                            return true;

                        ebbt.CurBodyTemperature = power * -10f;
                    }
                    // Igniting
                    if (__instance.Attributes.GetInt("igniting", 0) > 0)
                    {
                        entity.IsOnFire = true;
                    }
                    // Knockback
                    power = __instance.WatchedAttributes.GetInt("knockback", 0);
                    if (power > 0)
                    {
                        double weightedPower = __instance.Weight + power * 100;
                        entity.SidedPos.Motion.Mul(-weightedPower, 1, -weightedPower);
                    }
                    // Lightning
                    if (__instance.Attributes.GetInt("lightning", 0) > 0)
                    {
                        WeatherSystemServer weatherSystem = __instance.World.Api.ModLoader.GetModSystem<WeatherSystemServer>();
                        // It should default to 0f. Stun should stop at 0.5. Absorbtion should start at 1f.
                        weatherSystem.SpawnLightningFlash(entity.ServerPos.XYZ);
                    }
                    // Pit
                    power = __instance.Attributes.GetInt("pit", 0);
                    if (power > 0)
                    {
                        BlockPos bpos = entity.ServerPos.AsBlockPos;
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
                            Block block = __instance.World.BlockAccessor.GetBlock(ipos);

                            if (block != null)
                            {
                                string blockCode = block.Code.ToString();
                                if (blockCode.Contains("soil") || blockCode.Contains("sand") || blockCode.Contains("gravel"))
                                    __instance.World.BlockAccessor.BreakBlock(ipos, entity as IPlayer);
                            }
                        }
                    }

                    if (__instance.FiredBy is EntityPlayer)
                    {
                        __instance.World.PlaySoundFor(new AssetLocation("game:sounds/player/projectilehit"), (__instance.FiredBy as EntityPlayer).Player, false, 24);
                    }
                }
            }
            return true;
        }
    }
}
