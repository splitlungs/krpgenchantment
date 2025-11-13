using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Client;
using Vintagestory.GameContent;
using Vintagestory.API.Datastructures;

namespace KRPGLib.Enchantment
{
    // OBSOLETE
    // 
    // [HarmonyPatch]
    // public class ModSystemWearableStats_Patch
    // {
    //     [HarmonyPatch(typeof(ModSystemWearableStats), "handleDamaged")]
    //     public static void Postfix(ModSystemWearableStats __instance, ref float __result, IPlayer player, float damage, DamageSource dmgSource)
    //     {
    //         IInventory inv = player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName);
    // 
    //         int power = 0;
    //         string eType = null;
    // 
    //         if (dmgSource.Type == EnumDamageType.BluntAttack)
    //         {
    //             eType = EnumEnchantments.protection.ToString();
    //         }
    //         else if (dmgSource.Type == EnumDamageType.Electricity)
    //         {
    //             eType = EnumEnchantments.resistelectricity.ToString();
    //         }
    //         else if (dmgSource.Type == EnumDamageType.Fire)
    //         {
    //             eType = EnumEnchantments.resistfire.ToString();
    //         }
    //         else if (dmgSource.Type == EnumDamageType.Frost)
    //         {
    //             eType = EnumEnchantments.resistfrost.ToString();
    //         }
    //         else if (dmgSource.Type == EnumDamageType.Heal)
    //         {
    //             eType = EnumEnchantments.resistheal.ToString();
    //         }
    //         else if (dmgSource.Type == EnumDamageType.Injury)
    //         {
    //             eType = EnumEnchantments.resistinjury.ToString();
    //         }
    //         else if (dmgSource.Type == EnumDamageType.PiercingAttack)
    //         {
    //             eType = EnumEnchantments.protection.ToString();
    //         }
    //         else if (dmgSource.Type == EnumDamageType.Poison)
    //         {
    //             eType = EnumEnchantments.resistpoison.ToString();
    //         }
    //         else if (dmgSource.Type == EnumDamageType.SlashingAttack)
    //         {
    //             eType = EnumEnchantments.protection.ToString();
    //         }
    // 
    //         if (eType != null)
    //         {
    //             if (!inv[12].Empty)
    //             {
    //                 ITreeAttribute tree = inv[12].Itemstack.Attributes.GetOrAddTreeAttribute("enchantments");
    //                 power += tree.GetInt(eType, 0);
    //             }
    //             if (!inv[13].Empty)
    //             {
    //                 ITreeAttribute tree = inv[13].Itemstack.Attributes.GetOrAddTreeAttribute("enchantments");
    //                 power += tree.GetInt(eType, 0);
    //             }
    //             if (!inv[14].Empty)
    //             {
    //                 ITreeAttribute tree = inv[14].Itemstack.Attributes.GetOrAddTreeAttribute("enchantments");
    //                 power += tree.GetInt(eType, 0);
    //             }
    //         }
    // 
    //         if (power > 0)
    //         {
    //             float fPower = power * 0.1f;
    //             __result -= fPower;
    //         }
    //     }
    // }
}