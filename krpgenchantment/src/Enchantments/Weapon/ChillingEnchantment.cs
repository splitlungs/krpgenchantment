using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using KRPGLib.Enchantment.API;

namespace KRPGLib.Enchantment
{
    public class ChillingEnchantment : Enchantment
    {
        public ChillingEnchantment(ICoreAPI api) : base(api)
        {
            Enabled = true;
            Code = "chilling";
            LoreCode = "enchantment-chilling";
            LoreChapterID = 0;
            MaxTier = 5;
            Modifiers = new object[1] { -10 };
        }
        public override void OnAttack(EnchantmentSource enchant, ItemSlot slot, ref float? damage)
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] {0} is being affected by a chilling enchantment.", enchant.TargetEntity.GetName());
            
            EntityBehaviorBodyTemperature ebbt = enchant.TargetEntity.GetBehavior<EntityBehaviorBodyTemperature>();
            if (ebbt != null)
                ebbt.CurBodyTemperature = enchant.Power * (float)Modifiers[0];
        }
    }
}
