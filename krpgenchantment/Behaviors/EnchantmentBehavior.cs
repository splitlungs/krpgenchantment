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

namespace KRPGLib.Enchantment
{
    /// <summary>
    /// Holds all Enchantment data at runtime.
    /// </summary>
    public class EnchantmentProperties
    {
        public string Enchantable = "enchantable";
        public string ChillingID = "chilling";
        public string FlamingID = "flaming";
        public string FrostID = "frost";
        public string HarmingID = "harming";
        public string HealingID = "healing";
        public string IgnitingID = "igniting";
        public string KnockbackID = "knockback";
        public string LightID = "light";
        public string LightningID = "lightning";
        public string PitID = "pit";
        public string ShockingID = "shocking";

        public bool EnchantableVal = false;
        public int ChillingVal = 0;
        public int FlamingVal = 0;
        public int FrostVal = 0;
        public int HarmingVal = 0;
        public int HealingVal = 0;
        public int IgnitingVal = 0;
        public int KnockbackVal = 0;
        public int LightVal = 0;
        public int LightningVal = 0;
        public int PitVal = 0;
        public int ShockingVal = 0;

        /// <summary>
        /// Returns a copy.
        /// </summary>
        /// <returns></returns>
        public EnchantmentProperties Clone()
        {
            return new EnchantmentProperties()
            {
                Enchantable = Enchantable,
                ChillingID = ChillingID,
                FlamingID = FlamingID,
                FrostID = FrostID,
                HarmingID = HarmingID,
                HealingID = HealingID,
                IgnitingID = IgnitingID,
                KnockbackID = KnockbackID,
                LightID = LightID,
                LightningID = LightningID,
                PitID = PitID,
                ShockingID = ShockingID,

                EnchantableVal = EnchantableVal,
                ChillingVal = ChillingVal,
                FlamingVal = FlamingVal,
                FrostVal = FrostVal,
                HarmingVal = HarmingVal,
                HealingVal = HealingVal,
                IgnitingVal = IgnitingVal,
                KnockbackVal = KnockbackVal,
                LightVal = LightVal,
                LightningVal = LightningVal,
                PitVal = PitVal,
                ShockingVal = ShockingVal
            };
        }
    }

