using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;
using System.Reflection;
using System.Collections;
using Vintagestory.API.Common.Entities;
using System.Threading.Tasks;

namespace KRPGLib.Enchantment
{
    public class ReagentBehavior : CollectibleBehavior
    {
        public ICoreAPI Api;
        public string Potential;
        /// <summary>
        /// Handles all Reagent generation
        /// </summary>
        public ReagentBehavior(CollectibleObject collObj) : base(collObj)
        {
            Potential = collObj.Attributes?["potential"].ToString();
        }
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            Api = api;
        }
        public IEnumerable<Type> FindDerivedTypes(Assembly assembly, Type baseType)
        {
            return assembly.GetTypes().Where(t => baseType.IsAssignableFrom(t));
        }
        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            
        }
        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            if (!EnchantingConfigLoader.Config.ValidReagents.ContainsKey(collObj.Code)) return;

            ITreeAttribute tree = inSlot.Itemstack.Attributes?.GetOrAddTreeAttribute("enchantments");
            if (tree == null) return;

            int p = tree.GetInt("potential");

            // p = entity.Api.AssessReagent(stack);

            dsc.Append("<font color=\"" + Enum.GetName(typeof(EnchantColors), p) + "\">" + Lang.Get("krpgenchantment:reagent-potential") + ": " + p);
            if (!EnchantingConfigLoader.Config.ValidReagents.ContainsKey(collObj.Code)) return;
        }
        void RollPotential(ItemStack itemStack)
        {
            if (Api.Side != EnumAppSide.Server)
                return;

            ITreeAttribute tree = itemStack.Attributes.GetOrAddTreeAttribute("enchantingPotential");
        }
    }
}
