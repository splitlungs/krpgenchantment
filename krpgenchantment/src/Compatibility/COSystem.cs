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
using CombatOverhaul.MeleeSystems;
using CombatOverhaul.RangedSystems;
using Vintagestory.API.Client;

namespace KRPGLib.Enchantment.Compat
{
    public class COSystem
    {
        ICoreAPI Api;
        ICoreClientAPI cApi;
        ICoreServerAPI sApi;
        public void StartClientSide(ICoreAPI api)
        {
            Api = api;
            if (!(api is ICoreClientAPI capi)) return;
            cApi = capi;
            
            CombatOverhaulSystem COSys = capi.ModLoader.GetModSystem<CombatOverhaulSystem>();
            if (COSys != null)
            {
                COSys.ClientMeleeSystem.OnMeleeAttackStatusChange += OnMeleeStatusChange;
                COSys.ClientRangedWeaponSystem.RangedWeaponStatusChanged += OnRangedStatusChange;
            }
        }
        public void StartServerSide(ICoreAPI api)
        {
            Api = api;
            if (!(api is ICoreServerAPI sapi)) return;
            sApi = sapi;

            CombatOverhaulSystem COSys = sapi.ModLoader.GetModSystem<CombatOverhaulSystem>();
            if (COSys != null)
            {
                COSys.ServerMeleeSystem.OnDealMeleeDamage += OnMeleeDamaged;
                COSys.ServerMeleeSystem.OnMeleeAttackStatusChange += OnMeleeStatusChange;
                COSys.ServerProjectileSystem.OnDealRangedDamage += OnRangedDamaged;
                COSys.ServerRangedWeaponSystem.RangedWeaponStatusChanged += OnRangedStatusChange;
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
            bool didEnchantments = sApi.EnchantAccessor().TryEnchantments(slot, "OnAttackStop", damageSource.CauseEntity, target, enchants, ref parameters);
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
        public void OnMeleeStatusChange(Entity attacker, ItemSlot slot, MeleeAttackStatus status)
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] COSystem detected a MeleeAttackStatus change to {0}.", status.ToString());
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
            bool didEnchantments = sApi.EnchantAccessor().TryEnchantments(weaponStack, "OnAttackStop", damageSource.CauseEntity, target, enchants, ref parameters);
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
        public void OnRangedStatusChange(Entity attacker, ItemSlot weaponSlot, RangedWeaponStatus status)
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] COSystem detected a RangedWeaponStatus change to {0}.", status.ToString());
        }
    }
}
