using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using System.Reflection;
using HarmonyLib;
using KRPGLib.Enchantment.API;
// using System.Text.Json.Nodes;
using System.Xml.Linq;
using Vintagestory.API.Server;
using Vintagestory.API.Net;
using Cairo;
using static System.Net.Mime.MediaTypeNames;
using System.Reflection.Metadata;
using System.IO;

namespace KRPGLib.Enchantment
{
    /// <summary>
    /// Class for storing default enchantment data at runtime. Do not save your active enchantments here.
    /// </summary>
    public class EnchantmentBehavior : CollectibleBehavior
    {
        public ICoreAPI Api;
        public ICoreServerAPI sApi;
        public Dictionary<string, int> Enchantments = null;
        public bool Enchantable = false;
        public bool IsReagent = false;
        public float MiningSpeedMul = 1f;
        public EnchantmentBehavior(CollectibleObject collObj) : base(collObj)
        {
        }
        void GetEnchantments(ItemStack itemStack)
        {
            Enchantments = Api.EnchantAccessor().GetActiveEnchantments(itemStack);
        }
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            Api = api;
            // Particles - Not Working Yet
            // We only load the config on the server, so check side first
            if (api.Side != EnumAppSide.Server) return;
            sApi = api as ICoreServerAPI;
            if (EnchantingConfigLoader.Config?.ValidReagents.ContainsKey(collObj.Code) == true) 
                IsReagent = true;
            // Configure the Efficiency multiplier
            IEnchantment ench = sApi.EnchantAccessor().GetEnchantment("efficient");
            if (ench != null)
                MiningSpeedMul = ench.Modifiers.GetFloat("PowerMultiplier");
            
        }
        public override void OnUnloaded(ICoreAPI api)
        {
            base.OnUnloaded(api);
        }
        public override void OnDamageItem(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, ref int amount, ref EnumHandling bhHandling)
        {
            if (!(world.Api is ICoreServerAPI api)) return;
            
            bhHandling = EnumHandling.Handled;
            Dictionary<string, int> enchants = api.EnchantAccessor().GetActiveEnchantments(itemslot.Itemstack);
            if (enchants == null) return;

            int durable = enchants.GetValueOrDefault("durable", 0);
            if (durable > 0)
            {
                EnchantmentSource enchant = new EnchantmentSource()
                {
                    SourceStack = itemslot.Itemstack,
                    TargetEntity = byEntity,
                    Trigger = "OnDurability",
                    Code = "durable",
                    Power = durable
                };
                int dmg = amount;
                EnchantModifiers parameters = new EnchantModifiers() { { "damage", dmg } };
                bool didEnchantment = api.EnchantAccessor().TryEnchantment(enchant, ref parameters);
                if (didEnchantment == true)
                {
                    amount = parameters.GetInt("damage");
                }
            }
        }
        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            // Enchantments
            Dictionary<string, int> enchants = world.Api.EnchantAccessor().GetActiveEnchantments(inSlot?.Itemstack);
            if (enchants != null)
            {
                foreach (KeyValuePair<string, int> pair in enchants)
                {
                    // THE RIGHT WAY
                    dsc.AppendLine(string.Format("<font color=\"{0}\">{1} {2}</font>",
                    Enum.GetName(typeof(EnchantColors), pair.Value),
                    Lang.Get("krpgenchantment:enchantment-" + pair.Key),
                    Lang.Get("krpgenchantment:" + pair.Value)));
                }
            }
            // Reagent Charge
            int p = world.Api.EnchantAccessor().GetReagentCharge(inSlot.Itemstack);
            if (p != 0)
            {
               string s = string.Format("<font color=\"" + Enum.GetName(typeof(EnchantColors), p) + "\">" + Lang.Get("krpgenchantment:reagent-charge-prefix") + p.ToString() + "</font>");
                dsc.AppendLine(s);
            }
        }
        public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handHandling, ref EnumHandling handling)
        {
            if (!(Api is ICoreServerAPI sapi)) return;
            handHandling = EnumHandHandling.Handled;
            handling = EnumHandling.Handled;
            EnchantModifiers parameters = new EnchantModifiers();
            bool didEnchants = sapi.EnchantAccessor().TryEnchantments(slot, "OnAttackStart", byEntity, entitySel?.Entity, ref parameters);
            if (!didEnchants)
                    sapi.Logger.Warning("[KRPGEnchantments] Failed to TryEnchantments on {0}!", byEntity.GetName());
            base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handHandling, ref handling);
        }
        // Not called here?
        // public override bool OnHeldAttackCancel(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel, EnumItemUseCancelReason cancelReason, ref EnumHandling handling)
        // {
        //     if (!(Api is ICoreServerAPI sapi)) return base.OnHeldAttackCancel(secondsPassed, slot, byEntity, blockSelection, entitySel, cancelReason, ref handling);
        //     handling = EnumHandling.Handled;
        //     EnchantModifiers parameters = new EnchantModifiers();
        //     bool didEnchantments = sapi.EnchantAccessor().TryEnchantments(slot, "OnAttackCancel", byEntity, entitySel?.Entity, ref parameters);
        //     return base.OnHeldAttackCancel(secondsPassed, slot, byEntity, blockSelection, entitySel, cancelReason, ref handling);
        // }
        // Not called here?
        // public override void OnHeldAttackStop(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel, ref EnumHandling handling)
        // {
        //     if (!(Api is ICoreServerAPI sapi)) return;
        //     handling = EnumHandling.Handled;
        //     EnchantModifiers parameters = new EnchantModifiers();
        //     bool didEnchantments = sapi.EnchantAccessor().TryEnchantments(slot, "OnAttackStop", byEntity, entitySel?.Entity, ref parameters);
        //     base.OnHeldAttackStop(secondsPassed, slot, byEntity, blockSelection, entitySel, ref handling);
        // }
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
        {
            if (!(Api is ICoreServerAPI sapi)) return;
            handHandling = EnumHandHandling.Handled;
            handling = EnumHandling.Handled;
            EnchantModifiers parameters = new EnchantModifiers();
            bool didEnchants = sapi.EnchantAccessor().TryEnchantments(slot, "OnAttackStart", byEntity, entitySel?.Entity, ref parameters);
            if (!didEnchants)
                    sapi.Logger.Warning("[KRPGEnchantments] Failed to TryEnchantments on {0}!", byEntity.GetName());
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling);
        }
        // Not called here?
        // public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason, ref EnumHandling handled)
        // {
        //     if (!(Api is ICoreServerAPI sapi)) return base.OnHeldInteractCancel(secondsUsed, slot, byEntity, blockSel, entitySel, cancelReason, ref handled);
        //     handled = EnumHandling.Handled;
        //     EnchantModifiers parameters = new EnchantModifiers();
        //     bool didEnchantments = sapi.EnchantAccessor().TryEnchantments(slot, "OnAttackCancel", byEntity, entitySel?.Entity, ref parameters);
        //     return base.OnHeldInteractCancel(secondsUsed, slot, byEntity, blockSel, entitySel, cancelReason, ref handled);
        // }
        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
        {
            // base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel, ref handling);
            if (!(byEntity.Api is ICoreServerAPI sapi)) return;
            handling = EnumHandling.Handled;
            // EnchantModifiers parameters = new EnchantModifiers();
            // bool didEnchantments = sApi.EnchantAccessor().TryEnchantments(slot, "OnAttackStop", byEntity, entitySel?.Entity, ref parameters);

            // Specific use on self for KRPG Wands
            if (secondsUsed < 1 || slot?.Itemstack?.Collectible?.Class != "WandItem") return;

            int aimSelf = byEntity.WatchedAttributes.GetInt("aimSelf", 0);
            if (aimSelf == 1 && sApi.EnchantAccessor().GetActiveEnchantments(slot.Itemstack) != null)
            {
                EnchantModifiers parameters = new EnchantModifiers();
                bool didEnchantments = sApi.EnchantAccessor().TryEnchantments(slot, "OnAttackStop", byEntity, byEntity, ref parameters);
                if (didEnchantments == true && EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] Finished processing Enchantments on EnchantmentBehavior.OnInteractStop().");
            }

            if (byEntity.Attributes.GetInt("aimingCancel") == 1)
            {
                return;
            }
        }
        public override float OnGetMiningSpeed(IItemStack itemstack, BlockSelection blockSel, Block block, IPlayer forPlayer, ref EnumHandling bhHandling)
        {
            Dictionary<string, int> enchants = Api?.EnchantAccessor()?.GetActiveEnchantments((ItemStack)itemstack);
            if (enchants?.TryGetValue("efficient", out int power) == true)
            {
                float mSpeed = power * MiningSpeedMul;
                bhHandling = EnumHandling.Handled;
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] Applied an Efficient enchantment. Post MiningSpeedMul is {0}.", mSpeed);
                return mSpeed + base.OnGetMiningSpeed(itemstack, blockSel, block, forPlayer, ref bhHandling);
            }
            return base.OnGetMiningSpeed(itemstack, blockSel, block, forPlayer, ref bhHandling);
        }
    }
}