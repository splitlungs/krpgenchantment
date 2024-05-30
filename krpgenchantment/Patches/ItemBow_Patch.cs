using HarmonyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace KRPGLib.Enchantment
{
    /*
    [HarmonyPatch]
    public class ItemBow_Patch
    {
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(ItemBow), "OnHeldInteractStop")]
        public static void OnHeldInteractStop(ItemBow __instance, float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            // This inner transpiler will be applied to the original and
            // the result will replace this method
            //
            // That will allow this method to have a different signature
            // than the original and it must match the transpiled result
            //
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var list = Transpilers.Manipulator(instructions,
                    item => item.opcode == OpCodes.Ldloc_S,
                    item => item.opcode = OpCodes.Callvirt
                ).ToList();
                var mJoin = SymbolExtensions.GetMethodInfo(() => byEntity.World.SpawnEntity);
                var idx = list.FindIndex(item => item.opcode == OpCodes.Call && item.operand as MethodInfo == mJoin);
                list.RemoveRange(idx + 1, list.Count - (idx + 1));
                return list.AsEnumerable();
            }

            // make compiler happy
            _ = Transpiler(null);
        }
    }*/
    // TODO: Deprecate for CollectibleBehavior
    [HarmonyPatch]
    public class ItemBow_Patch
    {
        [HarmonyPatch(typeof(ItemBow), nameof(ItemBow.OnHeldInteractStop))]
        public static bool Prefix(ItemBow __instance, float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (byEntity.Attributes.GetInt("aimingCancel") == 1)
            {
                return false;
            }

            string aimAnimation = Traverse.Create(__instance).Field("aimAnimation").GetValue() as string;

            byEntity.Attributes.SetInt("aiming", 0);
            byEntity.AnimManager.StopAnimation(aimAnimation);
            if (byEntity.World is IClientWorldAccessor)
            {
                slot.Itemstack.TempAttributes.RemoveAttribute("renderVariant");
            }

            slot.Itemstack.Attributes.SetInt("renderVariant", 0);
            (byEntity as EntityPlayer)?.Player?.InventoryManager.BroadcastHotbarSlot();
            if (secondsUsed < 0.65f)
            {
                return false;
            }

            // We can't reach the protected method here
            // ItemSlot nextArrow = __instance.GetNextArrow(byEntity);

            ItemSlot nextArrow = null;
            byEntity.WalkInventory(delegate (ItemSlot invslot)
            {
                if (invslot is ItemSlotCreative)
                {
                    return true;
                }

                if (invslot.Itemstack != null && invslot.Itemstack.Collectible.Code.PathStartsWith("arrow-"))
                {
                    nextArrow = invslot;
                    return false;
                }

                return true;
            });

            if (nextArrow != null)
            {
                float num = 0f;
                float num2 = 0f;
                if (slot.Itemstack.Collectible.Attributes != null)
                {
                    num += slot.Itemstack.Collectible.Attributes["damage"].AsFloat();
                    num2 = 1f - slot.Itemstack.Collectible.Attributes["accuracyBonus"].AsFloat();
                }

                if (nextArrow.Itemstack.Collectible.Attributes != null)
                {
                    num += nextArrow.Itemstack.Collectible.Attributes["damage"].AsFloat();
                }

                // Get Enchantments
                Dictionary<string, int> enchants = new Dictionary<string, int>();
                foreach (var val in Enum.GetValues(typeof(EnumEnchantments)))
                {
                    int ePower = slot.Itemstack.Attributes.GetInt(val.ToString(), 0);
                    if (ePower > 0) { enchants.Add(val.ToString(), ePower); }
                }

                ItemStack itemStack = nextArrow.TakeOut(1);
                nextArrow.MarkDirty();
                IPlayer dualCallByPlayer = null;
                if (byEntity is EntityPlayer)
                {
                    dualCallByPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
                }

                byEntity.World.PlaySoundAt(new AssetLocation("game:sounds/bow-release"), byEntity, dualCallByPlayer, randomizePitch: false, 8f);
                float num3 = 0.5f;
                if (itemStack.ItemAttributes != null)
                {
                    num3 = itemStack.ItemAttributes["breakChanceOnImpact"].AsFloat(0.5f);
                }

                EntityProperties entityType = byEntity.World.GetEntityType(new AssetLocation("krpgenchantment", "enchanted-arrow-" + itemStack.Collectible.Variant["material"]));
                EnchantedEntityProjectile entityProjectile = byEntity.World.ClassRegistry.CreateEntity(entityType) as EnchantedEntityProjectile;
                entityProjectile.FiredBy = byEntity;
                entityProjectile.Damage = num;
                entityProjectile.ProjectileStack = itemStack;
                entityProjectile.DropOnImpactChance = 1f - num3;
                float num4 = Math.Max(0.001f, 1f - byEntity.Attributes.GetFloat("aimingAccuracy"));
                double num5 = byEntity.WatchedAttributes.GetDouble("aimingRandPitch", 1.0) * (double)num4 * (0.75 * (double)num2);
                double num6 = byEntity.WatchedAttributes.GetDouble("aimingRandYaw", 1.0) * (double)num4 * (0.75 * (double)num2);
                Vec3d vec3d = byEntity.ServerPos.XYZ.Add(0.0, byEntity.LocalEyePos.Y, 0.0);
                Vec3d pos = (vec3d.AheadCopy(1.0, (double)byEntity.SidedPos.Pitch + num5, (double)byEntity.SidedPos.Yaw + num6) - vec3d) * byEntity.Stats.GetBlended("bowDrawingStrength");
                entityProjectile.ServerPos.SetPos(byEntity.SidedPos.BehindCopy(0.21).XYZ.Add(0.0, byEntity.LocalEyePos.Y, 0.0));
                entityProjectile.ServerPos.Motion.Set(pos);
                entityProjectile.Pos.SetFrom(entityProjectile.ServerPos);
                entityProjectile.World = byEntity.World;
                entityProjectile.SetRotation();

                // Pass Enchantment Attributes to the Projectile
                foreach (KeyValuePair<string, int> pair in enchants)
                {
                    entityProjectile.WatchedAttributes.SetInt(pair.Key, pair.Value);
                }

                byEntity.World.SpawnEntity(entityProjectile);
                slot.Itemstack.Collectible.DamageItem(byEntity.World, byEntity, slot);
                byEntity.AnimManager.StartAnimation("bowhit");
            }
            return false;
        }
    }
}
