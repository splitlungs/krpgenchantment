using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.ServerMods;

namespace KRPGLib.Enchantment
{
    /// <summary>
    /// Not in use yet. Behavior for advanced Enchantment behavior on Block Entities.
    /// </summary>
    public class EnchantmentBEBehavior : BlockEntityBehavior
    {
        public EnchantmentBEBehavior(BlockEntity be) : base(be)
        {
            
        }
        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
        }
    }
}