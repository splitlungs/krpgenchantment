using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Common;
using KRPGLib.Enchantment;
using System.Text.Json.Nodes;

namespace KRPGLib.API
{
    public interface IEnchantment
    {
        ICoreAPI Api { get; set; }
        bool Enabled { get; set; }
        string Code { get; set; }
        string ClassName { get; set; }
        string LoreCode { get; set; }
        int LoreChapterID { get; set; }
        int MaxTier { get; set; }
        object[] Modifiers { get; set; }
        void OnTrigger(EnchantmentSource enchant, ItemSlot slot, object[] parameters);
        void OnAttack(EnchantmentSource enchant, ItemSlot slot, ref float? damage);
        void OnHit(EnchantmentSource enchant, ItemSlot slot, ref float? damage);
        void OnToggle(EnchantmentSource enchant, ItemSlot slot);
        void OnStart(EnchantmentSource enchant, ItemSlot slot);
        void OnEnd(EnchantmentSource enchant, ItemSlot slot);
    }
}
