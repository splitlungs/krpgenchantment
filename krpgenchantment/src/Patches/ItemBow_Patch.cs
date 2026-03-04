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
            // if (!(byEntity?.Api is ICoreServerAPI sapi)) return;
            // if (sapi.ModLoader.GetModSystem<KRPGEnchantmentSystem>().combatOverhaul != null) return;
            byEntity.World.Api.Logger.Event("[KRPGEnchantment] Firing ItemBow.OnHeldInteractStop postfix");
            // if (byEntity.Api.EnchantAccessor().GetActiveEnchantments(slot?.Itemstack) == null) return;
            // byEntity.WatchedAttributes.SetItemstack("pendingRangedEnchants", slot.Itemstack.Clone());
            string s = slot?.Itemstack?.Attributes?.GetTreeAttribute("enchantments")?.GetString("active", null);
            byEntity.WatchedAttributes.SetString("pendingRangedEnchants", s);
            byEntity.WatchedAttributes.SetLong("pendingRangedEnchantsTimer", byEntity.World.ElapsedMilliseconds);
            byEntity.World.Api.Logger.Event("[KRPGEnchantment] Finished firing ItemBow.OnHeldInteractStop postfix");
        }
    }
}
