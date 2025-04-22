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
using Vintagestory.API.Datastructures;
using KRPGLib.Wands;

namespace KRPGLib.Enchantment
{
    public class KRPGWandsSystem
    {
        ICoreServerAPI sApi;
        public void StartServerSide(ICoreServerAPI api)
        {
            sApi = api;

            KRPGWandsMod WandsMod = sApi.ModLoader.GetModSystem<KRPGWandsMod>();
            if (WandsMod != null)
            {
                WandsMod.OnDealWandDamage += OnProjectileDamaged;
            }
        }
        public void OnProjectileDamaged(Entity target, DamageSource damageSource, ItemSlot slot, ref float damage)
        {
            if (!target.HasBehavior<EnchantmentEntityBehavior>())
                return;

            EnchantmentEntityBehavior eeb = target.GetBehavior<EnchantmentEntityBehavior>();
            eeb.TryEnchantments(damageSource.CauseEntity as EntityAgent, slot.Itemstack);

            // Manual Healing check to overwrite damage
            Dictionary<string, int> enchants = sApi.GetEnchantments(slot.Itemstack);
            if (enchants == null)
                return;
            if (enchants.ContainsKey("healing"))
                damage = 0;
        }
    }
}
