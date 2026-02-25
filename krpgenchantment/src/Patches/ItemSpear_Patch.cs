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
    /*
    [HarmonyPatch]
    public class ItemSpear_Patch
    {
        [HarmonyPatch(typeof(ItemSpear), "OnHeldInteractStop")]
        public static void Postfix(ItemSpear __instance, float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (!(byEntity?.Api is ICoreServerAPI sapi)) return;
            if (sapi.EnchantAccessor().GetActiveEnchantments(slot?.Itemstack) == null) return;
            EnchantModifiers parameters = new EnchantModifiers();
            bool didEnchantments = sapi.EnchantAccessor().TryEnchantments(slot, "OnAttackStop", byEntity, entitySel?.Entity ?? null, ref parameters);
        }
    }
    */
}
