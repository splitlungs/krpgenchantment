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
        /// <summary>
        /// Delegate for wand hit target event, WandsMod.OnDealWandDamage.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="damageSource"></param>
        /// <param name="slot"></param>
        /// <param name="damage"></param>
        public void OnProjectileDamaged(Entity target, DamageSource damageSource, ItemSlot slot, ref float damage)
        {
            Dictionary<string, int> enchants = Api?.EnchantAccessor()?.GetActiveEnchantments(slot?.Itemstack);
            if (enchants == null)
            {
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] KRPGWands received OnProjectileDamaged event, but could not find Enchantments to try.");
                return;
            }

            EnchantModifiers parameters = new EnchantModifiers() { { "damage", damage } };
            bool didEnchantments = sApi.EnchantAccessor().TryEnchantments(slot, "OnAttack", damageSource.CauseEntity, target, enchants, ref parameters);
            if (didEnchantments)
            {
                damage = parameters.GetFloat("damage");
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] Did Enchantments and setting ref damage to {0}.", damage);
            }
        }
    }
}
