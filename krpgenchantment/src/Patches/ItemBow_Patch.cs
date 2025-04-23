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

namespace KRPGLib.Enchantment
{
    [HarmonyPatch]
    public class ItemBow_Patch
    {
        [HarmonyPatch(typeof(ItemBow), "OnHeldInteractStop")]
        public static void Postfix(ItemBow __instance, float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            Dictionary<string, int> enchants = byEntity.Api.EnchantAccessor().GetEnchantments(slot.Itemstack);
            if (enchants == null)
            {
                // byEntity.Api.Logger.Event("A bow was fired, but no enchantments were on it.");
                return;
            }
    
            string enchantString = byEntity.Api.World.Calendar.ElapsedSeconds.ToString() + ";";
            foreach (KeyValuePair<string, int> keyValuePair in enchants)
            {
                enchantString += keyValuePair.Key + ";" + keyValuePair.Value.ToString() + ";";
            }
            byEntity.WatchedAttributes.SetString("pendingRangedEnchants", enchantString);
    
            // byEntity.Api.Logger.Event("pendingRangedEnchants is {0}", enchantString);
    
        }
    }
}