    public class EnchantmentBehavior : CollectibleBehavior
    {
        #region Data
        public ICoreAPI Api { get; protected set; }
        /// <summary>
        /// Class for storing default enchantment configuration. Do not save your active enchantments here.
        /// </summary>
        public EnchantmentProperties EnchantProps { get; protected set; }
        // public ITreeAttribute Enchantments { get; protected set; }
        public EnchantmentBehavior(CollectibleObject collObj) : base(collObj)
        {
            this.EnchantProps = new EnchantmentProperties();
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
            EnchantProps = properties.AsObject<EnchantmentProperties>(null, collObj.Code.Domain);
        }
        /// <summary>
        /// Applies default JSON properties to EnchantProps.
        /// </summary>
        /// <param name="properties"></param>
        public virtual void GetProperties(JsonObject properties)
        {
            EnchantProps.EnchantableVal = properties["enchantable"].AsBool(false);
            EnchantProps.ChillingVal = properties["chilling"].AsInt(0);
            EnchantProps.FlamingVal = properties["flaming"].AsInt(0);
            EnchantProps.FrostVal = properties["frost"].AsInt(0);
            EnchantProps.HarmingVal = properties["harming"].AsInt(0);
            EnchantProps.HealingVal = properties["healing"].AsInt(0);
            EnchantProps.IgnitingVal = properties["igniting"].AsInt(0);
            EnchantProps.KnockbackVal = properties["knockback"].AsInt(0);
            EnchantProps.LightVal = properties["light"].AsInt(0);
            EnchantProps.LightningVal = properties["lightning"].AsInt(0);
            EnchantProps.PitVal = properties["pit"].AsInt(0);
            EnchantProps.ShockingVal = properties["shocking"].AsInt(0);
        }
        /// <summary>
        /// Save all EnchantProps to ItemStack's Attributes.
        /// </summary>
        /// <param name="itemStack"></param>
        public void SetAttributesFromProps(ItemStack itemStack)
        {
            itemStack.Attributes.GetOrAddTreeAttribute("enchantments");

            itemStack.Attributes.SetBool(EnchantProps.Enchantable, EnchantProps.EnchantableVal);
            itemStack.Attributes.SetInt(EnchantProps.ChillingID, EnchantProps.ChillingVal);
            itemStack.Attributes.SetInt(EnchantProps.FlamingID, EnchantProps.FlamingVal);
            itemStack.Attributes.SetInt(EnchantProps.FrostID, EnchantProps.FrostVal);
            itemStack.Attributes.SetInt(EnchantProps.HarmingID, EnchantProps.HarmingVal);
            itemStack.Attributes.SetInt(EnchantProps.HealingID, EnchantProps.HealingVal);
            itemStack.Attributes.SetInt(EnchantProps.IgnitingID, EnchantProps.IgnitingVal);
            itemStack.Attributes.SetInt(EnchantProps.KnockbackID, EnchantProps.KnockbackVal);
            itemStack.Attributes.SetInt(EnchantProps.LightID, EnchantProps.LightVal);
            itemStack.Attributes.SetInt(EnchantProps.LightningID, EnchantProps.LightningVal);
            itemStack.Attributes.SetInt(EnchantProps.PitID, EnchantProps.PitVal);
            itemStack.Attributes.SetInt(EnchantProps.ShockingID, EnchantProps.ShockingVal);
        }
        /// <summary>
        /// Gets Enchantment attributes from the ItemStack and writes to Enchant Properties
        /// </summary>
        /// <param name="itemStack"></param>
        public void GetAttributes(ItemStack itemStack)
        {
            var attr = itemStack.Attributes.GetOrAddTreeAttribute("enchantments");
            
            EnchantProps.EnchantableVal = itemStack.Attributes.GetBool(EnchantProps.Enchantable, false);
            EnchantProps.ChillingVal = itemStack.Attributes.GetInt(EnchantProps.ChillingID, 0);
            EnchantProps.FlamingVal = itemStack.Attributes.GetInt(EnchantProps.FlamingID, 0);
            EnchantProps.FrostVal = itemStack.Attributes.GetInt(EnchantProps.FrostID, 0);
            EnchantProps.HarmingVal = itemStack.Attributes.GetInt(EnchantProps.HarmingID, 0);
            EnchantProps.HealingVal = itemStack.Attributes.GetInt(EnchantProps.HealingID, 0);
            EnchantProps.IgnitingVal = itemStack.Attributes.GetInt(EnchantProps.IgnitingID, 0);
            EnchantProps.KnockbackVal = itemStack.Attributes.GetInt(EnchantProps.KnockbackID, 0);
            EnchantProps.LightVal = itemStack.Attributes.GetInt(EnchantProps.LightID, 0);
            EnchantProps.LightningVal = itemStack.Attributes.GetInt(EnchantProps.LightningID, 0);
            EnchantProps.PitVal = itemStack.Attributes.GetInt(EnchantProps.PitID, 0);
            EnchantProps.ShockingVal = itemStack.Attributes.GetInt(EnchantProps.ShockingID, 0);
        }
        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            // Get Enchantments
            Dictionary<string, int> enchants = new Dictionary<string, int>();
            foreach (var val in Enum.GetValues(typeof(EnumEnchantments)))
            {
                int ePower = inSlot.Itemstack.Attributes.GetInt(val.ToString(), 0);
                if (ePower > 0) { enchants.Add(val.ToString(), ePower); }
            }
            // Write to Description
            foreach (KeyValuePair<string, int> pair in enchants)
            {
                dsc.AppendLine(string.Format("<font color=\"cyan\">" + Lang.Get("krpgenchantment:enchantment-" + pair.Key + "-" + pair.Value) + "</font>"));
            }
        }
        #endregion
    }
}
