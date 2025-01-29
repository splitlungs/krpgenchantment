using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using KRPGLib.Enchantment;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace CombatOverhaul
{
    public sealed class CombatOverhaulMeleeCompat : ModSystem
    {
        ICoreServerAPI sApi;
        // CombatOverhaulSystem COSys;
        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            sApi = api;

            if (EnchantingConfigLoader.Config.CustomPatches.GetValueOrDefault("CombatOverhaul") != true)
                return;

            CombatOverhaulSystem COSys = sApi.ModLoader.GetModSystem<CombatOverhaulSystem>();
            if (COSys != null)
                COSys.ServerMeleeSystem.OnDealMeleeDamage += OnMeleeDamaged;
            // COSys.ServerProjectileSystem.
        }

        public void OnMeleeDamaged(Entity target, DamageSource damageSource, ItemSlot slot, ref float damage)
        {
            if (!target.HasBehavior<EnchantmentEntityBehavior>())
                return;

            EnchantmentEntityBehavior eeb = target.GetBehavior<EnchantmentEntityBehavior>();
            Dictionary<string, int> enchants = sApi.GetEnchantments(slot);

            // Should avoid default during healing
            // if (enchants.ContainsKey(EnumEnchantments.healing.ToString()))
            //     handled = EnumHandling.PreventDefault;
            // else
            //     handled = EnumHandling.Handled;

            eeb.TryEnchantments(damageSource.SourceEntity as EntityAgent, slot.Itemstack, enchants);
        }

        public void OnRangedDamaged()
        {

        }
    }
}
