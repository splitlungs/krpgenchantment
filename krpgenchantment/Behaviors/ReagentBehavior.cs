using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;
using System.Reflection;

namespace KRPGLib.Enchantment
{
    public class ReagentBehavior : CollectibleBehavior
    {
        public ICoreAPI Api;
        public string Potential;
        /// <summary>
        /// Class for storing default enchantment configuration. Do not save your active enchantments here.
        /// </summary>
        public ReagentBehavior(CollectibleObject collObj) : base(collObj)
        {

        }
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            Api = api;

            Potential = this.collObj.Attributes?["enchantmentPotential"].ToString();
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

            Dictionary<string, int> enchants = Api.GetEnchantments(inSlot.Itemstack);
            if (enchants != null)
            {
                foreach (KeyValuePair<string, int> pair in enchants)
                    dsc.AppendLine(string.Format("<font color=\"" + Enum.GetName(typeof(EnchantColors), pair.Value) + "\">" + Lang.Get("krpgenchantment:enchantment-" + pair.Key.ToString()) + " " + Lang.Get("krpgenchantment:" + pair.Value) + "</font>"));
            }
        }
    }
}
