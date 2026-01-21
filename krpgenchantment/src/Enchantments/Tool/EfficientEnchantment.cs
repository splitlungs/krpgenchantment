using System;
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
using Vintagestory.API.Server;

namespace KRPGLib.Enchantment
{
    public class EfficientEnchantment : Enchantment
    {
        float PowerMultiplier { get { return Modifiers.GetFloat("PowerMultiplier"); } }
        /// <summary>
        /// Increases the gathering speed of the enchanted item.
        /// </summary>
        /// <param name="api"></param>
        public EfficientEnchantment(ICoreAPI api) : base(api)
        {
            // Setup the default config
            Enabled = false;
            Code = "efficient";
            Category = "Gathering";
            LoreCode = "enchantment-efficient";
            LoreChapterID = 20;
            MaxTier = 5;
            ValidToolTypes = new List<string>() {
                "Chisel", "Cleaver", "Hammer", "Hoe", "Meter", "Pickaxe", "Probe", "Saw", "Scythe", "Shears", "Shovel", "Sickle", "Wrench",
                "Knife", "Axe",
                "Drill",
                };
            Modifiers = new EnchantModifiers { { "PowerMultiplier", 0.20f } };
            Version = 1.00f;
        }
        public override void OnEquip(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            // int toolTierPlus = enchant.SourceSlot.Itemstack.Item.ToolTier + enchant.Power;
            // enchant.SourceSlot.Itemstack.Item.GetMiningSpeed
            ICoreServerAPI sApi = Api as ICoreServerAPI;
            EntityPlayer entity = sApi.World.GetEntityById(enchant.CauseEntity.EntityId) as EntityPlayer;
            ItemStack stack = enchant.SourceStack;
            bool doEquip = parameters.GetBool("equip");
            float traitRate = 0f;
            if (doEquip == true)
            {
        
                traitRate = entity.Stats.GetBlended("miningSpeedMul");
                if (EnchantingConfigLoader.Config.Debug == true)
                    sApi.Logger.Event("[KRPGEnchantment] Equipping an Efficient enchantment. Pre-equip MiningSpeedMul is {0}.", traitRate);
                entity.Stats.Set("miningSpeedMul", "KRPGMSMul", enchant.Power, true);
                // stack.Item.GetMiningSpeed
            }
            else
                entity.Stats.Set("miningSpeedMul", "KRPGMSMul", 1f, true);
        
            traitRate = entity.Stats.GetBlended("miningSpeedMul");
            if (EnchantingConfigLoader.Config.Debug == true)
                sApi.Logger.Event("[KRPGEnchantment] Post-equip MiningSpeedMul is {0}.", traitRate);
        }
    }
}
