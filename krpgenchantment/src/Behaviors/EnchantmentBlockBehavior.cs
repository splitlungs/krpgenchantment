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
using Vintagestory.API.Util;

namespace KRPGLib.Enchantment
{
    /// <summary>
    /// Class for storing default enchantment data at runtime. Do not save your active enchantments here.
    /// </summary>
    public class EnchantmentBlockBehavior : BlockBehavior
    {
         
        public ICoreAPI Api;
        public ICoreServerAPI sApi;
        public Dictionary<string, int> Enchantments = null;
        public bool Enchantable = false;
        public bool IsReagent = false;
        public float DropsMul = 1f;
        public EnchantmentBlockBehavior(Block block) : base(block)
        {
        }
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            Api = api;
            // We only load the config on the server, so check side first
            if (api.Side != EnumAppSide.Server) return;
            sApi = api as ICoreServerAPI;
            if (EnchantingConfigLoader.Config?.ValidReagents.ContainsKey(collObj.Code) == true) 
                IsReagent = true;
            // Configure the Fortunate multiplier
            IEnchantment ench = sApi.EnchantAccessor().GetEnchantment("fortunate");
            DropsMul = ench.Modifiers.GetFloat("PowerMultiplier");
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
        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref float dropChanceMultiplier, ref EnumHandling handling)
        {
            handling = EnumHandling.Handled;
            if (byPlayer?.InventoryManager?.ActiveHotbarSlot == null || block.Attributes == null) 
                return base.GetDrops(world, pos, byPlayer, ref dropChanceMultiplier, ref handling);
            
            if (EnchantingConfigLoader.Config.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] Checking for Fortunate enchantment. Pre dropChanceMultiplier is {0}", dropChanceMultiplier);
            handling = EnumHandling.Handled;
            ItemStack itemstack = byPlayer?.InventoryManager?.ActiveHotbarSlot?.Itemstack;
            Dictionary<string, int> enchants = Api?.EnchantAccessor()?.GetActiveEnchantments(itemstack);
            if (enchants?.TryGetValue("fortunate", out int power) == true)
            {
                float eMul = GetFortunateMul(itemstack, power);
                dropChanceMultiplier *= MathF.Min(eMul, 1.99f); // Never go 1.0 or above to avoide true dupe
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] Applied an Fortunate enchantment. Post dropChanceMultiplier is {0}.", dropChanceMultiplier);         
            }
       
            return base.GetDrops(world, pos, byPlayer, ref dropChanceMultiplier, ref handling);
        }
        public float GetFortunateMul(ItemStack itemstack, int power)
        {
            bool valid = false;
            EnumTool? tool = itemstack.Item.Tool;
            switch (tool)
            {
                case EnumTool.Axe:
                    if (IsTree() == true)
                        valid = true;
                    break;
                case EnumTool.Drill:
                    if (block.BlockMaterial == EnumBlockMaterial.Ore || block.Code.FirstCodePart() == "rock")
                        valid = true;
                    break;
                case EnumTool.Knife:
                    if (block.BlockMaterial == EnumBlockMaterial.Plant)
                        valid = true;
                    break;
                case EnumTool.Pickaxe:
                    if (block.BlockMaterial == EnumBlockMaterial.Ore || block.Code.FirstCodePart() == "rock")
                        valid = true;
                    break;
                case EnumTool.Scythe:
                    if (block.BlockMaterial == EnumBlockMaterial.Plant)
                        valid = true;
                    break;
                case EnumTool.Shears:
                    if (block.Code.FirstCodePart().ContainsFast("leaves"))
                        valid = true;
                    break;
                case EnumTool.Shovel:
                    if (block.BlockMaterial == EnumBlockMaterial.Soil || block.BlockMaterial == EnumBlockMaterial.Sand || block.BlockMaterial == EnumBlockMaterial.Gravel)
                        valid = true;
                    break;
                default:
                    break;
            }
            float dMul = 1.0f;
            if (valid)
                dMul += power * DropsMul;
            return dMul;
        }
        public bool IsTree()
        {
            if (!block.Code.Path.ContainsFast("log"))
                return false;
            if (!block.Code.Path.Contains("placed"))
                return true;
            return false;
        }
    }
}