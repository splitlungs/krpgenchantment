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
        // Similar to "Attributes". You can set your own serializable values here
        Dictionary<string, object> Modifiers { get; set; }
        // Configured in struct, assigned by config file
        // ITreeAttribute Attributes { get; set; }
        // Used to manage generic ticks. You still have to register your tick method with the API.
        Dictionary<long, EnchantTick> TickRegistry { get; set; }
        void Initialize(EnchantmentProperties properties);
        #nullable enable
        /// <summary>
        /// Generic method to execute a method matching the Trigger parameter. Called by the TriggerEnchant event in KRPGEnchantmentSystem.
        /// </summary>
        /// <param name="enchant"></param>
        /// <param name="parameters"></param>
        void OnTrigger(EnchantmentSource enchant, ref Dictionary<string, object>? parameters);
        #nullable disable
        /// <summary>
        /// Triggered from an enchanted item when it successfully attacks an entity.
        /// </summary>
        /// <param name="enchant"></param>
        /// <param name="parameters"></param>
        void OnAttack(EnchantmentSource enchant, ref Dictionary<string, object> parameters);
        void OnHit(EnchantmentSource enchant, ref Dictionary<string, object> parameters);
        void OnToggle(EnchantmentSource enchant);
        void OnStart(EnchantmentSource enchant);
        void OnEnd(EnchantmentSource enchant);
    }
}
