﻿using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using KRPGLib.Enchantment.API;
using CombatOverhaul.Armor;

namespace KRPGLib.Enchantment
{
    public class EnchantmentEntityBehavior : EntityBehavior
    {
        public override string PropertyName() { return "EnchantmentEntityBehavior"; }
        public ICoreAPI Api;
        public ICoreClientAPI cApi;
        public ICoreServerAPI sApi;

        private EntityAgent agent;
        private IServerPlayer player = null;
        private IInventory gearInventory = null;
        private IInventory hotbarInventory = null;

        public Dictionary<string, EnchantTick> TickRegistry;

        public bool IsPlayer { get { if (player != null) return true; return false; } }

        public override void OnEntityDeath(DamageSource damageSourceForDeath)
        {
            base.OnEntityDeath(damageSourceForDeath);
            
            // TODO: Make a way to check if the ticks should be cleared on death or not.
            TickRegistry.Clear();
        }

        public EnchantmentEntityBehavior(Entity entity) : base(entity)
        {
            Api = entity.Api;
            sApi = entity.Api as ICoreServerAPI;
            agent = entity as EntityAgent;
            TickRegistry = new Dictionary<string, EnchantTick>();
            
            // if (agent != null)
            // {
            //     EntityBehaviorHealth hp = entity.GetBehavior<EntityBehaviorHealth>();
            //     hp.onDamaged += this.OnDamaged;
            // }
        }
        // public float OnDamaged(float dmg, DamageSource dmgSource)
        // {
        //     if (EnchantingConfigLoader.Config?.Debug == true)
        //         Api.Logger.Event("[KRPGEnchantment] {0}'s Health behavior has received damage event for {1} {2} damage.", entity.GetName(), dmg, dmgSource.Type);
        //     return dmg;
        // }
        public void RegisterPlayer(IServerPlayer byPlayer)
        {
            if (Api.Side != EnumAppSide.Server) return;
            
            // Save the IServerPlayer
            player = byPlayer;
            // Register inventory listener
            gearInventory = player.Entity?.GetBehavior<EntityBehaviorPlayerInventory>()?.Inventory;
            gearInventory.SlotModified += OnGearModified;
            hotbarInventory = player.InventoryManager.GetHotbarInventory();
            hotbarInventory.SlotModified += OnHotbarModified;
            // Initialize already equipped items
            ICoreServerAPI sapi = Api as ICoreServerAPI;
            foreach (ItemSlot slot in gearInventory)
            {
                EnchantModifiers parameters = new EnchantModifiers() { { "IsHotbar", false } };
                if (!slot.Empty) sapi.EnchantAccessor().TryEnchantments(slot, "OnEquip", entity, entity, ref parameters);
            }
            foreach (ItemSlot slot in hotbarInventory)
            {
                EnchantModifiers parameters = new EnchantModifiers() { { "IsHotbar", true } };
                if (!slot.Empty) sapi.EnchantAccessor().TryEnchantments(slot, "OnEquip", entity, entity, ref parameters);
            }
        }
        
        public void OnGearModified(int slotId)
        {
            // Sanity Check
            if (Api.Side != EnumAppSide.Server || gearInventory == null) return;
            // Cleanup empty slots
            if (gearInventory[slotId].Empty)
            {
                foreach (KeyValuePair<string, EnchantTick> pair in TickRegistry)
                {
                    string s = pair.Key.Split(":")[1];
                    if (s == slotId.ToString())
                    {
                        TickRegistry.Remove(pair.Key);
                    }
                }
                return;
            }

            // Armor slots, probably
            // 12.Head 13.Body 14.Legs
            // int[] wearableSlots = new int[3] { 12, 13, 14 };

            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] Player {0} gear modified slot {1}. Attempting to trigger OnEquip enchantments.", player.PlayerUID, slotId);

