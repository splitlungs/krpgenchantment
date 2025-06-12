using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Net;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.ServerMods.NoObf;
using static System.Net.Mime.MediaTypeNames;
using KRPGLib.Enchantment.API;
using System.Collections;
using HarmonyLib;

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

        public Dictionary<string, EnchantTick> TickRegistry;

        public bool IsPlayer { get { if (player != null) return true; return false; } }

        public override void OnEntityDeath(DamageSource damageSourceForDeath)
        {
            base.OnEntityDeath(damageSourceForDeath);

            KRPGEnchantmentSystem eSys = Api.ModLoader.GetModSystem<KRPGEnchantmentSystem>();
            
        }

        public EnchantmentEntityBehavior(Entity entity) : base(entity)
        {
            Api = entity.Api;
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
            player = byPlayer;
            // We'll probably use this later
            // Edit: We won't in 1.20
            // Edit2: Maybe we will
            // player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName).SlotModified += OnGearModified;
        }
        public void OnGearModified(int slotId)
        {
            if (!IsPlayer)
            {
                Api.Logger.Event("Player {0} modified slot {1}", player.PlayerUID, slotId);
                IInventory ownInventory = player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName);
                if (ownInventory != null)
                {
                    if (ownInventory[slotId].Empty)
                        Api.Logger.Event("Modified slot {0} was empty!", slotId);
                    else
                    {
                        int power = ownInventory[slotId].Itemstack.Attributes.GetInt(EnumEnchantments.protection.ToString(), 0);
                        Api.Logger.Event("Modified slot {0} as Protection {1}", slotId, power);
                    }
                }
            }
        }
        public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
        {
            if (mode == EnumInteractMode.Attack && itemslot.Itemstack != null && entity.Api.Side == EnumAppSide.Server)
            {
                if (EnchantingConfigLoader.Config?.Debug == true)
                    byEntity.Api.Logger.Event("[KRPGEnchantment] {0} was attacked by an enchanted weapon.", entity.GetName());
                // Get Enchantments
                Dictionary<string, int> enchants = byEntity.Api.EnchantAccessor().GetEnchantments(itemslot.Itemstack);
                if (enchants != null)
                {
                    // Should avoid default during healing
                    if (enchants.ContainsKey(EnumEnchantments.healing.ToString()))
                        handled = EnumHandling.PreventDefault;
                    else
                        handled = EnumHandling.Handled;
        
                    EnchantModifiers parameters = new EnchantModifiers();
                    byEntity.Api.EnchantAccessor().TryEnchantments(itemslot, "OnAttack", byEntity, entity, ref parameters);
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
            base.OnGameTick(deltaTime);

            if (Api.Side == EnumAppSide.Server && TickRegistry.Count > 0)
            {
                foreach (KeyValuePair<string, EnchantTick> pair in TickRegistry)
                {
                    int tr = pair.Value.TicksRemaining;
                    if (tr > 0)
                    {
                        IEnchantment enchant = Api.EnchantAccessor().GetEnchantment(pair.Key);
                        EnchantTick eTick = pair.Value;
                        enchant.OnTick(deltaTime, ref eTick);
                        TickRegistry[pair.Key] = eTick;
                    }
                    else
                    {
                        if (EnchantingConfigLoader.Config?.Debug == true)
                            Api.Logger.Event("[KRPGEnchantment] Enchantment finished Ticking for {0}.", pair.Key);
                        pair.Value.Dispose();
                        TickRegistry.Remove(pair.Key);
                    }
                }
            }
        }
    }
}