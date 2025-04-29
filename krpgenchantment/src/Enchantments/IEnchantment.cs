using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Common;
using KRPGLib.Enchantment;
using System.Text.Json.Nodes;
using Vintagestory.GameContent;

namespace KRPGLib.Enchantment.API
{
    public interface IEnchantment
    {
        ICoreAPI Api { get; set; }
        // Toggles processing of this enchantment
        bool Enabled { get; set; }
        // How the Enchantment is referenced in code
        string Code { get; set; }
        // Used to sort the configs currently
        string Category { get; set; }
        // Define which registered class to instantiate with
        string ClassName { get; set; }
        // The code used to lookup the enchantment's Lore in the lang file
        string LoreCode { get; set; }
        // The ID of the chapter in the Lore config file
        int LoreChapterID { get; set; }
        // The maximum functional Power of an Enchantment
        int MaxTier { get; set; }
        // Configurable JSON multiplier for effects
        Dictionary<string, object> Modifiers { get; set; }
        // Used to manage generic ticks. You still have to register your tick method with the API.
        Dictionary<long, EnchantTick> TickRegistry { get; set; }
        /// <summary>
        /// Generic method to execute a method matching the Trigger parameter. Called by the TriggerEnchant event in KRPGEnchantmentSystem.
        /// </summary>
        /// <param name="enchant"></param>
        /// <param name="slot"></param>
        /// <param name="parameters"></param>
        void OnTrigger(EnchantmentSource enchant, ItemSlot slot, ref object[] parameters);
        /// <summary>
        /// Triggered from an enchanted item when it successfully attacks an entity.
        /// </summary>
        /// <param name="enchant"></param>
        /// <param name="slot"></param>
        /// <param name="damage"></param>
        void OnAttack(EnchantmentSource enchant, ItemSlot slot, ref float? damage);
        void OnHit(EnchantmentSource enchant, ItemSlot slot, ref float? damage);
        void OnToggle(EnchantmentSource enchant, ItemSlot slot);
        void OnStart(EnchantmentSource enchant, ItemSlot slot);
        void OnEnd(EnchantmentSource enchant, ItemSlot slot);
    }
}
