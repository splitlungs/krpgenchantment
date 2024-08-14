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

namespace KRPGLib.Enchantment
{
    [HarmonyPatch]
    public class ModSystemWearableStats_Patch
    {
        [HarmonyPatch(typeof(ModSystemWearableStats), "handleDamaged")]
        public static void Postfix(ModSystemWearableStats __instance, ref float __result, IPlayer player, float damage, DamageSource dmgSource)
        {
            IInventory inv = player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName);

            int power = 0;
            string eType;

            if (dmgSource.Type == EnumDamageType.BluntAttack)
            {
                eType = EnumEnchantments.protection.ToString();
                if (!inv[12].Empty)
                    power += inv[12].Itemstack.Attributes.GetInt(eType, 0);
                if (!inv[13].Empty)
                    power += inv[13].Itemstack.Attributes.GetInt(eType, 0);
                if (!inv[14].Empty)
                    power += inv[14].Itemstack.Attributes.GetInt(eType, 0);
            }
            else if (dmgSource.Type == EnumDamageType.Electricity)
            {
                eType = EnumEnchantments.resistelectric.ToString();
                if (!inv[12].Empty)
                    power += inv[12].Itemstack.Attributes.GetInt(eType, 0);
                if (!inv[13].Empty)
                    power += inv[13].Itemstack.Attributes.GetInt(eType, 0);
                if (!inv[14].Empty)
                    power += inv[14].Itemstack.Attributes.GetInt(eType, 0);
            }
            else if (dmgSource.Type == EnumDamageType.Fire)
            {
                eType = EnumEnchantments.resistfire.ToString();
                if (!inv[12].Empty)
                    power += inv[12].Itemstack.Attributes.GetInt(eType, 0);
                if (!inv[13].Empty)
                    power += inv[13].Itemstack.Attributes.GetInt(eType, 0);
                if (!inv[14].Empty)
                    power += inv[14].Itemstack.Attributes.GetInt(eType, 0);
            }
            else if (dmgSource.Type == EnumDamageType.Frost)
            {
                eType = EnumEnchantments.resistfrost.ToString();
                if (!inv[12].Empty)
                    power += inv[12].Itemstack.Attributes.GetInt(eType, 0);
                if (!inv[13].Empty)
                    power += inv[13].Itemstack.Attributes.GetInt(eType, 0);
                if (!inv[14].Empty)
                    power += inv[14].Itemstack.Attributes.GetInt(eType, 0);
            }
            else if (dmgSource.Type == EnumDamageType.Heal)
            {
                eType = EnumEnchantments.resistheal.ToString();
                if (!inv[12].Empty)
                    power += inv[12].Itemstack.Attributes.GetInt(eType, 0);
                if (!inv[13].Empty)
                    power += inv[13].Itemstack.Attributes.GetInt(eType, 0);
                if (!inv[14].Empty)
                    power += inv[14].Itemstack.Attributes.GetInt(eType, 0);
            }
            else if (dmgSource.Type == EnumDamageType.Injury)
            {
                eType = EnumEnchantments.resistinjury.ToString();
                if (!inv[12].Empty)
                    power += inv[12].Itemstack.Attributes.GetInt(eType, 0);
                if (!inv[13].Empty)
                    power += inv[13].Itemstack.Attributes.GetInt(eType, 0);
                if (!inv[14].Empty)
                    power += inv[14].Itemstack.Attributes.GetInt(eType, 0);
            }
            else if (dmgSource.Type == EnumDamageType.PiercingAttack)
            {
                eType = EnumEnchantments.protection.ToString();
                if (!inv[12].Empty)
                    power += inv[12].Itemstack.Attributes.GetInt(eType, 0);
                if (!inv[13].Empty)
                    power += inv[13].Itemstack.Attributes.GetInt(eType, 0);
                if (!inv[14].Empty)
                    power += inv[14].Itemstack.Attributes.GetInt(eType, 0);
            }
            else if (dmgSource.Type == EnumDamageType.Poison)
            {
                eType = EnumEnchantments.resistpoison.ToString();
                if (!inv[12].Empty)
                    power += inv[12].Itemstack.Attributes.GetInt(eType, 0);
                if (!inv[13].Empty)
                    power += inv[13].Itemstack.Attributes.GetInt(eType, 0);
                if (!inv[14].Empty)
                    power += inv[14].Itemstack.Attributes.GetInt(eType, 0);
            }
            else if (dmgSource.Type == EnumDamageType.SlashingAttack)
            {
                eType = EnumEnchantments.protection.ToString();
                if (!inv[12].Empty)
                    power += inv[12].Itemstack.Attributes.GetInt(eType, 0);
                if (!inv[13].Empty)
                    power += inv[13].Itemstack.Attributes.GetInt(eType, 0);
                if (!inv[14].Empty)
                    power += inv[14].Itemstack.Attributes.GetInt(eType, 0);
            }

            if (power > 0)
            {
                float fPower = power * 0.1f;
                __result -= fPower;
            }
        }
    }
}