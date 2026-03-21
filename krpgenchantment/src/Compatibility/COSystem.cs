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
        public void StartClientSide(ICoreClientAPI capi)
        {
            if (!(capi is ICoreClientAPI)) return;
            cApi = capi;
            Api = capi as ICoreAPI;
            
            CombatOverhaulSystem COSys = capi.ModLoader.GetModSystem<CombatOverhaulSystem>();
            if (COSys != null)
            {
                COSys.ClientMeleeSystem.OnMeleeAttackStatusChange += OnMeleeStatusChange;
                COSys.ClientRangedWeaponSystem.RangedWeaponStatusChanged += OnRangedStatusChange;
            }
        }
        public void StartServerSide(ICoreServerAPI sapi)
        {
            if (!(sapi is ICoreServerAPI)) return;
            sApi = sapi;
            Api = sapi as ICoreAPI;
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
            Dictionary<string, int> enchants = sApi?.EnchantAccessor()?.GetActiveEnchantments(slot?.Itemstack);
            if (enchants == null)
            {
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] COSystem received OnMeleeDamaged event, but could not find Enchantments to try.");
                return;
            }

            float dmg = damage;
            EnchantModifiers parameters = new EnchantModifiers() { { "damage", dmg } };
            bool didEnchantments = sApi.EnchantAccessor().TryEnchantments(slot, "OnAttacked", damageSource.CauseEntity, target, enchants, ref parameters);
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
            Dictionary<string, int> enchants = sApi?.EnchantAccessor()?.GetActiveEnchantments(weaponStack);
            if (enchants == null)
            {
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] COSystem received OnRangedDamaged event, but could not find Enchantments to try.");
                return;
            }

            float dmg = damage;
            EnchantModifiers parameters = new EnchantModifiers() { { "damage", dmg } };
            bool didEnchantments = sApi.EnchantAccessor().TryEnchantments(weaponStack, "OnAttacked", damageSource.CauseEntity, target, enchants, ref parameters);
            if (didEnchantments)
            {
                damage = parameters.GetFloat("damage");
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] Did Enchantments and setting ref damage to {0}.", damage);
            }

            // if (didEnchantments != false && EnchantingConfigLoader.Config?.Debug == true)
            //     Api.Logger.Event("[KRPGEnchantment] COSystem finished processing Enchantments.");
            // if (!didEnchantments && EnchantingConfigLoader.Config?.Debug == true)
            //     Api.Logger.Event("[KRPGEnchantment] COSystem failed processing Enchantments.");
        }
        public void OnRangedStatusChange(Entity attacker, ItemSlot weaponSlot, RangedWeaponStatus status)
        {
            // if (EnchantingConfigLoader.Config?.Debug == true)
            //     Api.Logger.Event("[KRPGEnchantment] COSystem detected a RangedWeaponStatus change to {0}.", status.ToString());
            // Servers only plz
            if (!(Api is ICoreServerAPI sapi)) return;
            Dictionary<string, int> enchants = sapi.EnchantAccessor().GetActiveEnchantments(weaponSlot?.Itemstack);
            if (enchants == null) return;
            // TODO: Testing for passing ItemStacks as EnchantModifiers
            // EnchantModifiers parameters = new EnchantModifiers() { {"WeaponStack", weaponSlot.Itemstack} };
            EnchantModifiers parameters = new EnchantModifiers() { {"RangedWeaponStatus", (int)status} };
            bool didEnchants = false;
            switch (status)
            {
                case RangedWeaponStatus.StartLoading:
                {
                    didEnchants = sapi.EnchantAccessor().TryEnchantments(weaponSlot, "OnAttackStart", attacker, attacker, enchants, ref parameters);
                    if (didEnchants == true && EnchantingConfigLoader.Config?.Debug == true)
                        Api.Logger.Event("[KRPGEnchantment] COSystem successfully processed RangedWeaponStatus: {0} Enchantment trigger.", status.ToString());
                    break;
                }
                case RangedWeaponStatus.EndLoading:
                {
                    didEnchants = sapi.EnchantAccessor().TryEnchantments(weaponSlot, "OnAttackStop", attacker, attacker, enchants, ref parameters);
                    if (didEnchants == true && EnchantingConfigLoader.Config?.Debug == true)
                        Api.Logger.Event("[KRPGEnchantment] COSystem successfully processed RangedWeaponStatus: {0} Enchantment trigger.", status.ToString());
                    break;
                }
                case RangedWeaponStatus.StartAiming:
                {
                    didEnchants = sapi.EnchantAccessor().TryEnchantments(weaponSlot, "OnAttackStart", attacker, attacker, enchants, ref parameters);
                    if (didEnchants == true && EnchantingConfigLoader.Config?.Debug == true)
                        Api.Logger.Event("[KRPGEnchantment] COSystem successfully processed RangedWeaponStatus: {0} Enchantment trigger.", status.ToString());
                    break;
                }
                
                case RangedWeaponStatus.EndAiming:
                {
                    didEnchants = sapi.EnchantAccessor().TryEnchantments(weaponSlot, "OnAttackStop", attacker, attacker, enchants, ref parameters);
                    if (didEnchants == true && EnchantingConfigLoader.Config?.Debug == true)
                        Api.Logger.Event("[KRPGEnchantment] COSystem successfully processed RangedWeaponStatus: {0} Enchantment trigger.", status.ToString());
                    break;
                }
                case RangedWeaponStatus.TriggeredShot:
                {
                    didEnchants = sapi.EnchantAccessor().TryEnchantments(weaponSlot, "OnAttackStep", attacker, attacker, enchants, ref parameters);
                    if (didEnchants == true && EnchantingConfigLoader.Config?.Debug == true)
                        Api.Logger.Event("[KRPGEnchantment] COSystem successfully processed RangedWeaponStatus: {0} Enchantment trigger.", status.ToString());
                    break;
                }
                case RangedWeaponStatus.SpawnedProjectile:
                {
                    didEnchants = sapi.EnchantAccessor().TryEnchantments(weaponSlot, "OnAttackStep", attacker, attacker, enchants, ref parameters);
                    if (didEnchants == true && EnchantingConfigLoader.Config?.Debug == true)
                        Api.Logger.Event("[KRPGEnchantment] COSystem successfully processed RangedWeaponStatus: {0} Enchantment trigger.", status.ToString());
                    break;
                }
                default:
                    break;
            }
        }
    }
}
