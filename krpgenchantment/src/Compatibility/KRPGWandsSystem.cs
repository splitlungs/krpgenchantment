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
using Vintagestory.Common;

namespace KRPGLib.Enchantment
{
    public class KRPGWandsSystem
    {
        ICoreAPI Api;
        ICoreServerAPI sApi;
        public void StartServerSide(ICoreAPI api)
        {
            if (!(api is ICoreServerAPI sapi)) return;

            Api = api;
            sApi = sapi;

            KRPGWandsMod WandsMod = sapi.ModLoader.GetModSystem<KRPGWandsMod>();
            if (WandsMod != null)
            {
                WandsMod.OnDealWandDamage += OnProjectileDamaged;
            }
        }
        public void OnProjectileDamaged(Entity target, DamageSource damageSource, ItemSlot slot, ref float damage)
        {
            if (sApi?.EnchantAccessor()?.GetActiveEnchantments(slot?.Itemstack) == null) return;

            EnchantModifiers parameters = new EnchantModifiers() { { "damage", damage } };
            bool didEnchants = Api.EnchantAccessor().TryEnchantments(slot, "OnAttack", damageSource.CauseEntity, target, ref parameters);
            damage = parameters.GetFloat("damage");
        }
    }
}
