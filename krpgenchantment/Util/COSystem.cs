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

namespace KRPGLib.Enchantment
{
    public class COSystem
    {
        ICoreServerAPI sApi;
        public void StartServerSide(ICoreServerAPI api)
        {
            sApi = api;

            CombatOverhaulSystem COSys = sApi.ModLoader.GetModSystem<CombatOverhaulSystem>();
            if (COSys != null)
            {
                COSys.ServerMeleeSystem.OnDealMeleeDamage += OnMeleeDamaged;
                COSys.ServerProjectileSystem.OnDealRangedDamage += OnRangedDamaged;
            }
        }
        public void OnMeleeDamaged(Entity target, DamageSource damageSource, ItemSlot slot, ref float damage)
        {
            if (!target.HasBehavior<EnchantmentEntityBehavior>())
                return;

            EnchantmentEntityBehavior eeb = target.GetBehavior<EnchantmentEntityBehavior>();
            eeb.TryEnchantments(damageSource.SourceEntity as EntityAgent, slot.Itemstack);
        }
        public void OnRangedDamaged(Entity target, DamageSource damageSource, ItemStack? weaponStack, ref float damage)
        {
            if (!target.HasBehavior<EnchantmentEntityBehavior>())
                return;

            EnchantmentEntityBehavior eeb = target.GetBehavior<EnchantmentEntityBehavior>();
            eeb.TryEnchantments(damageSource.SourceEntity as EntityAgent, weaponStack);
        }
    }
}