            // ItemSlot slot = gearInventory[slotId];
            EnchantModifiers parameters = new EnchantModifiers() { { "IsHotbar", false } };
            sApi.EnchantAccessor().TryEnchantments(gearInventory[slotId], "OnEquip", entity, entity, ref parameters);
        }
        public void OnHotbarModified(int slotId)
        {
            // Sanity Check
            if (Api.Side != EnumAppSide.Server || hotbarInventory == null) return;
            // Cleanup empty slots
            if (hotbarInventory[slotId].Empty)
            {
                foreach (KeyValuePair<string, EnchantTick> pair in TickRegistry)
                {
                    string s = pair.Key.Split(":")[1];
                    if (s == slotId.ToString())
                    {
                        TickRegistry.Remove(pair.Key);
                    }
                }
                return;
            }

            // 11. Offhand, probably
            // int activeSlot = player.InventoryManager.ActiveHotbarSlotNumber;
            // if (activeSlot != (slotId | 11)) return;

            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] Player {0} modified hotbar slot {1}. Attempting to trigger OnEquip enchantments.", player.PlayerUID, slotId);

            // ItemSlot slot = hotbarInventory[slotId];
            EnchantModifiers parameters = new EnchantModifiers() { { "IsHotbar", true } };
            sApi.EnchantAccessor().TryEnchantments(hotbarInventory[slotId], "OnEquip", entity, entity, ref parameters);
        }
        public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
        {
            if (mode == EnumInteractMode.Attack && itemslot.Itemstack != null && entity.Api.Side == EnumAppSide.Server)
            {
                if (EnchantingConfigLoader.Config?.Debug == true)
                    byEntity.Api.Logger.Event("[KRPGEnchantment] {0} was attacked by an enchanted weapon.", entity.GetName());
                // Get Enchantments
                Dictionary<string, int> enchants = byEntity.Api.EnchantAccessor().GetActiveEnchantments(itemslot.Itemstack);
                if (enchants != null)
                {
                    // Should avoid default during healing
                    if (enchants.ContainsKey(EnumEnchantments.healing.ToString()))
                        handled = EnumHandling.PreventDefault;
                    else
                        handled = EnumHandling.Handled;

                    ICoreServerAPI sapi = Api as ICoreServerAPI;
                    EnchantModifiers parameters = new EnchantModifiers();
                    sapi.EnchantAccessor().TryEnchantments(itemslot, "OnAttack", byEntity, entity, ref parameters);
                }
            }
            base.OnInteract(byEntity, itemslot, hitPosition, mode, ref handled);
        }
        public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
        {
            base.OnEntityReceiveDamage(damageSource, ref damage);
        }
        public override void OnGameTick(float deltaTime)
        {
            if (Api.Side != EnumAppSide.Server || TickRegistry?.Count <= 0) return;

            if (!player.InventoryManager.ActiveHotbarSlot.Empty) 
            {
                if (Api.EnchantAccessor().GetActiveEnchantments(player.InventoryManager.ActiveHotbarSlot.Itemstack) != null)
                {

                }
            }

            foreach (KeyValuePair<string, EnchantTick> pair in TickRegistry)
            {
                // Don't run if it's on hotbar, but unselected & not in the offhand
                if (pair.Value.IsHotbar == true 
                    && pair.Value.Source.SourceStack.Id != player?.InventoryManager?.ActiveHotbarSlot?.Itemstack?.Id
                    && pair.Value.Source.SourceSlot.StorageType != EnumItemStorageFlags.Offhand)
                    continue;
                
                // Handle multi
                string eCode = pair.Key;
                int tr = pair.Value.TicksRemaining;
                if (tr > 0 || pair.Value.Persistent == true)
                {
                    if (pair.Key.Contains(":")) eCode = eCode.Split(":")?[0];
                    IEnchantment enchant = sApi.EnchantAccessor().GetEnchantment(eCode);
                    EnchantTick eTick = pair.Value;
                    enchant.OnTick(deltaTime, ref eTick);
                    TickRegistry[pair.Key] = eTick;
                }
                else
                {
                    if (EnchantingConfigLoader.Config?.Debug == true)
                        Api.Logger.Event("[KRPGEnchantment] Enchantment finished Ticking for {0}.", eCode);
                    pair.Value.Dispose();
                    TickRegistry.Remove(eCode);
                }
            }
        }
    }
}