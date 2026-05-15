using System;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.ServerMods;
using KRPGLib.Enchantment.API;
using Vintagestory.GameContent;

namespace KRPGLib.Enchantment
{
    /// <summary>
    /// Not in use yet. Behavior for advanced Enchantment behavior on Block Entities.
    /// </summary>
    public class EnchantmentBEBehavior : BlockEntityBehavior
    {
        ICoreServerAPI sApi;
        float DropsMul = 1;
        public EnchantmentBEBehavior(BlockEntity be) : base(be)
        {
            
        }
        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            Api = api;
            // We only load the config on the server, so check side first
            if (api.Side != EnumAppSide.Server) return;
            sApi = api as ICoreServerAPI;
            // Configure the Fortunate multiplier
            IEnchantment ench = sApi.EnchantAccessor().GetEnchantment("fortunate");
            DropsMul = ench.Modifiers.GetFloat("PowerMultiplier");
        }
        #nullable enable
        public override void OnBlockBroken(IPlayer? byPlayer = null)
        {
            base.OnBlockBroken(byPlayer);
        }
        #nullable disable
        // This must exist here because of the fucking charcoal shenanigans.
        // What an absolute crazy mess of code that is.
        public float GetFortunateMul(ItemStack itemstack, int power)
        {
            bool valid = false;
            EnumTool? tool = itemstack.Item.Tool;
            switch (tool)
            {
                case EnumTool.Shovel:
                    if (Block.Code.FirstCodePart() == "charcoalpit")
                    {
                        Api.Logger.Event("[KRPGEnchantment] This block is a CharcoalPit");
                        valid = true;
                    }
                    else if (Block.Code.FirstCodePart() == "charcoalpile")
                    {
                        Api.Logger.Event("[KRPGEnchantment] This block is a CharcoalPile");
                        valid = true;
                        
                    }
                    break;
                default:
                    break;
            }
            float dMul = 1.0f;
            if (valid)
                dMul += power * DropsMul;
            return dMul;
        }
    }
}