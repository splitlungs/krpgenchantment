﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using KRPGLib.Enchantment.API;
using Vintagestory.GameContent;
using Vintagestory.API.Config;
using HarmonyLib;
using Vintagestory.API.Server;

namespace KRPGLib.Enchantment
{
    public class ReversionEnchantment : Enchantment
    {
        long TickDuration { get { return Modifiers.GetLong("TickDuration"); } }
        int PowerMultiplier { get { return Modifiers.GetInt("PowerMultiplier"); } }
        /// <summary>
        /// Rolls a % chance to negate item damage.
        /// </summary>
        /// <param name="api"></param>
        public ReversionEnchantment(ICoreAPI api) : base(api)
        {
            // Setup the default config
            Enabled = true;
            Code = "reversion";
            Category = "Universal";
            LoreCode = "enchantment-reversion";
            LoreChapterID = 19;
            MaxTier = 5;
            ValidToolTypes = new List<string>() {
                "Armor-Head", "Armor-Body", "Armor-Legs",
                "ArmorHead", "ArmorBody", "ArmorLegs",
                "Shield",
                "Chisel", "Cleaver", "Hammer", "Hoe", "Meter", "Pickaxe", "Probe", "Saw", "Scythe", "Shears", "Shovel", "Sickle", "Wrench",
                "Knife", "Axe",
                "Club", "Sword",
                "Spear",
                "Bow", "Sling",
                "Drill",
                "Halberd", "Mace", "Pike", "Polearm", "Poleaxe", "Staff", "Warhammer",
                "Javelin",
                "Crossbow", "Firearm",
                "Wand" };
            Modifiers = new EnchantModifiers 
            {
                {"TickDuration", 10000 }, { "PowerMultiplier", 1 } 
            };
            Version = 1.00f;
        }
        public override void OnEquip(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            EnchantmentEntityBehavior eeb = enchant.CauseEntity.GetBehavior<EnchantmentEntityBehavior>();
            if (eeb == null) return;
            // Get ID's
            int stackID = enchant.SourceStack.Id;
            int slotID = enchant.SourceSlot.Inventory.GetSlotId(enchant.SourceSlot);
            string codeID = Code + ":" + slotID + ":" + stackID;
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] CodeID for Reversion Tick is {0}.", codeID);
            if (!enchant.SourceSlot.Empty)
            {
                // Toggle On
                if (!eeb.TickRegistry.ContainsKey(codeID))
                {
                    EnchantTick eTick = enchant.ToEnchantTick();
                    eTick.Persistent = true;
                    eTick.IsHotbar = parameters.GetBool("IsHotbar");
                    eTick.TickDuration = TickDuration;
                    // { LastTickTime = 0, Source = enchant, TicksRemaining = 0, Persistent = true, IsHotbar = parameters.GetBool("IsHotbar") };
                    eeb.TickRegistry.Add(codeID, eTick);
                }
                // Toggle Off - Failsafe
                // else
                // {
                //     eeb.TickRegistry[codeID].Dispose();
                //     eeb.TickRegistry.Remove(codeID);
                // }
            }
            // Toggle Off - If Empty
            else
            {
                eeb.TickRegistry[codeID].Dispose();
            }
        }
        public override void OnTick(ref EnchantTick eTick)
        {
            if (!(Api is ICoreServerAPI api))
            {
                Api.Logger.Event("[KRPGEnchantment] Failed to get ICoreServerAPI for a Reversion tick. Disposing.");
                eTick.Dispose();
                return;
            }
            Entity entity = api.World.GetEntityById(eTick.CauseEntityID);
            if (entity == null)
            {
                Api.Logger.Event("[KRPGEnchantment] Failed to get the Entity for a Reversion tick. Disposing.");
                eTick.Dispose();
                return;
            }
            EnchantmentEntityBehavior eeb = entity.GetBehavior<EnchantmentEntityBehavior>();
            IInventory inventory;
            if (eTick.IsHotbar == true)
                inventory = eeb.hotbarInventory;
            else
                inventory = eeb.gearInventory;
            if (inventory == null)
            {
                Api.Logger.Event("[KRPGEnchantment] Failed to get the IInventory for a Reversion tick. Disposing.");
                eTick.Dispose();
                return;
            }
            ItemSlot slot = inventory[eTick.SlotID];
            if (slot.Empty)
            {
                Api.Logger.Event("[KRPGEnchantment] Failed to get the ItemSlot for a Reversion tick. Disposing.");
                eTick.Dispose();
                return;
            }

            // IPlayer p = eTick.Source.SourceEntity as IPlayer;
            // if (!p.InventoryManager.ActiveHotbarSlot.Empty)
            // {
            //     Dictionary<string, int> enchants = Api.EnchantAccessor().GetActiveEnchantments(p.InventoryManager.ActiveHotbarSlot.Itemstack);
            //     if (enchants != null) sApi
            // }
            // if (EnchantingConfigLoader.Config?.Debug == true)
            //     Api.Logger.Event("[KRPGEnchantment] {0} is being ticked by a Reversion enchantment.", eTick.Source.SourceStack.GetName());

            // if (EnchantingConfigLoader.Config?.Debug == true)
            //     Api.Logger.Event("[KRPGEnchantment] Current Time: {0}, Last Tick Time: {1}, Tick Duration: {2}", Api.World.ElapsedMilliseconds, eTick.LastTickTime, TickDuration);

            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] {0} is being affected by a Reversion enchantment.", slot.Itemstack.GetName());

            EntityPos causePos = entity.SidedPos;
            // EntityBehaviorTemporalStabilityAffected tempStab = eTick.Source.CauseEntity.GetBehavior<EntityBehaviorTemporalStabilityAffected>();
            SystemTemporalStability tempStabilitySystem = api.ModLoader.GetModSystem<SystemTemporalStability>();
            float stabf = tempStabilitySystem.GetTemporalStability(causePos.AsBlockPos);

            // Entity entity = eTick.Source.GetCauseEntity();
            // IPlayer player = null;
            // if (entity is EntityPlayer ep)
            // {
            //     player = Api?.World.PlayerByUid(ep.PlayerUID);
            // }
            // IInventory inv = player.Entity.GetBehavior<EntityBehaviorPlayerInventory>()?.Inventory;

            if (stabf < 1)
            {
                int amount = eTick.Power * PowerMultiplier;
                int remDur = slot.Itemstack.Collectible.GetRemainingDurability(slot.Itemstack);
                int maxDur = slot.Itemstack.Collectible.GetMaxDurability(slot.Itemstack);
                if (remDur < maxDur)
                {

                    remDur += amount;
                    remDur = Math.Min(remDur, maxDur);
                    slot.Itemstack.Attributes.SetInt("durability", remDur);

                    if (EnchantingConfigLoader.Config?.Debug == true)
                        Api.Logger.Event("[KRPGEnchantment] Restoring {0} durability.", amount);
                }
            }
            eTick.LastTickTime = api.World.ElapsedMilliseconds;
            slot.MarkDirty();
        }
    }
}
