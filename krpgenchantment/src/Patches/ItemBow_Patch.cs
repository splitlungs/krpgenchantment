using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.API.Datastructures;
using System.Reflection.Emit;
using Vintagestory.Common;
using System.Runtime.CompilerServices;
using System.Reflection;

namespace KRPGLib.Enchantment
{
    [HarmonyPatch]
    public static class ItemBow_Patch
    {
        // private delegate void BaseOnHeldInteractStartDelegate(CollectibleObject instance, ItemSlot slot, EntityAgent byEntity,BlockSelection blockSel, EntitySelection entitySel, 
        //     bool firstEvent, ref EnumHandHandling handling);
        // static readonly BaseOnHeldInteractStartDelegate baseStartCall =
        //     (BaseOnHeldInteractStartDelegate)Delegate.CreateDelegate(typeof(BaseOnHeldInteractStartDelegate), null, 
        //     AccessTools.Method(typeof(CollectibleObject), "OnHeldInteractStart"));
        // private delegate ItemStack GetNextArrowDelegate(
        //     CollectibleObject instance,
        //     EntityAgent byEntity
        // );
        // static readonly GetNextArrowDelegate getNextArrow =
        //     (GetNextArrowDelegate)Delegate.CreateDelegate(
        //         typeof(GetNextArrowDelegate),
        //         null,
        //         AccessTools.Method(typeof(CollectibleObject), "GetNextArrow")
        //     );
        // [HarmonyPrefix]
        // [HarmonyPatch(typeof(ItemBow), nameof(ItemBow.OnHeldInteractStart))]
        // public static bool OnHeldInteractStart_Prefix(ItemBow __instance, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, 
        //     bool firstEvent, ref EnumHandHandling handling, ref string ___aimAnimation)
        // {
        //     baseStartCall(__instance, slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
        //     if (handling != EnumHandHandling.PreventDefault && (!(byEntity.MountedOn?.Controls ?? byEntity.Controls).CtrlKey 
        //         || (entitySel?.SelectionBoxIndex ?? (-1)) < 0 || entitySel.Entity?.GetBehavior<EntityBehaviorAttachable>() == null) 
        //         && getNextArrow(__instance, byEntity) != null)
        //     {
        //         if (byEntity.World is IClientWorldAccessor)
        //         {
        //             slot.Itemstack.TempAttributes.SetInt("renderVariant", 1);
        //         }
        //         string anim = ___aimAnimation;
        //         if (byEntity.Api.EnchantAccessor().GetActiveEnchantments(slot.Itemstack).TryGetValue("quickdraw", out int p) == true)
        //             anim += "-quick";
        //         slot.Itemstack.Attributes.SetInt("renderVariant", 1);
        //         byEntity.Attributes.SetInt("aiming", 1);
        //         byEntity.Attributes.SetInt("aimingCancel", 0);
        //         byEntity.AnimManager.StartAnimation(anim);
        //         IPlayer dualCallByPlayer = null;
        //         if (byEntity is EntityPlayer)
        //         {
        //             dualCallByPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
        //         }
        // 
        //         byEntity.World.PlaySoundAt(new AssetLocation("sounds/bow-draw"), byEntity, dualCallByPlayer, randomizePitch: false, 8f);
        //         handling = EnumHandHandling.PreventDefault;
        //     }
        //     return false;
        // }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemBow), nameof(ItemBow.OnHeldInteractStop))]
        public static void OnHeldInteractStop_Postfix(ItemBow __instance, float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (!(byEntity?.Api is ICoreServerAPI sapi)) return;
            // Skip if running Combat Overhaul
            if (sapi.ModLoader.GetModSystem<KRPGEnchantmentSystem>().COSysServer != null) return;
            if (EnchantingConfigLoader.Config?.Debug == true)
                sapi.Logger.Event("[KRPGEnchantment] Firing ItemBow.OnHeldInteractStop postfix");
            // if (byEntity.Api.EnchantAccessor().GetActiveEnchantments(slot?.Itemstack) == null) return;
            // byEntity.WatchedAttributes.SetItemstack("pendingRangedEnchants", slot?.Itemstack);
            Dictionary<string, int> enchants = sapi.EnchantAccessor().GetActiveEnchantments(slot?.Itemstack);
            if (enchants == null) return;
            EnchantModifiers parameters = new EnchantModifiers();
            bool didEnchants = sapi.EnchantAccessor().TryEnchantments(slot, "OnAttackStop", byEntity, byEntity, enchants, ref parameters);
            if (!didEnchants)
                    sapi.Logger.Warning("[KRPGEnchantments] Failed to TryEnchantments on {0}!", byEntity.GetName());
            // Filtering enchant list
            // if (enchants.ContainsKey("accurate")) enchants.Remove("accurate"); // Trying to remove false positives for warnings
            // if (enchants.ContainsKey("quickdraw")) enchants.Remove("quickdraw"); // Trying to remove false positives for warnings
            // if (enchants.ContainsKey("reversion")) enchants.Remove("reversion"); // Trying to remove false positives for warnings
            // if (enchants.ContainsKey("durable")) enchants.Remove("durable"); // Trying to remove false positives for warnings
            // string s = null;
            // foreach (KeyValuePair<string, int> pair in enchants)
            // {
            //     s += pair.Key + ":" + pair.Value + ";";
            // }
            // if (s == null) return;
            string s = slot.Itemstack.Attributes.GetTreeAttribute("enchantments").GetString("active");
            long t = sapi.World.ElapsedMilliseconds;
            // TODO: Move this to the ProjectileStack?
            byEntity.WatchedAttributes.SetString("pendingRangedEnchants", s);
            byEntity.WatchedAttributes.SetLong("pendingRangedEnchantsTimer", t);
            if (EnchantingConfigLoader.Config?.Debug == true)
                sapi.Logger.Event("[KRPGEnchantment] Finished firing ItemBow.OnHeldInteractStop postfix. Found enchants {0} at {1}.", s, t);
        }
        // [HarmonyPostfix]
        // [HarmonyPatch(typeof(ItemBow), "aimAnimation")]
        // public static void aimAnimation_Postfix(ItemBow __instance, ref string __result, ref ICoreAPI ___api)
        // {
        //     __result += "-quick";
        // }
    }
}