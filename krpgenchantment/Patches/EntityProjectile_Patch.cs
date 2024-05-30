using HarmonyLib;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace KRPGLib.Enchantment
{
    [HarmonyPatch]
    public class EntityProjectile_Patch
    {
        [HarmonyPatch(typeof(EntityProjectile), "impactOnEntity")]
        public static bool Prefix(EntityProjectile __instance, Entity entity)
        {
            if (!entity.Alive) return false;

            if (__instance != null)
            {
                var msCollide = (long)Traverse.Create(__instance).Field("msCollide").GetValue();

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

                    // Get Enchantments
                    Dictionary<string, int> enchants = new Dictionary<string, int>();
                    foreach (var val in Enum.GetValues(typeof(EnumEnchantments)))
                    {
                        // Item overrides Entity's Enchantment
                        int ePower = __instance.ProjectileStack.Attributes.GetInt(val.ToString(), 0);
                        if (ePower > 0) 
                        { 
                            enchants.Add(val.ToString(), ePower);
                        }
                        else
                        {
                            ePower = __instance.WatchedAttributes.GetInt(val.ToString(), 0);
                            if (ePower > 0) { enchants.Add(val.ToString(), ePower); }
                        }
                    }

                    bool didDamage = false;
                    // Healing
                    if (enchants.ContainsKey(EnumEnchantments.healing.ToString()))
                    {
                        DamageSource dSource = new DamageSource();
                        dSource.Source = EnumDamageSource.Entity;
                        dSource.SourceEntity = __instance.FiredBy == null ? __instance : __instance.FiredBy;
                        dSource.Type = EnumDamageType.Heal;
                        float dmg = __instance.World.Rand.Next(1, 6) + enchants[EnumEnchantments.healing.ToString()];
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
                    if (enchants.ContainsKey(EnumEnchantments.flaming.ToString()))
                    {
                        DamageSource dSource = new DamageSource();
                        dSource.Source = EnumDamageSource.Entity;
                        dSource.SourceEntity = __instance.FiredBy == null ? __instance : __instance.FiredBy;
                        dSource.Type = EnumDamageType.Fire;
                        float dmg = __instance.World.Rand.Next(1, 6) + enchants[EnumEnchantments.flaming.ToString()];
                        didDamage = entity.ReceiveDamage(dSource, dmg);
                    }
                    // Frost
                    if (enchants.ContainsKey(EnumEnchantments.frost.ToString()))
                    {
                        DamageSource dSource = new DamageSource();
                        dSource.Source = EnumDamageSource.Entity;
                        dSource.SourceEntity = __instance.FiredBy == null ? __instance : __instance.FiredBy;
                        dSource.Type = EnumDamageType.Frost;
                        float dmg = __instance.World.Rand.Next(1, 6) + enchants[EnumEnchantments.frost.ToString()];
                        didDamage = entity.ReceiveDamage(dSource, dmg);
                    }
                    // Harming
                    if (enchants.ContainsKey(EnumEnchantments.harming.ToString()))
                    {
                        DamageSource dSource = new DamageSource();
                        dSource.Source = EnumDamageSource.Entity;
                        dSource.SourceEntity = __instance.FiredBy == null ? __instance : __instance.FiredBy;
                        dSource.Type = EnumDamageType.Injury;
                        float dmg = __instance.World.Rand.Next(1, 6) + enchants[EnumEnchantments.harming.ToString()];
                        didDamage = entity.ReceiveDamage(dSource, dmg);
                    }
                    // Shocking
                    if (enchants.ContainsKey(EnumEnchantments.shocking.ToString()))
                    {
                        DamageSource dSource = new DamageSource();
                        dSource.Source = EnumDamageSource.Entity;
                        dSource.SourceEntity = __instance.FiredBy == null ? __instance : __instance.FiredBy;
                        dSource.Type = EnumDamageType.Electricity;
                        float dmg = __instance.World.Rand.Next(1, 6) + enchants[EnumEnchantments.shocking.ToString()];
                        didDamage = entity.ReceiveDamage(dSource, dmg);
                    }
                    // Base Knockback
                    float kbresist = entity.Properties.KnockbackResistance;
                    entity.SidedPos.Motion.Add(kbresist * pos.Motion.X * __instance.Weight, kbresist * pos.Motion.Y * __instance.Weight, kbresist * pos.Motion.Z * __instance.Weight);

                    // Chilling
                    if (enchants.ContainsKey(EnumEnchantments.chilling.ToString()))
                    {
                        EntityBehaviorBodyTemperature ebbt = entity.GetBehavior<EntityBehaviorBodyTemperature>();

                        // If we encounter something without one, bail
                        if (ebbt == null)
                            return false;

                        ebbt.CurBodyTemperature = enchants[EnumEnchantments.chilling.ToString()] * -10f;
                    }
                    // Igniting
                    if (enchants.ContainsKey(EnumEnchantments.igniting.ToString()))
                    {
                        entity.IsOnFire = true;
                    }
                    // Knockback
                    if (enchants.ContainsKey(EnumEnchantments.knockback.ToString()))
                    {
                        double weightedPower = __instance.Weight + enchants[EnumEnchantments.knockback.ToString()] * 100;
                        entity.SidedPos.Motion.Mul(-weightedPower, 1, -weightedPower);
                    }
                    // Lightning
                    if (enchants.ContainsKey(EnumEnchantments.lightning.ToString()))
                    {
                        WeatherSystemServer weatherSystem = __instance.World.Api.ModLoader.GetModSystem<WeatherSystemServer>();
                        // It should default to 0f. Stun should stop at 0.5. Absorbtion should start at 1f.
                        weatherSystem.SpawnLightningFlash(entity.ServerPos.XYZ);
                    }
                    // Pit
                    if (enchants.ContainsKey(EnumEnchantments.pit.ToString()))
                    {
                        BlockPos bpos = entity.ServerPos.AsBlockPos;
                        List<Vec3d> pitArea = new List<Vec3d>();

                        for (int x = 0; x <= enchants[EnumEnchantments.pit.ToString()]; x++)
                        {
                            for (int y = 0; y <= enchants[EnumEnchantments.pit.ToString()]; y++)
                            {
                                for (int z = 0; z <= enchants[EnumEnchantments.pit.ToString()]; z++)
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

            return false;
        }
    }
}