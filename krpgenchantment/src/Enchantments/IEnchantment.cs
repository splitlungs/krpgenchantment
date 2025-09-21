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
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

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
        // The EnumTool types in string format which can receive this enchantment
        public List<string> ValidToolTypes { get; set; }
        // The maximum functional Power of an Enchantment
        int MaxTier { get; set; }
        // Similar to "Attributes". You can set your own serializable values here
        EnchantModifiers Modifiers { get; set; }
        // Configured in struct, assigned by config file
        // ITreeAttribute Attributes { get; set; }
        // Used to manage generic ticks. You still have to register your tick method with the API.
        // Dictionary<long, EnchantTick> TickRegistry { get; set; }
        void Initialize(EnchantmentProperties properties);
        /// <summary>
        /// Attempt to write this Enchantment to provided ItemStack. Returns null if it cannot enchant the item.
        /// </summary>
        /// <param name="inStack"></param>
        /// <param name="enchantPower"></param>
        /// <param name="api"></param>
        /// <returns></returns>
        bool TryEnchantItem(ref ItemStack inStack, int enchantPower, ICoreServerAPI api);
        #nullable enable
        /// <summary>
        /// Generic method to execute a method matching the Trigger parameter. Called by the TriggerEnchant event in KRPGEnchantmentSystem.
        /// </summary>
        /// <param name="enchant"></param>
        /// <param name="parameters"></param>
        void OnTrigger(EnchantmentSource enchant, ref EnchantModifiers? parameters);
        #nullable disable
        /// <summary>
        /// Generic method to execute a method matching the Trigger parameter. Called by the TriggerEnchant event in KRPGEnchantmentSystem.
        /// </summary>
        /// <param name="enchant"></param>
        void OnTrigger(EnchantmentSource enchant);
        /// <summary>
        /// Triggered from an enchanted item when it successfully attacks an entity.
        /// </summary>
        /// <param name="enchant"></param>
        /// <param name="parameters"></param>
        void OnAttack(EnchantmentSource enchant, ref EnchantModifiers parameters);
        /// <summary>
        /// Triggered when an entity wearing an enchanted item is successfully attacked.
        /// </summary>
        /// <param name="enchant"></param>
        /// <param name="parameters"></param>
        void OnHit(EnchantmentSource enchant, ref EnchantModifiers parameters);
        /// <summary>
        /// Called by the Enchantment Entity behavior or Enchantment Behavior.
        /// </summary>
        /// <param name="deltTime"></param>
        void OnTick(float deltTime, ref EnchantTick eTick);
        /// <summary>
        /// Called by the Enchantment Entity behavior when an entity changes an equip slot.
        /// </summary>
        /// <param name="enchant"></param>
        /// <param name="parameters"></param>
        void OnEquip(EnchantmentSource enchant, ref EnchantModifiers parameters);
        /// <summary>
        /// Called by an ItemStack when a toggle is requested.
        /// </summary>
        /// <param name="enchant"></param>
        /// <param name="parameters"></param>
        void OnToggle(EnchantmentSource enchant, ref EnchantModifiers parameters);
    }
}
