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
            if (api.Side != EnumAppSide.Server) return;

            Api = api;
            sApi = (ICoreServerAPI)api;


            KRPGWandsMod WandsMod = sApi.ModLoader.GetModSystem<KRPGWandsMod>();
            if (WandsMod != null)
            {
                WandsMod.OnDealWandDamage += OnProjectileDamaged;
            }
        }
        public void OnProjectileDamaged(Entity target, DamageSource damageSource, ItemSlot slot, ref float damage)
        {
            if (Api.GetEnchantments(slot.Itemstack) == null) return;

            Dictionary<string, object> parameters = new Dictionary<string, object>() { { "damage", damage } };
            bool didEnchants = Api.TryEnchantments(slot, "OnAttack", damageSource.CauseEntity, target, ref parameters);
            damage = (float)parameters["damage"];

            // if (!target.HasBehavior<EnchantmentEntityBehavior>())
            //     return;
            // 
            // EnchantmentEntityBehavior eeb = target.GetBehavior<EnchantmentEntityBehavior>();
            // eeb.TryEnchantments(damageSource.CauseEntity as EntityAgent, slot.Itemstack);

            // Manual Healing check to overwrite damage
            // Dictionary<string, int> enchants = sApi.EnchantAccessor().GetEnchantments(slot.Itemstack);
            // if (enchants != null)
            // {
            //     if (enchants.ContainsKey("healing"))
            //         damage = 0;
            // 
            //     foreach (KeyValuePair<string, int> keyValuePair in enchants) {
            //         float amountf = 1f;
            //         EnchantmentSource enchant = new EnchantmentSource() { Trigger = "OnAttack", Code = keyValuePair.Key, Power = keyValuePair.Value };
            //         object[] parameters = new object[1] { amountf };
            //         bool didEnchantment = sApi.EnchantAccessor().DoEnchantment(enchant, slot, ref parameters);
            //         if (didEnchantment != true)
            //             return;
            //     }
            // }
        }
    }
}
