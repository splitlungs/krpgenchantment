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

namespace KRPGLib.Enchantment
{
    [HarmonyPatch]
    public class ItemBow_Patch
    {
        [HarmonyPatch(typeof(ItemBow), "OnHeldInteractStop")]
        public static void Postfix(ItemBow __instance, float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, 
            EntitySelection entitySel)
        {
            if (slot.Empty || byEntity == null || 
                byEntity.Api.EnchantAccessor().GetActiveEnchantments(slot.Itemstack) == null) 
                return;

            byEntity.WatchedAttributes.SetItemstack("pendingRangedEnchants", slot.Itemstack);
            byEntity.WatchedAttributes.SetLong("pendingRangedEnchantsTimer", byEntity.Api.World.ElapsedMilliseconds);
        }
    }
}
