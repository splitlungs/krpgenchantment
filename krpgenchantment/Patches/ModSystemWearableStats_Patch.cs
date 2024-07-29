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
        public static void Postfix (ModSystemWearableStats __instance, ref float __result, IPlayer player, float damage, DamageSource dmgSource)
        {
            IInventory inv = player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName);

            int power = 0;
            float fPower = 0f;
            string eType = "";

            switch (dmgSource.Type)
            {
                case (EnumDamageType.BluntAttack):
                    eType = EnumEnchantments.protection.ToString();
                    power += inv[12].Itemstack.Attributes.GetInt(eType, 0) + inv[13].Itemstack.Attributes.GetInt(eType, 0) + inv[14].Itemstack.Attributes.GetInt(eType, 0);
                    fPower = power * 0.1f;
                    break;
                case (EnumDamageType.Electricity):
                    eType = EnumEnchantments.resistelectric.ToString();
                    power += inv[12].Itemstack.Attributes.GetInt(eType, 0) + inv[13].Itemstack.Attributes.GetInt(eType, 0) + inv[14].Itemstack.Attributes.GetInt(eType, 0);
                    fPower = power * 0.1f;
                    break;
                case (EnumDamageType.Fire):
                    eType = EnumEnchantments.resistfire.ToString();
                    power += inv[12].Itemstack.Attributes.GetInt(eType, 0) + inv[13].Itemstack.Attributes.GetInt(eType, 0) + inv[14].Itemstack.Attributes.GetInt(eType, 0);
                    fPower = power * 0.1f;
                    break;
                case (EnumDamageType.Frost):
                    eType = EnumEnchantments.resistfrost.ToString();
                    power += inv[12].Itemstack.Attributes.GetInt(eType, 0) + inv[13].Itemstack.Attributes.GetInt(eType, 0) + inv[14].Itemstack.Attributes.GetInt(eType, 0);
                    fPower = power * 0.1f;
                    break;
                case (EnumDamageType.Heal):
                    eType = EnumEnchantments.resistheal.ToString();
                    power += inv[12].Itemstack.Attributes.GetInt(eType, 0) + inv[13].Itemstack.Attributes.GetInt(eType, 0) + inv[14].Itemstack.Attributes.GetInt(eType, 0);
                    fPower = power * 0.1f;
                    break;
                case (EnumDamageType.Injury):
                    eType = EnumEnchantments.resistinjury.ToString();
                    power += inv[12].Itemstack.Attributes.GetInt(eType, 0) + inv[13].Itemstack.Attributes.GetInt(eType, 0) + inv[14].Itemstack.Attributes.GetInt(eType, 0);
                    fPower = power * 0.1f;
                    break;
                case (EnumDamageType.PiercingAttack):
                    eType = EnumEnchantments.protection.ToString();
                    power += inv[12].Itemstack.Attributes.GetInt(eType, 0) + inv[13].Itemstack.Attributes.GetInt(eType, 0) + inv[14].Itemstack.Attributes.GetInt(eType, 0);
                    fPower = power * 0.1f;
                    break;
                case (EnumDamageType.Poison):
                    eType = EnumEnchantments.resistpoison.ToString();
                    power += inv[12].Itemstack.Attributes.GetInt(eType, 0) + inv[13].Itemstack.Attributes.GetInt(eType, 0) + inv[14].Itemstack.Attributes.GetInt(eType, 0);
                    fPower = power * 0.1f;
                    break;
                case (EnumDamageType.SlashingAttack):
                    eType = EnumEnchantments.protection.ToString();
                    power += inv[12].Itemstack.Attributes.GetInt(eType, 0) + inv[13].Itemstack.Attributes.GetInt(eType, 0) + inv[14].Itemstack.Attributes.GetInt(eType, 0);
                    fPower = power * 0.1f;
                    break;
            }

            __result -= fPower;
        }
    }
}
