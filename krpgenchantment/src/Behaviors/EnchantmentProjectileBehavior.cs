using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using KRPGLib.Enchantment.API;
using Vintagestory.API.Util;
using Vintagestory.API.Datastructures;
using Cairo;

namespace KRPGLib.Enchantment
{
    public class EnchantmentProjectileBehavior : EntityBehavior
    {
        public override string PropertyName() { return "EnchantmentProjectileBehavior"; }
        ICoreAPI Api;
        byte[] lightHsv;
        public EnchantmentProjectileBehavior(Entity entity) : base(entity)
        {
            Api = entity.Api;
        }
        /*
        public override void OnEntitySpawn()
        {
            base.OnEntitySpawn();
            if (!(entity is EntityProjectile proj)) return;
            Api.Logger.Event("[KRPGEnchantment] EnchantmentProjectileBehavior starting Light check.", proj.FiredBy.GetName());
            byte[] b = null;
            // if (proj.ProjectileStack?.Collectible?.Code?.FirstCodePart()?.EqualsFastIgnoreCase("spear") == true) return;
            if (proj.ProjectileStack?.Item?.Tool == EnumTool.Spear)
            {
                b = proj.ProjectileStack.Attributes?.GetBytes("lightHsv", null);
                if (b == null) 
                {
                    Api.Logger.Warning("[KRPGEnchantment] EnchantmentProjectileBehavior could not find light on the firing item stack.");
                    return;
                }
                entity.WatchedAttributes?.SetBytes("lightHsv", b);
                entity.LightHsv = b;
                Api.Logger.Event("[KRPGEnchantment] EnchantmentProjectileBehavior has spawned properly. Fired by {0}.", entity.GetName());
                return;
            }
            if (!(proj.FiredBy is EntityPlayer player))
            {
                Api.Logger.Warning("[KRPGEnchantment] EnchantmentProjectileBehavior could not find firing player.");
                return;
            }
            ItemStack stack = player.ActiveHandItemSlot?.Itemstack;
            if (stack is null)
            {
                Api.Logger.Warning("[KRPGEnchantment] EnchantmentProjectileBehavior could not find firing item stack.");
                return;
            }
            b = stack?.Attributes?.GetBytes("lightHsv", null);
            if (b == null) 
            {
                Api.Logger.Warning("[KRPGEnchantment] EnchantmentProjectileBehavior could not find light on the firing item stack.");
                return;
            }
            entity?.WatchedAttributes?.SetBytes("lightHsv", b);
            entity.LightHsv = b;
            Api.Logger.Event("[KRPGEnchantment] EnchantmentProjectileBehavior has spawned properly. Fired by {0}.", player.GetName());
        }
        */
        /*
        public override void AfterInitialized(bool onFirstSpawn)
        {
            base.AfterInitialized(onFirstSpawn);
            if (!(entity is EntityProjectile proj)) return;
            Api.Logger.Event("[KRPGEnchantment] EnchantmentProjectileBehavior starting Light check.", proj.FiredBy.GetName());
            byte[] b = null;
            if (proj.ProjectileStack?.Collectible?.Code?.FirstCodePart()?.EqualsFastIgnoreCase("spear") == true) return;
            // if (proj.ProjectileStack?.Collectible?.Tool == EnumTool.Spear)
            // {
            //     b = proj.ProjectileStack.Attributes?.GetBytes("lightHsv", null);
            //     if (b == null) 
            //     {
            //         Api.Logger.Warning("[KRPGEnchantment] EnchantmentProjectileBehavior could not find light on the firing item stack.");
            //         return;
            //     }
            //     entity.WatchedAttributes?.SetBytes("lightHsv", b);
            //     entity.LightHsv = b;
            //     Api.Logger.Event("[KRPGEnchantment] EnchantmentProjectileBehavior has spawned properly. Fired by {0}.", entity.GetName());
            //     return;
            // }
            if (!(proj.FiredBy is EntityPlayer player))
            {
                Api.Logger.Warning("[KRPGEnchantment] EnchantmentProjectileBehavior could not find firing player.");
                return;
            }
            ItemStack stack = player.ActiveHandItemSlot?.Itemstack;
            if (stack is null)
            {
                Api.Logger.Warning("[KRPGEnchantment] EnchantmentProjectileBehavior could not find firing item stack.");
                return;
            }
            b = stack?.Attributes?.GetBytes("lightHsv", null);
            if (b == null) 
            {
                Api.Logger.Warning("[KRPGEnchantment] EnchantmentProjectileBehavior could not find light on the firing item stack.");
                return;
            }
            entity?.WatchedAttributes?.SetBytes("lightHsv", b);
            entity.LightHsv = b;
            Api.Logger.Event("[KRPGEnchantment] EnchantmentProjectileBehavior has spawned properly. Fired by {0}.", player.GetName());
        }
        */
        // public override void OnGameTick(float deltaTime)
        // {
        //     base.OnGameTick(deltaTime);
        //     var capi = Api as ICoreClientAPI;
        //     Vec3f color = new Vec3f(32f, 5f, 10f );
        //     Vec3d pos = new Vec3d(0d, 0d, 0d);
        //     IPointLight light = new IPointLight()
        //     {
        //         Color = color,
        //         Pos = pos
        //     };
        //     ICoreServerAPI sapi = Api as ICoreServerAPI;
        //     capi?.Render.AddPointLight(__instance.Pos.AsBlockPos, hsv);
        //     // Force a chunk relight around the entity position
        //     var bpos = __instance.Pos.AsBlockPos;
        //     capi?.World.BlockAccessor.MarkBlockDirty(bpos);
        // }
    }
}