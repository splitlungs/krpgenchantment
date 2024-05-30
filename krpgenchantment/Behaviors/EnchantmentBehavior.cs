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
        #region Data
        public ICoreAPI Api { get; protected set; }
        private string aimAnimation;
        // protected AssetLocation strikeSound;
        // public EnumHandInteract strikeSoundHandInteract = EnumHandInteract.HeldItemAttack;
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
            aimAnimation = collObj.Attributes["aimAnimation"].AsString();
            // ICoreServerAPI sApi = api as ICoreServerAPI;
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

            /*
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
            */
        }
        #endregion
        #region Interract
        /*
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
        {
            EnumTool tool = slot.Itemstack.Item.Tool.Value;
            byEntity.Api.Logger.Event("Tool is being used");
            if (tool == EnumTool.Bow)
            {
                byEntity.Api.Logger.Event("Intercepting a Bow Attack");
                if (GetNextArrow(byEntity) != null)
                {
                    if (byEntity.World is IClientWorldAccessor)
                    {
                        slot.Itemstack.TempAttributes.SetInt("renderVariant", 1);
                    }

                    slot.Itemstack.Attributes.SetInt("renderVariant", 1);
                    byEntity.Attributes.SetInt("aiming", 1);
                    byEntity.Attributes.SetInt("aimingCancel", 0);
                    byEntity.AnimManager.StartAnimation(aimAnimation);
                    IPlayer dualCallByPlayer = null;
                    if (byEntity is EntityPlayer)
                    {
                        dualCallByPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
                    }

                    byEntity.World.PlaySoundAt(new AssetLocation("game:sounds/bow-draw"), byEntity, dualCallByPlayer, randomizePitch: false, 8f);
                    handHandling = EnumHandHandling.PreventDefault;
                    handling = EnumHandling.Handled;
                }
            }
            else if (tool == EnumTool.Spear)
            {
                byEntity.Api.Logger.Event("Intercepting a Spear Attack");
                if (handHandling != EnumHandHandling.PreventDefault)
                {
                    handHandling = EnumHandHandling.PreventDefault;
                    byEntity.Attributes.SetInt("aiming", 1);
                    byEntity.Attributes.SetInt("aimingCancel", 0);
                    byEntity.StartAnimation("aim");
                }
                handling = EnumHandling.Handled;
            }
            
            else if (tool == EnumTool.Wand)
            {
                WandAttack(secondsUsed, slot, byEntity);
                handling = EnumHandling.PreventSubsequent;

                handling = EnumHandling.Handled;
            }
            else
            {
                handling = EnumHandling.Handled;
            }

            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling);
        }
        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
        {
            return base.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel, ref handling);
        }
        public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason, ref EnumHandling handled)
        {
            return base.OnHeldInteractCancel(secondsUsed, slot, byEntity, blockSel, entitySel, cancelReason, ref handled);
        }
        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
        {
            handling = EnumHandling.PreventDefault;

            EnumTool tool = slot.Itemstack.Item.Tool.Value;
            byEntity.Api.Logger.Event("Tool is being used");
            if (tool == EnumTool.Bow)
            {
                byEntity.Api.Logger.Event("Intercepting a Bow Attack Stop");
                BowAttack(secondsUsed, slot, byEntity);
                handling = EnumHandling.PreventSubsequent;
            }
            else if (tool == EnumTool.Spear)
            {
                byEntity.Api.Logger.Event("Intercepting a Spear Attack Stop");
                SpearAttack(secondsUsed, slot, byEntity);
                handling = EnumHandling.PreventSubsequent;
            }

            else if (tool == EnumTool.Wand)
            {
                WandAttack(secondsUsed, slot, byEntity);
                handling = EnumHandling.PreventSubsequent;
                
                handling = EnumHandling.Handled;
            }

            base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel, ref handling);
        }
        public override void OnHeldAttackStop(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel, ref EnumHandling handling)
        {
            handling = EnumHandling.PreventDefault;

            EnumTool tool = slot.Itemstack.Item.Tool.Value;
            byEntity.Api.Logger.Event("Tool is being used");
            if (tool == EnumTool.Bow)
            {
                byEntity.Api.Logger.Event("Intercepting a Bow Attack Stop");
                BowAttack(secondsPassed, slot, byEntity);
                handling = EnumHandling.PreventSubsequent;
            }
            else if (tool == EnumTool.Spear)
            {
                byEntity.Api.Logger.Event("Intercepting a Spear Attack Stop");
                SpearAttack(secondsPassed, slot, byEntity);
                handling = EnumHandling.PreventSubsequent;
            }
            base.OnHeldAttackStop(secondsPassed, slot, byEntity, blockSelection, entitySel, ref handling);
        }
        private void BowAttack(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
        {

            if (byEntity.Attributes.GetInt("aimingCancel") == 1)
            {
                return;
            }

            string aimAnimation = slot.Itemstack.Collectible.Attributes["aimAnimation"].AsString();

            byEntity.Attributes.SetInt("aiming", 0);
            byEntity.AnimManager.StopAnimation(aimAnimation);
            if (byEntity.World is IClientWorldAccessor)
            {
                slot.Itemstack.TempAttributes.RemoveAttribute("renderVariant");
            }

            slot.Itemstack.Attributes.SetInt("renderVariant", 0);
            (byEntity as EntityPlayer)?.Player?.InventoryManager.BroadcastHotbarSlot();
            if (secondsUsed < 0.65f)
            {
                return;
            }

            ItemSlot nextArrow = null;
            byEntity.WalkInventory(delegate (ItemSlot invslot)
            {
                if (invslot is ItemSlotCreative)
                {
                    return true;
                }

                if (invslot.Itemstack != null && invslot.Itemstack.Collectible.Code.PathStartsWith("arrow-"))
                {
                    nextArrow = invslot;
                    return false;
                }

                return true;
            });

            if (nextArrow != null)
            {
                float num = 0f;
                float num2 = 0f;
                if (slot.Itemstack.Collectible.Attributes != null)
                {
                    num += slot.Itemstack.Collectible.Attributes["damage"].AsFloat();
                    num2 = 1f - slot.Itemstack.Collectible.Attributes["accuracyBonus"].AsFloat();
                }

                if (nextArrow.Itemstack.Collectible.Attributes != null)
                {
                    num += nextArrow.Itemstack.Collectible.Attributes["damage"].AsFloat();
                }

                // Get Enchantments
                Dictionary<string, int> enchants = new Dictionary<string, int>();
                foreach (var val in Enum.GetValues(typeof(EnumEnchantments)))
                {
                    int ePower = slot.Itemstack.Attributes.GetInt(val.ToString(), 0);
                    if (ePower > 0)
                    {
                        enchants.Add(val.ToString(), ePower);
                        byEntity.Api.Logger.Event("Found {0} on {1} before throw.", val.ToString(), collObj.ItemClass.ToString());
                    }
                }

                ItemStack itemStack = nextArrow.TakeOut(1);
                nextArrow.MarkDirty();
                IPlayer dualCallByPlayer = null;
                if (byEntity is EntityPlayer)
                {
                    dualCallByPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
                }

                byEntity.World.PlaySoundAt(new AssetLocation("game:sounds/bow-release"), byEntity, dualCallByPlayer, randomizePitch: false, 8f);
                float num3 = 0.5f;
                if (itemStack.ItemAttributes != null)
                {
                    num3 = itemStack.ItemAttributes["breakChanceOnImpact"].AsFloat(0.5f);
                }

                EntityProperties entityType = byEntity.World.GetEntityType(new AssetLocation("krpgenchantment", "enchanted-arrow-" + itemStack.Collectible.Variant["material"]));
                EntityProjectile entityProjectile = byEntity.World.ClassRegistry.CreateEntity(entityType) as EntityProjectile;
                entityProjectile.FiredBy = byEntity;
                entityProjectile.Damage = num;
                entityProjectile.ProjectileStack = itemStack;
                entityProjectile.DropOnImpactChance = 1f - num3;
                float num4 = Math.Max(0.001f, 1f - byEntity.Attributes.GetFloat("aimingAccuracy"));
                double num5 = byEntity.WatchedAttributes.GetDouble("aimingRandPitch", 1.0) * (double)num4 * (0.75 * (double)num2);
                double num6 = byEntity.WatchedAttributes.GetDouble("aimingRandYaw", 1.0) * (double)num4 * (0.75 * (double)num2);
                Vec3d vec3d = byEntity.ServerPos.XYZ.Add(0.0, byEntity.LocalEyePos.Y, 0.0);
                Vec3d pos = (vec3d.AheadCopy(1.0, (double)byEntity.SidedPos.Pitch + num5, (double)byEntity.SidedPos.Yaw + num6) - vec3d) * byEntity.Stats.GetBlended("bowDrawingStrength");
                entityProjectile.ServerPos.SetPos(byEntity.SidedPos.BehindCopy(0.21).XYZ.Add(0.0, byEntity.LocalEyePos.Y, 0.0));
                entityProjectile.ServerPos.Motion.Set(pos);
                entityProjectile.Pos.SetFrom(entityProjectile.ServerPos);
                entityProjectile.World = byEntity.World;
                entityProjectile.SetRotation();

                // Pass Enchantment Attributes to the Projectile
                foreach (KeyValuePair<string, int> pair in enchants)
                {
                    entityProjectile.WatchedAttributes.SetInt(pair.Key, pair.Value);
                    byEntity.Api.Logger.Event("Found {0} on ItemSpear before throw.", pair.Key.ToString());
                }

                byEntity.World.SpawnEntity(entityProjectile);
                slot.Itemstack.Collectible.DamageItem(byEntity.World, byEntity, slot);
                byEntity.AnimManager.StartAnimation("bowhit");
            }
        }
        protected ItemSlot GetNextArrow(EntityAgent byEntity)
        {
            ItemSlot slot = null;
            byEntity.WalkInventory(delegate (ItemSlot invslot)
            {
                if (invslot is ItemSlotCreative)
                {
                    return true;
                }

                if (invslot.Itemstack != null && invslot.Itemstack.Collectible.Code.PathStartsWith("arrow-"))
                {
                    slot = invslot;
                    return false;
                }

                return true;
            });
            return slot;
        }
        protected void SpearAttack(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
        {
            if (byEntity.Attributes.GetInt("aimingCancel") == 1)
            {
                return;
            }

            byEntity.Attributes.SetInt("aiming", 0);
            byEntity.StopAnimation("aim");
            if (secondsUsed < 0.35f)
            {
                return;
            }

            float damage = 1.5f;
            if (slot.Itemstack.Collectible.Attributes != null)
            {
                damage = slot.Itemstack.Collectible.Attributes["damage"].AsFloat();
            }

            // Get Enchantments
            Dictionary<string, int> enchants = new Dictionary<string, int>();
            foreach (var val in Enum.GetValues(typeof(EnumEnchantments)))
            {
                int ePower = slot.Itemstack.Attributes.GetInt(val.ToString(), 0);
                if (ePower > 0)
                {
                    enchants.Add(val.ToString(), ePower);
                    byEntity.Api.Logger.Event("Found {0} on {1} before throw.", val.ToString(), collObj.ItemClass.ToString());
                }
            }

            (byEntity.Api as ICoreClientAPI)?.World.AddCameraShake(0.17f);
            ItemStack projectileStack = slot.TakeOut(1);
            slot.MarkDirty();
            IPlayer player = null;
            if (byEntity is EntityPlayer)
            {
                player = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
            }

            byEntity.World.PlaySoundAt(new AssetLocation("game:sounds/player/throw"), byEntity, player, randomizePitch: false, 8f);
            EntityProperties entityType = byEntity.World.GetEntityType(new AssetLocation("krpgenchantment", "enchanted-" + slot.Itemstack.Collectible.Attributes["spearEntityCode"].AsString()));
            EnchantedEntityProjectile entityProjectile = byEntity.World.ClassRegistry.CreateEntity(entityType) as EnchantedEntityProjectile;
            entityProjectile.FiredBy = byEntity;
            entityProjectile.Damage = damage;
            entityProjectile.ProjectileStack = projectileStack;
            entityProjectile.DropOnImpactChance = 1.1f;
            entityProjectile.DamageStackOnImpact = true;
            entityProjectile.Weight = 0.3f;
            float num = 1f - byEntity.Attributes.GetFloat("aimingAccuracy");
            double num2 = byEntity.WatchedAttributes.GetDouble("aimingRandPitch", 1.0) * (double)num * 0.75;
            double num3 = byEntity.WatchedAttributes.GetDouble("aimingRandYaw", 1.0) * (double)num * 0.75;
            Vec3d vec3d = byEntity.ServerPos.XYZ.Add(0.0, byEntity.LocalEyePos.Y - 0.2, 0.0);
            Vec3d pos = (vec3d.AheadCopy(1.0, (double)byEntity.ServerPos.Pitch + num2, (double)byEntity.ServerPos.Yaw + num3) - vec3d) * 0.65;
            Vec3d pos2 = byEntity.ServerPos.BehindCopy(0.21).XYZ.Add(byEntity.LocalEyePos.X, byEntity.LocalEyePos.Y - 0.2, byEntity.LocalEyePos.Z);
            entityProjectile.ServerPos.SetPos(pos2);
            entityProjectile.ServerPos.Motion.Set(pos);
            entityProjectile.Pos.SetFrom(entityProjectile.ServerPos);
            entityProjectile.World = byEntity.World;
            entityProjectile.SetRotation();

            // Pass Enchantment Attributes to the Projectile
            foreach (KeyValuePair<string, int> pair in enchants)
            {
                entityProjectile.WatchedAttributes.SetInt(pair.Key, pair.Value);
                byEntity.Api.Logger.Event("Found {0} on ItemSpear before throw.", pair.Key.ToString());
            }

            byEntity.World.SpawnEntity(entityProjectile);
            byEntity.StartAnimation("throw");
            if (byEntity is EntityPlayer)
            {
                RefillSlotIfEmpty(slot, byEntity, (ItemStack itemstack) => itemstack.Collectible is ItemSpear);
            }

            float pitchModifier = (byEntity as EntityPlayer).talkUtil.pitchModifier;
            player.Entity.World.PlaySoundAt(new AssetLocation("game:sounds/player/strike"), player.Entity, player, pitchModifier * 0.9f + (float)byEntity.Api.World.Rand.NextDouble() * 0.2f, 16f, 0.35f);

        }
        protected void RefillSlotIfEmpty(ItemSlot slot, EntityAgent byEntity, ActionConsumable<ItemStack> matcher)
        {
            if (!slot.Empty)
            {
                return;
            }

            byEntity.WalkInventory(delegate (ItemSlot invslot)
            {
                if (invslot is ItemSlotCreative)
                {
                    return true;
                }

                InventoryBase inventory = invslot.Inventory;
                if (!(inventory is InventoryBasePlayer) && !inventory.HasOpened((byEntity as EntityPlayer).Player))
                {
                    return true;
                }

                if (invslot.Itemstack != null && matcher(invslot.Itemstack))
                {
                    invslot.TryPutInto(byEntity.World, slot);
                    invslot.Inventory?.PerformNotifySlot(invslot.Inventory.GetSlotId(invslot));
                    slot.Inventory?.PerformNotifySlot(slot.Inventory.GetSlotId(slot));
                    slot.MarkDirty();
                    invslot.MarkDirty();
                    return false;
                }

                return true;
            });
        }
        protected void WandAttack(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
        {
            if (byEntity.Attributes.GetInt("aimingCancel") == 1) return;
            byEntity.Attributes.SetInt("aiming", 0);
            byEntity.AnimManager.StopAnimation("bowaim");

            if (byEntity.World is IClientWorldAccessor)
            {
                slot.Itemstack.TempAttributes.RemoveAttribute("renderVariant");
            }

            slot.Itemstack.Attributes.SetInt("renderVariant", 0);
            (byEntity as EntityPlayer)?.Player?.InventoryManager.BroadcastHotbarSlot();
            if (secondsUsed < 0.65f)
            {
                return;
            }

            float damage = 0;
            float accuracyBonus = 0f;

            // Base Item damage
            if (slot.Itemstack.Collectible.Attributes != null)
            {
                damage += slot.Itemstack.Collectible.Attributes["damage"].AsFloat(0);

                accuracyBonus = 1 - slot.Itemstack.Collectible.Attributes["accuracyBonus"].AsFloat(0);
            }

            IPlayer byPlayer = null;
            if (byEntity is EntityPlayer) byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
            byEntity.World.PlaySoundAt(new AssetLocation("game:sounds/effect/translocate-breakdimension"), byEntity, byPlayer, false, 8);


            // TODO: Make different projectile entities for different wands
            // EntityProperties type = byEntity.World.GetEntityType(new AssetLocation("krpgwands:magicmissile-" + slot.Itemstack.Collectible.Variant["material"]));
            EntityProperties type = byEntity.World.GetEntityType(new AssetLocation("krpgwands:magicmissile-" + "temporal"));
            var entitymagicmissile = byEntity.World.ClassRegistry.CreateEntity(type) as MagicProjectileEntity;
            entitymagicmissile.FiredBy = byEntity;
            entitymagicmissile.Damage = damage;

            // Enchantments
            int power = 0;
            power = slot.Itemstack.Attributes.GetInt("chilling", 0);
            if (power > 0) entitymagicmissile.WatchedAttributes.SetInt("chilling", power);
            power = slot.Itemstack.Attributes.GetInt("flaming", 0);
            if (power > 0) entitymagicmissile.WatchedAttributes.SetInt("flaming", power);
            power = slot.Itemstack.Attributes.GetInt("frost", 0);
            if (power > 0) entitymagicmissile.WatchedAttributes.SetInt("frost", power);
            power = slot.Itemstack.Attributes.GetInt("harming", 0);
            if (power > 0) entitymagicmissile.WatchedAttributes.SetInt("harming", power);
            power = slot.Itemstack.Attributes.GetInt("healing", 0);
            if (power > 0) entitymagicmissile.WatchedAttributes.SetInt("healing", power);
            power = slot.Itemstack.Attributes.GetInt("knockback", 0);
            if (power > 0) entitymagicmissile.WatchedAttributes.SetInt("knockback", power);
            power = slot.Itemstack.Attributes.GetInt("igniting", 0);
            if (power > 0) entitymagicmissile.WatchedAttributes.SetInt("igniting", power);
            power = slot.Itemstack.Attributes.GetInt("lightning", 0);
            if (power > 0) entitymagicmissile.WatchedAttributes.SetInt("lightning", power);
            power = slot.Itemstack.Attributes.GetInt("shocking", 0);
            if (power > 0) entitymagicmissile.WatchedAttributes.SetInt("shocking", power);
            power = slot.Itemstack.Attributes.GetInt("pit", 0);
            if (power > 0) entitymagicmissile.WatchedAttributes.SetInt("pit", power);

            float acc = Math.Max(0.001f, (1 - byEntity.Attributes.GetFloat("aimingAccuracy", 0)));

            double rndpitch = byEntity.WatchedAttributes.GetDouble("aimingRandPitch", 1) * acc * (0.75 * accuracyBonus);
            double rndyaw = byEntity.WatchedAttributes.GetDouble("aimingRandYaw", 1) * acc * (0.75 * accuracyBonus);

            Vec3d pos = byEntity.ServerPos.XYZ.Add(0, byEntity.LocalEyePos.Y, 0);
            Vec3d aheadPos = pos.AheadCopy(1, byEntity.SidedPos.Pitch + rndpitch, byEntity.SidedPos.Yaw + rndyaw);
            Vec3d velocity = (aheadPos - pos) * byEntity.Stats.GetBlended("bowDrawingStrength");


            entitymagicmissile.ServerPos.SetPos(byEntity.SidedPos.BehindCopy(0.21).XYZ.Add(0, byEntity.LocalEyePos.Y, 0));
            entitymagicmissile.ServerPos.Motion.Set(velocity);
            entitymagicmissile.Pos.SetFrom(entitymagicmissile.ServerPos);
            entitymagicmissile.World = byEntity.World;
            entitymagicmissile.SetRotation();

            byEntity.World.SpawnEntity(entitymagicmissile);

            slot.Itemstack.Collectible.DamageItem(byEntity.World, byEntity, slot);

            byEntity.AnimManager.StartAnimation("bowhit");
        }
        */
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
