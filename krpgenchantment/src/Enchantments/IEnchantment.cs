using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Common;

namespace KRPGLib.Enchantment
{
    public interface IEnchantment
    {
        bool Enabled();
        string Code();
        string Name();
        string Description();
        int MaxTier();
        float Multiplier();
        void OnAttack(EnchantmentSource enchant, ItemSlot slot, ref float damage);
        void OnHit(EnchantmentSource enchant, ItemSlot slot, ref float damage);
        void OnToggle(EnchantmentSource enchant, ItemSlot slot);
        void OnStart(EnchantmentSource enchant, ItemSlot slot);
        void OnEnd(EnchantmentSource enchant, ItemSlot slot);
    }
}
