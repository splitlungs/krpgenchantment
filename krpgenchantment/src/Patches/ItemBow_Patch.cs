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
        [HarmonyPatch(typeof(ItemBow), nameof(ItemBow.OnHeldInteractStop))]
        public static void Postfix(ItemBow __instance, float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
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
    }
}
