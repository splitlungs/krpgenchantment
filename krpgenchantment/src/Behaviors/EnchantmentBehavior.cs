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
using Cairo;
using static System.Net.Mime.MediaTypeNames;
using System.Reflection.Metadata;

namespace KRPGLib.Enchantment
{
    public class EnchantmentBehavior : CollectibleBehavior
    {
        public ICoreAPI Api;
        public ICoreServerAPI sApi;
        /// <summary>
        /// Class for storing default enchantment configuration. Do not save your active enchantments here.
        /// </summary>
        private Dictionary<string, int> Enchantments = new Dictionary<string, int>();
        public bool Enchantable = false;
        public bool IsReagent = false;
        public EnchantmentBehavior(CollectibleObject collObj) : base(collObj)
        {
        }
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            Api = api;
            // Particles - Not Working Yet
            // ConfigParticles();
            
            // We only load the config on the server, so check side first
            if (api.Side != EnumAppSide.Server) return;
            sApi = api as ICoreServerAPI;
            if (EnchantingConfigLoader.Config.ValidReagents.ContainsKey(collObj.Code)) 
                IsReagent = true;
        }
        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            // Enchantments
            Dictionary<string, int> enchants = world.Api.EnchantAccessor().GetActiveEnchantments(inSlot.Itemstack);
            if (enchants != null)
            {
                foreach (KeyValuePair<string, int> pair in enchants)
                {
                    // string s = string.Format("<font color=\"" + Enum.GetName(typeof(EnchantColors), pair.Value) + "\">" + Lang.Get("krpgenchantment:enchantment-" + pair.Key) + " " + Lang.Get("krpgenchantment:" + pair.Value) + "</font>");
                    // dsc.AppendLine(s);
                    // dsc.AppendLine(string.Format("<font color=\"" + Enum.GetName(typeof(EnchantColors), pair.Value) + "\">" + Lang.Get("krpgenchantment:enchantment-" + pair.Key) + " " + Lang.Get("krpgenchantment:" + pair.Value) + "</font>"));
                    
                    // THE RIGHT WAY
                    dsc.AppendLine(string.Format("<font color=\"{0}\">{1} {2}</font>",
                    Enum.GetName(typeof(EnchantColors), pair.Value),
                    Lang.Get("krpgenchantment:enchantment-" + pair.Key),
                    Lang.Get("krpgenchantment:" + pair.Value)));
                }
            }
            // Reagent Charge
            int p = world.Api.EnchantAccessor().GetReagentCharge(inSlot.Itemstack);
            if (p != 0)
            {
               string s = string.Format("<font color=\"" + Enum.GetName(typeof(EnchantColors), p) + "\">" + Lang.Get("krpgenchantment:reagent-charge-prefix") + p.ToString() + "</font>");
                dsc.AppendLine(s);
            }
        }
        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
        {
            // Specific use on self for KRPG Wands
            if (secondsUsed < 1 || slot.Itemstack.Collectible.Class != "WandItem") return;

            handling = EnumHandling.Handled;

            int aimSelf = byEntity.WatchedAttributes.GetInt("aimSelf", 0);
            if (aimSelf == 1 && Api.EnchantAccessor().GetActiveEnchantments(slot.Itemstack) != null)
            {
                EnchantModifiers parameters = new EnchantModifiers();
                bool didEnchantments = sApi.EnchantAccessor().TryEnchantments(slot, "OnAttack", byEntity, byEntity, ref parameters);
            }

            if (byEntity.Attributes.GetInt("aimingCancel") == 1)
            {
                return;
            }

            base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel, ref handling);
        }

        #region Retired
        // private void ConfigParticles()
        // {
        //     collObj.LightHsv = new byte[3] { 4, 4, 14 };
        //     collObj.ParticleProperties = new AdvancedParticleProperties[3];
        //     collObj.ParticleProperties[0] = new AdvancedParticleProperties
        //     {
        // 
        //         basePos = collObj.TopMiddlePos.ToVec3d(),
        //         // PosOffset = new NatFloat[3]
        //         // {
        //         //     NatFloat.createUniform(-0.2f, 0f),
        //         //     NatFloat.createUniform(0f, 0f),
        //         //     NatFloat.createUniform(0f, 0f)
        //         // },
        //         HsvaColor = new NatFloat[4]
        //         {
        //             NatFloat.createUniform(30f, 20f),
        //             NatFloat.createUniform(255f, 50f),
        //             NatFloat.createUniform(255f, 50f),
        //             NatFloat.createUniform(255f, 0f)
        //         },
        //         GravityEffect = NatFloat.createUniform(0f, 0f),
        //         Velocity = new NatFloat[3]
        //         {
        //             NatFloat.createUniform(0.2f, 0.05f),
        //             NatFloat.createUniform(0.5f, 0.1f),
        //             NatFloat.createUniform(0.2f, 0.05f)
        //         },
        //         Size = NatFloat.createUniform(0.1f, 0f),
        //         Quantity = NatFloat.createUniform(0.25f, 0f),
        //         VertexFlags = 128,
        //         SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -0.25f),
        //         SelfPropelled = true,
        //         DieInLiquid = true
        //     };
        //     collObj.ParticleProperties[1] = new AdvancedParticleProperties
        //     {
        //         basePos = collObj.TopMiddlePos.ToVec3d(),
        //         // PosOffset = new NatFloat[3]
        //         // {
        //         //     NatFloat.createUniform(-0.2f, 0f),
        //         //     NatFloat.createUniform(0f, 0f),
        //         //     NatFloat.createUniform(0f, 0f)
        //         // },
        //         HsvaColor = new NatFloat[4]
        //         {
        //         NatFloat.createUniform(30f, 20f),
        //         NatFloat.createUniform(255f, 50f),
        //         NatFloat.createUniform(255f, 50f),
        //         NatFloat.createUniform(255f, 0f)
        //         },
        //         OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f),
        //         GravityEffect = NatFloat.createUniform(0f, 0f),
        //         Velocity = new NatFloat[3]
        //         {
        //         NatFloat.createUniform(0f, 0.02f),
        //         NatFloat.createUniform(0f, 0.02f),
        //         NatFloat.createUniform(0f, 0.02f)
        //         },
        //         Size = NatFloat.createUniform(0.12f, 0.05f),
        //         Quantity = NatFloat.createUniform(0.25f, 0f),
        //         VertexFlags = 128,
        //         SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 0.3f),
        //         LifeLength = NatFloat.createUniform(0.5f, 0f),
        //         ParticleModel = EnumParticleModel.Quad,
        //         DieInLiquid = true
        //     };
        //     collObj.ParticleProperties[2] = new AdvancedParticleProperties
        //     {
        //         basePos = collObj.TopMiddlePos.ToVec3d(),
        //         // PosOffset = new NatFloat[3]
        //         // {
        //         //     NatFloat.createUniform(-0.2f, 0f),
        //         //     NatFloat.createUniform(0f, 0f),
        //         //     NatFloat.createUniform(0f, 0f)
        //         // },
        //         HsvaColor = new NatFloat[4]
        //         {
        //         NatFloat.createUniform(0f, 0f),
        //         NatFloat.createUniform(0f, 0f),
        //         NatFloat.createUniform(40f, 30f),
        //         NatFloat.createUniform(220f, 50f)
        //         },
        //         OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f),
        //         GravityEffect = NatFloat.createUniform(0f, 0f),
        //         Velocity = new NatFloat[3]
        //         {
        //         NatFloat.createUniform(0f, 0.05f),
        //         NatFloat.createUniform(0.2f, 0.3f),
        //         NatFloat.createUniform(0f, 0.05f)
        //         },
        //         Size = NatFloat.createUniform(0.12f, 0.05f),
        //         Quantity = NatFloat.createUniform(0.25f, 0f),
        //         SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 0.5f),
        //         LifeLength = NatFloat.createUniform(1.5f, 0f),
        //         ParticleModel = EnumParticleModel.Quad,
        //         SelfPropelled = true,
        //         DieInLiquid = true
        //     };
        // }

        // public IEnumerable<Type> FindDerivedTypes(Assembly assembly, Type baseType)
        // {
        //     return assembly.GetTypes().Where(t => baseType.IsAssignableFrom(t));
        // }
        // public override void Initialize(JsonObject properties)
        // {
        //     base.Initialize(properties);
        // }
        /// <summary>
        /// Applies default JSON properties to EnchantProps.
        /// </summary>
        /// <param name="properties"></param>
        // public virtual void GetProperties(JsonObject properties)
        // {
        //     // EnchantProps.Enchantable = properties["enchantable"].AsBool(false);
        //     foreach (var eType in Enum.GetNames<EnumEnchantments>())
        //         properties[eType].AsInt(0);
        // }
        /// <summary>
        /// Save all EnchantProps to ItemStack's Attributes.
        /// </summary>
        /// <param name="itemStack"></param>
        // public void SetAttributesFromProps(ItemStack itemStack)
        // {
        //     ITreeAttribute tree = itemStack.Attributes.GetOrAddTreeAttribute("enchantments");
        //     // tree.SetBool("enchantable", EnchantProps.Enchantable);
        //     // foreach (KeyValuePair<string, int> keyValuePair in EnchantProps.Enchants)
        //     //     tree.SetInt(keyValuePair.Key, keyValuePair.Value);
        //     itemStack.Attributes.MergeTree(tree);
        // }
        /// <summary>
        /// Gets Enchantment attributes from the ItemStack and writes to Enchant Properties
        /// </summary>
        /// <param name="itemStack"></param>
        // public void GetAttributes(ItemSlot inSlot)
        // {
        //     Enchantments = Api.EnchantAccessor().GetActiveEnchantments(inSlot.Itemstack);
        //     Enchantable = Api.EnchantAccessor().IsEnchantable(inSlot);
        // }
        /// <summary>
        /// Sets all Enchantment data to ItemStack's Attributes
        /// </summary>
        /// <param name="itemStack"></param>
        // public void SetAttributes(ItemStack itemStack)
        // {
        //     ITreeAttribute tree = itemStack.Attributes.GetOrAddTreeAttribute("enchantments");
        //     tree.SetBool("enchantable", Enchantable);
        //     foreach (KeyValuePair<string, int> keyValuePair in Enchantments)
        //         itemStack.Attributes.SetInt(keyValuePair.Key, keyValuePair.Value);
        //     itemStack.Attributes.MergeTree(tree);
        // }


        // public override void OnHeldAttackStop(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel, ref EnumHandling handling)
        // {
        //     if (entitySel == null || entitySel.Entity == null || byEntity == null || slot == null || slot.Empty) return;
        // 
        //     Dictionary<string, int> enchants = Api.EnchantAccessor().GetActiveEnchantments(slot.Itemstack);
        //     if (enchants == null) return;
        // 
        //     // Should avoid default during healing
        //     if (enchants.ContainsKey("healing"))
        //         handling = EnumHandling.PreventDefault;
        //     else
        //         handling = EnumHandling.Handled;
        // 
        //     EnchantModifiers parameters = new EnchantModifiers();
        //     bool didEnchantments = byEntity.Api.EnchantAccessor().TryEnchantments(slot, "OnAttack", byEntity, entitySel.Entity, ref parameters);
        //     if (!didEnchantments)
        //         Api.Logger.Warning("[KRPGEnchantments] Failed to TryEnchantments on {0}!", slot.Itemstack.GetName());
        // 
        //     base.OnHeldAttackStop(secondsPassed, slot, byEntity, blockSelection, entitySel, ref handling);
        // }

        /*
        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            if (renderinfo != null)
            {
                if (target == EnumItemRenderTarget.HandFp || target == EnumItemRenderTarget.HandTp || target == EnumItemRenderTarget.HandTpOff)
                {
                    // Get Enchantments
                    Dictionary<string, int> enchants = new Dictionary<string, int>();
                    foreach (var val in Enum.GetValues(typeof(EnumEnchantments)))
                    {
                        int ePower = itemstack.Attributes.GetInt(val.ToString(), 0);
                        if (ePower > 0)
                            enchants.Add(val.ToString(), ePower);
                    }
                }
            }

            Vec3d pos = byEntity.Pos.AheadCopy(0.4000000059604645).XYZ;
            pos.X += byEntity.LocalEyePos.X;
            pos.Y += byEntity.LocalEyePos.Y - 0.4000000059604645;
            pos.Z += byEntity.LocalEyePos.Z;
            if (secondsUsed > 0.5f && (int)(30f * secondsUsed) % 7 == 1)
            {
                byEntity.World.SpawnCubeParticles(pos, spawnParticleStack ?? slot.Itemstack, 0.3f, 4, 0.5f, (byEntity as EntityPlayer)?.Player);
            }
            if (byEntity.World is IClientWorldAccessor)
            {
                ModelTransform tf = new ModelTransform();
                tf.EnsureDefaultValues();
                tf.Origin.Set(0f, 0f, 0f);
                if (secondsUsed > 0.5f)
                {
                    tf.Translation.Y = Math.Min(0.02f, GameMath.Sin(20f * secondsUsed) / 10f);
                }
                tf.Translation.X -= Math.Min(1f, secondsUsed * 4f * 1.57f);
                tf.Translation.Y -= Math.Min(0.05f, secondsUsed * 2f);
                tf.Rotation.X += Math.Min(30f, secondsUsed * 350f);
                tf.Rotation.Y += Math.Min(80f, secondsUsed * 350f);
                byEntity.Controls.UsingHeldItemTransformAfter = tf;
                return secondsUsed <= 1f;
            }

            base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
        }
        */
        #endregion
    }
}