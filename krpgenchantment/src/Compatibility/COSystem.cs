using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using CombatOverhaul;
using Vintagestory.API.Datastructures;
using CombatOverhaul.Implementations;
using KRPGLib.Enchantment.API;
using System.Collections;

namespace KRPGLib.Enchantment
{
    public class COSystem
    {
        ICoreServerAPI sApi;
        ICoreAPI Api;

        public void StartServerSide(ICoreAPI api)
        {
            if (!(api is ICoreServerAPI sapi)) return;

            Api = api;
            sApi = sapi;

            CombatOverhaulSystem COSys = sApi.ModLoader.GetModSystem<CombatOverhaulSystem>();
            if (COSys != null)
            {
                COSys.ServerMeleeSystem.OnDealMeleeDamage += OnMeleeDamaged;
                COSys.ServerProjectileSystem.OnDealRangedDamage += OnRangedDamaged;
            }
        }
        public void OnMeleeDamaged(Entity target, DamageSource damageSource, ItemSlot slot, ref float damage)
        {
            Dictionary<string, int> enchants = Api?.EnchantAccessor()?.GetActiveEnchantments(slot?.Itemstack);
            if (enchants == null)
            {
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] COSystem received OnMeleeDamaged event, but could not find Enchantments to try.");
                return;
            }

            float dmg = damage;
            EnchantModifiers parameters = new EnchantModifiers() { { "damage", dmg } };
            bool didEnchantments = sApi.EnchantAccessor().TryEnchantments(slot, "OnAttack", damageSource.CauseEntity, target, enchants, ref parameters);
            if (didEnchantments)
            {
                damage = parameters.GetFloat("damage");
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] Did Enchantments and setting ref damage to {0}.", damage);
            }
            
            if (didEnchantments != false && EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] COSystem finished processing Enchantments.");
            if (!didEnchantments && EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] COSystem failed processing Enchantments.");
        }
        public void OnRangedDamaged(Entity target, DamageSource damageSource, ItemStack weaponStack, ref float damage)
        {
            Dictionary<string, int> enchants = Api?.EnchantAccessor()?.GetActiveEnchantments(weaponStack);
            if (enchants == null)
            {
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] COSystem received OnRangedDamaged event, but could not find Enchantments to try.");
                return;
            }

            float dmg = damage;
            EnchantModifiers parameters = new EnchantModifiers() { { "damage", dmg } };
            bool didEnchantments = sApi.EnchantAccessor().TryEnchantments(weaponStack, "OnAttack", damageSource.CauseEntity, target, enchants, ref parameters);
            if (didEnchantments)
            {
                damage = parameters.GetFloat("damage");
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] Did Enchantments and setting ref damage to {0}.", damage);
            }

            if (didEnchantments != false && EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] COSystem finished processing Enchantments.");
            if (!didEnchantments && EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] COSystem failed processing Enchantments.");
        }
    }
}
