using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace KRPGLib.Enchantment
{
    public class ChargingBlock : Block
    {
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
        }

        public override List<ItemStack> GetHandBookStacks(ICoreClientAPI capi)
        {
            return base.GetHandBookStacks(capi);
        }
    }
}