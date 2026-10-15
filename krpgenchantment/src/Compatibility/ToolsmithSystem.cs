using KRPGLib.Enchantment;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Toolsmith;
using Toolsmith.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace KRPGLib.Enchantment.Compat
{
    public class ToolsmithSystem
    {
        ICoreAPI Api;
        ICoreClientAPI cApi;
        ICoreServerAPI sApi;

        public bool ToolsmithSysEnchantCompatActive = false;
        public void StartClientSide(ICoreClientAPI capi)
        {
            if (!(capi is ICoreClientAPI)) return;
        }
        public void StartServerSide(ICoreServerAPI sapi)
        {
            if (!(sapi is ICoreServerAPI)) return;
        }

        public void ProcessReversion(ItemSlot slot, int amount)
        {
            if ((slot.Itemstack.GetToolhandleForData() != null) && (slot.Itemstack.GetToolhandleCurrentDurability() < slot.Itemstack.GetToolhandleMaxDurability()))
            {
                int remDurH = slot.Itemstack.GetToolhandleCurrentDurability();
                int maxDurH = slot.Itemstack.GetToolhandleMaxDurability();
                if (remDurH < maxDurH)
                {
                    remDurH += amount;
                    remDurH = Math.Min(remDurH, maxDurH);
                    slot.Itemstack.SetToolhandleCurrentDurability(remDurH);
                }
            }
            if ((slot.Itemstack.GetToolbinding() != null) && (slot.Itemstack.GetToolbindingCurrentDurability() < slot.Itemstack.GetToolbindingMaxDurability()))
            {
                int remDurB = slot.Itemstack.GetToolbindingCurrentDurability();
                int maxDurB = slot.Itemstack.GetToolbindingMaxDurability();
                if (remDurB < maxDurB)
                {
                    remDurB += amount;
                    remDurB = Math.Min(remDurB, maxDurB);
                    slot.Itemstack.SetToolbindingCurrentDurability(remDurB);
                }
            }
        }
    }
}
