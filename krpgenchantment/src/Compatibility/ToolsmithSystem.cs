using System;
using Toolsmith.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace KRPGLib.Enchantment.Compat
{
    /// <summary>
    /// Compatibility system for the Toolsmith mod.
    /// </summary>
    public class ToolsmithSystem
    {
        ICoreServerAPI sApi;
        public void StartServerSide(ICoreServerAPI sapi)
        {
            sApi = sapi;
        }
        /// <summary>
        /// Processes the reversion enchantment for Toolsmith tools. This method should be called when the reversion enchantment is triggered on a Toolsmith tool to restore its durability.
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="amount"></param>
        /// <param name="mulH"></param>
        /// <param name="mulB"></param>
        public void ProcessReversion(ItemSlot slot, int amount, float mulH, float mulB)
        {
            if ((slot.Itemstack.HasToolhandleCurrentDurability() != false) && (slot.Itemstack.GetToolhandleCurrentDurability() < slot.Itemstack.GetToolhandleMaxDurability()))
            {
                int remDurH = slot.Itemstack.GetToolhandleCurrentDurability();
                int maxDurH = slot.Itemstack.GetToolhandleMaxDurability();
                if (remDurH < maxDurH)
                {
                    remDurH += (int)(amount * mulH);
                    remDurH = Math.Min(remDurH, maxDurH);
                    slot.Itemstack.SetToolhandleCurrentDurability(remDurH);
                }
            }
            if ((slot.Itemstack.HasToolbindingCurrentDurability() != false) && (slot.Itemstack.GetToolbindingCurrentDurability() < slot.Itemstack.GetToolbindingMaxDurability()))
            {
                int remDurB = slot.Itemstack.GetToolbindingCurrentDurability();
                int maxDurB = slot.Itemstack.GetToolbindingMaxDurability();
                if (remDurB < maxDurB)
                {
                    remDurB += (int)(amount * mulB);
                    remDurB = Math.Min(remDurB, maxDurB);
                    slot.Itemstack.SetToolbindingCurrentDurability(remDurB);
                }
            }
        }
    }
}
