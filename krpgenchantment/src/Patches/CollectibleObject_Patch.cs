using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.API.Datastructures;
using static System.Net.Mime.MediaTypeNames;
using System.Reflection;

namespace KRPGLib.Enchantment
{
    [HarmonyPatch(typeof(CollectibleObject))]
    public class CollectibleObject_Patch
    {
        // [HarmonyPostfix]
        // [HarmonyPatch("GetMiningSpeed")]
        // public static void Postfix(ref float __result, IItemStack itemstack, ICoreAPI ___api)
        // {
        //     if (!(___api is ICoreServerAPI sApi)) return;
        //     
        //     if (EnchantingConfigLoader.Config?.Debug == true)
        //         sApi.Logger.Event("[KRPGEnchantment] Applying an Efficient enchantment. Pre MiningSpeedMul is {0}.", __result);
        //     Dictionary<string, int> enchants = sApi.EnchantAccessor().GetActiveEnchantments((ItemStack)itemstack);
        //     if (enchants?.TryGetValue("efficient", out int power) == true)
        //     {
        //         KRPGLib.Enchantment.API.IEnchantment ench = sApi.EnchantAccessor().GetEnchantment("efficient");
        //         if (ench?.Modifiers == null) return;
        //         float eMul = ench.Modifiers.GetFloat("PowerMultiplier");
        //         float mSpeed = eMul * power;
        //         __result += mSpeed;
        //     }
        //     if (EnchantingConfigLoader.Config?.Debug == true)
        //         sApi.Logger.Event("[KRPGEnchantment] Applied an Efficient enchantment. Post MiningSpeedMul is {0}.", __result);
        // }
        // OBSOLETE - Now triggered through "EnchantmentEntityBehavior.OnEntityReceiveDamage" override
        // [HarmonyPatch(typeof(CollectibleObject), "DamageItem")]
        // public static bool Prefix(CollectibleObject __instance, IWorldAccessor world, Entity byEntity, ItemSlot itemslot, ref int amount)
        // {
        //     if (world.Side != EnumAppSide.Server) return true;
        //     ICoreServerAPI sApi = world.Api as ICoreServerAPI;
        //     Dictionary<string, int> enchants = byEntity.Api.EnchantAccessor().GetActiveEnchantments(itemslot.Itemstack);
        //     if (enchants == null)
        //         return true;
        //     int durable = enchants.GetValueOrDefault("durable", 0);
        //     if (durable > 0)
        //     {
        //         EnchantmentSource enchant = new EnchantmentSource() {
        //             SourceStack = itemslot.Itemstack,
        //             TargetEntity = byEntity,
        //             Trigger = "OnDurability",
        //             Code = "durable",
        //             Power = durable };
        //         int dmg = amount;
        //         EnchantModifiers parameters = new EnchantModifiers() { { "damage", dmg } };
        //         bool didEnchantment = sApi.EnchantAccessor().TryEnchantment(enchant, ref parameters);
        //         if (didEnchantment == true)
        //         {
        //             amount = parameters.GetInt("damage");
        //             return false; // Only skip if we 
        //         }
        //     }
        //     return true;
        // }
    }
}
