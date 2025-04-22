using System;
using System.Collections.Generic;
using Vintagestory.GameContent;
using Vintagestory.API.Server;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using HarmonyLib;
using System.Reflection;
using Vintagestory.API.Common.Entities;

namespace KRPGLib.Enchantment
{
    /// <summary>
    /// Base class for an Enchantment.
    /// </summary>
    public abstract class Enchantment
    {
        public bool Enabled = false; // Toggles processing of this enchantment
        public string Code = "enchantment";
        public string Name = "Enchantment";
        public string Description = "Description of an enchantment.";
        public string[] ItemType = new string[] { "all" };
        public int MaxTier = 5;
        public float Multiplier = 1f;

        /// <summary>
        /// Will execute a method matching the Trigger parameter. Called by the TriggerEnchant event in KRPGEnchantmentSystem.
        /// </summary>
        /// <param name="trigger"></param>
        /// <param name="target"></param>
        /// <param name="damageSource"></param>
        /// <param name="slot"></param>
        /// <param name="damage"></param>
        public void OnTrigger(string trigger, Entity target, DamageSource damageSource, ItemSlot slot, ref float damage)
        {
            MethodInfo meth = this.GetType().GetMethod(trigger, BindingFlags.Instance);
            meth?.Invoke(this, new object[4] { target, damageSource, slot, damage });
        }

        protected virtual void OnLoaded()
        {

        }
        protected virtual void OnHit()
        {

        }
        protected virtual void OnToggle()
        {

        }
        protected virtual void OnEnd()
        {

        }
    }
}
