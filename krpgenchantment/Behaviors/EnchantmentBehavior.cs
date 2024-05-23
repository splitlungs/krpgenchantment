using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Newtonsoft.Json;
using HarmonyLib;
using System.ComponentModel;
using System.Diagnostics.Tracing;
using System.Reflection.Metadata;
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
        public ICoreAPI Api { get; protected set; }
        protected AssetLocation strikeSound;
        public EnumHandInteract strikeSoundHandInteract = EnumHandInteract.HeldItemAttack;
        /// <summary>
        /// Class for storing default enchantment configuration. Do not save your active enchantments here.
        /// </summary>
        public EnchantmentProperties EnchantProps { get; protected set; }
        public ITreeAttribute Enchantments { get; protected set; }
        public EnchantmentBehavior(CollectibleObject collObj) : base(collObj)
        {
            this.EnchantProps = new EnchantmentProperties();
        }
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            Api = api;
            ICoreServerAPI sApi = api as ICoreServerAPI;
        }
        public IEnumerable<Type> FindDerivedTypes(Assembly assembly, Type baseType)
        {
            return assembly.GetTypes().Where(t => baseType.IsAssignableFrom(t));
        }
        #region Data
        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            EnchantProps = properties.AsObject<EnchantmentProperties>(null, collObj.Code.Domain);
        }
        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot, ref EnumHandling handling)
        {
            handling = EnumHandling.PassThrough;
            return new WorldInteraction[]
            {
                new WorldInteraction
                {
                    //HotKeyCodes = StorageProps.SprintKey ? new string[] {"ctrl", "shift" } : new string[] {"shift"},
                    ActionLangCode = "heldhelp-enchantment",
                    MouseButton = EnumMouseButton.Right
                }
            };
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

            int power = 0;
            // Check Attributes
            if (inSlot.Itemstack.Attributes.GetBool("enchantable", false) == true)
                dsc.AppendLine(string.Format("<font color=\"green\">" + Lang.Get("krpgenchantment:krpg-enchantable") + "</font>"));
            power = inSlot.Itemstack.Attributes.GetInt("chilling", 0);
            if (power > 0)
                dsc.AppendLine(string.Format("<font color=\"cyan\">" + Lang.Get("krpgenchantment:enchantment-chilling-" + power) + "</font>"));
            power = inSlot.Itemstack.Attributes.GetInt("flaming", 0);
            if (power > 0)
                dsc.AppendLine(string.Format("<font color=\"cyan\">" + Lang.Get("krpgenchantment:enchantment-flaming-" + power) + "</font>"));
            power = inSlot.Itemstack.Attributes.GetInt("frost", 0);
            if (power > 0)
                dsc.AppendLine(string.Format("<font color=\"cyan\">" + Lang.Get("krpgenchantment:enchantment-frost-" + power) + "</font>"));
            power = inSlot.Itemstack.Attributes.GetInt("harming", 0);
            if (power > 0)
                dsc.AppendLine(string.Format("<font color=\"cyan\">" + Lang.Get("krpgenchantment:enchantment-harming-" + power) + "</font>"));
            power = inSlot.Itemstack.Attributes.GetInt("healing", 0);
            if (power > 0)
                dsc.AppendLine(string.Format("<font color=\"cyan\">" + Lang.Get("krpgenchantment:enchantment-healing-" + power) + "</font>"));
            power = inSlot.Itemstack.Attributes.GetInt("igniting", 0);
            if (power > 0)
                dsc.AppendLine(string.Format("<font color=\"cyan\">" + Lang.Get("krpgenchantment:enchantment-igniting-" + power) + "</font>"));
            power = inSlot.Itemstack.Attributes.GetInt("knockback", 0);
            if (power > 0)
                dsc.AppendLine(string.Format("<font color=\"cyan\">" + Lang.Get("krpgenchantment:enchantment-knockback-" + power) + "</font>"));
            power = inSlot.Itemstack.Attributes.GetInt("light", 0);
            if (power > 0)
                dsc.AppendLine(string.Format("<font color=\"cyan\">" + Lang.Get("krpgenchantment:enchantment-light-" + power) + "</font>"));
            power = inSlot.Itemstack.Attributes.GetInt("lightning", 0);
            if (power > 0)
                dsc.AppendLine(string.Format("<font color=\"cyan\">" + Lang.Get("krpgenchantment:enchantment-lightning-" + power) + "</font>"));
            power = inSlot.Itemstack.Attributes.GetInt("shocking", 0);
            if (power > 0)
                dsc.AppendLine(string.Format("<font color=\"cyan\">" + Lang.Get("krpgenchantment:enchantment-shocking-" + power) + "</font>"));
            power = inSlot.Itemstack.Attributes.GetInt("pit", 0);
            if (power > 0)
                dsc.AppendLine(string.Format("<font color=\"cyan\">" + Lang.Get("krpgenchantment:enchantment-pit-" + power) + "</font>"));
        }
        #endregion
        #region Effects
        /// <summary>
        /// Reduce the Entity's BodyTemperature to -10, multiplied by Power
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="power"></param>
        public void ChillEntity(Entity entity, int power)
        {
            EntityBehaviorBodyTemperature ebbt = entity.GetBehavior<EntityBehaviorBodyTemperature>();

            // If we encounter something without one, bail
            if (ebbt == null)
                return;

            ebbt.CurBodyTemperature = power * -10f;
        }
        /// <summary>
        /// Creates a 1x1x1 pit at the Pos, multiplied by Power. Only works only Soil, Sand, or Gravel
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="byEntity"></param>
        /// <param name="power"></param>
        public void CreatePit(EntityPos pos, Entity byEntity, int power)
        {
            BlockPos bpos = pos.AsBlockPos;
            List<Vec3d> pitArea = new List<Vec3d>();

            for (int x = 0; x <= power; x++)
            {
                for (int y = 0; y <= power; y++)
                {
                    for (int z = 0; z <= power; z++)
                    {
                        pitArea.Add(new Vec3d(bpos.X + x, bpos.Y - y, bpos.Z + z));
                        pitArea.Add(new Vec3d(bpos.X - x, bpos.Y - y, bpos.Z - z));
                        pitArea.Add(new Vec3d(bpos.X + x, bpos.Y - y, bpos.Z - z));
                        pitArea.Add(new Vec3d(bpos.X - x, bpos.Y - y, bpos.Z + z));
                    }
                }
            }

            for (int i = 0; i < pitArea.Count; i++)
            {
                BlockPos ipos = bpos;
                ipos.Set(pitArea[i]);
                Block block = byEntity.World.BlockAccessor.GetBlock(ipos);

                if (block != null)
                {
                    string blockCode = block.Code.ToString();
                    if (blockCode.Contains("soil") || blockCode.Contains("sand") || blockCode.Contains("gravel"))
                        byEntity.World.BlockAccessor.BreakBlock(ipos, byEntity as IPlayer);
                }
            }
        }
        /// <summary>
        /// Attempts to slay the target instantly. Chance of success is multiplied by Power
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="byEntity"></param>
        /// <param name="power"></param>
        public void DeathPoison(Entity entity, DamageSource byEntity, int power)
        {
            Api.Event.TriggerEntityDeath(entity, byEntity);
        }
        /// <summary>
        /// Attempt to set the target on fire. Power not doing anything currently.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="power"></param>
        public void IgniteEntity(Entity entity, int power)
        {
            entity.IsOnFire = true;
            
        }
        /// <summary>
        /// Create a lightning strike at Pos. Power not doing anything currently
        /// </summary>
        /// <param name="world"></param>
        /// <param name="pos"></param>
        public void CallLightning(IWorldAccessor world, Vec3d pos, int power)
        {
            WeatherSystemServer weatherSystem = world.Api.ModLoader.GetModSystem<WeatherSystemServer>();
            // It should default to 0f. Stun should stop at 0.5. Absorbtion should start at 1f.
            if (weatherSystem != null)
                weatherSystem.SpawnLightningFlash(pos);
            else
                world.Api.Logger.Debug("Could not find Weather System!");
        }
        #endregion
    }
}
