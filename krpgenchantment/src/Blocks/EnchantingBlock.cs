using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace KRPGLib.Enchantment
{
    // public class EnchantingBlockTrunk : EnchantingBlock
    // {
    //     ICoreAPI Api;
    // 
    //     public override void OnLoaded(ICoreAPI api)
    //     {
    //         base.OnLoaded(api);
    //         Api = api;
    //     }
    // }

    public class EnchantingBlock : Block
    {
        ICoreAPI Api;
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            Api = api;
        }
        public override List<ItemStack> GetHandBookStacks(ICoreClientAPI capi)
        {
            return base.GetHandBookStacks(capi);
        }
    }
}