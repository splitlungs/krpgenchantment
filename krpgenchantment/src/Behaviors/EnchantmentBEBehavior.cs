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
        // public bool IsPlayerPlaced { get; private set; } = false;
        public EnchantmentBEBehavior(BlockEntity be) : base(be)
        {
            
        }
        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            // IsPlayerPlaced = tree.GetBool("IsPlayerPlaced", false);
            // Api.Logger.Event("[KRPGEnchantment] Setting block IsPlayerPlaced to {0}.", IsPlayerPlaced);
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            // tree.SetBool("IsPlayerPlaced", IsPlayerPlaced);
            // Api.Logger.Event("[KRPGEnchantment] Setting block IsPlayerPlaced to {0}.", IsPlayerPlaced);
        }
        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(byItemStack);
            // IsPlayerPlaced = true;
        }
    }
}