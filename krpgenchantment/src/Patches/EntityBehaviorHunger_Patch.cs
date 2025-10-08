// Probably won't hook into this if I don't have to, but it's here just in case

// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Text;
// using System.Threading.Tasks;
// using HarmonyLib;
// using Vintagestory.API.Common.Entities;
// using Vintagestory.API.Client;
// using Vintagestory.API.Config;
// using Vintagestory.API.Common;
// using Vintagestory.API.Server;
// using Vintagestory.API.MathTools;
// using Vintagestory.GameContent;
// using Vintagestory.API.Datastructures;
// using static System.Net.Mime.MediaTypeNames;
// using System.Reflection;
// using System.Data;
// 
// namespace KRPGLib.Enchantment
// {
//     [HarmonyPatch]
//     public class EntityBehaviorHunger_Patch
//     {
//         [HarmonyReversePatch]
//         [HarmonyPatch(typeof(EntityBehaviorHunger), nameof(EntityBehaviorHunger.ConsumeSaturation))]
//         public static bool Prefix(EntityBehaviorHunger __instance, float amount)
//         {
//             EnchantmentEntityBehavior eeb = __instance.entity.GetBehavior<EnchantmentEntityBehavior>();
//             if (eeb != null)
//             {
//                 // Push OnHit to each cached Gear enchants
//                 foreach (KeyValuePair<int, ActiveEnchantCache> pair in eeb.GearEnchantCache)
//                 {
//                     if (pair.Value.Enchantments == null) continue;
// 
//                     EnchantModifiers parameters = new EnchantModifiers() { { "damage", amount }, { "type", dmgType } };
//                     bool didEnchants =
//                         eeb.sApi.EnchantAccessor().TryEnchantments(eeb.gearInventory[pair.Key]?.Itemstack, "OnHit", damageSource.CauseEntity, entity, pair.Value.Enchantments, ref parameters);
//                     if (didEnchants)
//                     {
//                         amount = parameters.GetFloat("damage");
//                     }
//                 }
//             }
// 
//             return true;
//         }
//     }
// }