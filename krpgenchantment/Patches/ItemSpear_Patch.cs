using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.API.Datastructures;

namespace KRPGLib.Enchantment
{
    // TODO: Deprecate for CollectibleBehavior
    /*
    [HarmonyPatch]
    public class ItemSpear_Patch
    {
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(ItemSpear), "OnHeldInteractStop")]
        public static void OnHeldInteractStop(ItemSpear __instance, float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (byEntity.Attributes.GetInt("aimingCancel") == 1)
            {
                return;
            }

            byEntity.Attributes.SetInt("aiming", 0);
            byEntity.StopAnimation("aim");
            if (secondsUsed < 0.35f)
            {
                return;
            }

            float damage = 1.5f;
            if (slot.Itemstack.Collectible.Attributes != null)
            {
                damage = slot.Itemstack.Collectible.Attributes["damage"].AsFloat();
            }

            // Get Enchantments
            Dictionary<string, int> enchants = new Dictionary<string, int>();
            foreach (var val in Enum.GetValues(typeof(EnumEnchantments)))
            {
                int ePower = slot.Itemstack.Attributes.GetInt(val.ToString(), 0);
                if (ePower > 0) 
                { 
                    enchants.Add(val.ToString(), ePower); 
                    byEntity.Api.Logger.Event("Found {0} on ItemSpear before throw.", val.ToString());
                }
            }


            (byEntity.Api as ICoreClientAPI)?.World.AddCameraShake(0.17f);
            ItemStack projectileStack = slot.TakeOut(1);
            slot.MarkDirty();
            IPlayer player = null;
            if (byEntity is EntityPlayer)
            {
                player = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
            }

            byEntity.World.PlaySoundAt(new AssetLocation("game:sounds/player/throw"), byEntity, player, randomizePitch: false, 8f);
            EntityProperties entityType = byEntity.World.GetEntityType(new AssetLocation("krpgenchantment", "enchanted-" + __instance.Attributes["spearEntityCode"].AsString()));
            EnchantedEntityProjectile entityProjectile = byEntity.World.ClassRegistry.CreateEntity(entityType) as EnchantedEntityProjectile;
            entityProjectile.FiredBy = byEntity;
            entityProjectile.Damage = damage;
            entityProjectile.ProjectileStack = projectileStack;
            entityProjectile.DropOnImpactChance = 1.1f;
            entityProjectile.DamageStackOnImpact = true;
            entityProjectile.Weight = 0.3f;
            float num = 1f - byEntity.Attributes.GetFloat("aimingAccuracy");
            double num2 = byEntity.WatchedAttributes.GetDouble("aimingRandPitch", 1.0) * (double)num * 0.75;
            double num3 = byEntity.WatchedAttributes.GetDouble("aimingRandYaw", 1.0) * (double)num * 0.75;
            Vec3d vec3d = byEntity.ServerPos.XYZ.Add(0.0, byEntity.LocalEyePos.Y - 0.2, 0.0);
            Vec3d pos = (vec3d.AheadCopy(1.0, (double)byEntity.ServerPos.Pitch + num2, (double)byEntity.ServerPos.Yaw + num3) - vec3d) * 0.65;
            Vec3d pos2 = byEntity.ServerPos.BehindCopy(0.21).XYZ.Add(byEntity.LocalEyePos.X, byEntity.LocalEyePos.Y - 0.2, byEntity.LocalEyePos.Z);
            entityProjectile.ServerPos.SetPos(pos2);
            entityProjectile.ServerPos.Motion.Set(pos);
            entityProjectile.Pos.SetFrom(entityProjectile.ServerPos);
            entityProjectile.World = byEntity.World;
            entityProjectile.SetRotation();

            // Pass Enchantment Attributes to the Projectile
            foreach (KeyValuePair<string, int> pair in enchants)
            {
                entityProjectile.WatchedAttributes.SetInt(pair.Key, pair.Value);
                byEntity.Api.Logger.Event("Found {0} on ItemSpear before throw.", pair.Key.ToString());
            }

            byEntity.World.SpawnEntity(entityProjectile);
            byEntity.StartAnimation("throw");
            if (byEntity is EntityPlayer)
            {
                __instance.RefillSlotIfEmpty(slot, byEntity, (ItemStack itemstack) => itemstack.Collectible is ItemSpear);
            }

            float pitchModifier = (byEntity as EntityPlayer).talkUtil.pitchModifier;
            player.Entity.World.PlaySoundAt(new AssetLocation("game:sounds/player/strike"), player.Entity, player, pitchModifier * 0.9f + (float)byEntity.Api.World.Rand.NextDouble() * 0.2f, 16f, 0.35f);
        }
    }*/
}
