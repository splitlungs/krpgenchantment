using System;
using System.Collections.Generic;
using Vintagestory.GameContent;
using Vintagestory.API.Server;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using HarmonyLib;
using System.Reflection;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using KRPGLib.Enchantment.API;
using System.IO;
using System.Xml.Linq;

namespace KRPGLib.Enchantment
{
    /// <summary>
    /// Configurable JSON data used to configure Enchantment classes.
    /// </summary>
    public class EnchantmentProperties : IByteSerializable
    {
        // Toggles processing of this enchantment
        public bool Enabled = true;
        // How the Enchantment is referenced in code
        public string Code = "enchantment";
        // Define which registered class to instantiate with
        public string ClassName { get; set; }
        // The code used to lookup the enchantment's Lore in the lang file
        public string LoreCode = "enchantment-lore";
        // The ID of the chapter in the Lore config file
        public int LoreChapterID = 0;
        // The maximum functional Power of an Enchantment
        public int MaxTier = 5;
        // Configurable JSON multiplier for effects
        public object[] Modifiers;
        public EnchantmentProperties Clone()
        {
            return CloneTo<EnchantmentProperties>();
        }

        public T CloneTo<T>() where T : EnchantmentProperties, new()
        {
            T val = new T
            {
                Enabled = Enabled,
                Code = Code,
                ClassName = ClassName,
                LoreChapterID = LoreChapterID,
                MaxTier = MaxTier,
                Modifiers = Modifiers
            };

            return val;
        }
        public void ToBytes(BinaryWriter writer)
        {
            writer.Write(Enabled);
            writer.Write(Code);
            writer.Write(ClassName);
            writer.Write(LoreCode);
            writer.Write(MaxTier);
            writer.Write((int)Modifiers.Length);
            foreach (var val in Modifiers)
            {
                Type t = val.GetType();
                writer.Write((string)val);
            }
        }
        public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
        {
            Enabled = reader.ReadBoolean();
            Code = reader.ReadString();
            ClassName = reader.ReadString();
            LoreCode = reader.ReadString();
            MaxTier = reader.ReadInt32();
            int length = reader.ReadInt32();
            Modifiers = new string[length];
            for (int i = 0; i < Modifiers.Length; i++)
            {
                Modifiers[i] = reader.ReadString();
            }
        }
    }
    /// <summary>
    /// Generic for creating Tick Registries
    /// </summary>
    public class EnchantTick
    {
        public int TicksRemaining;
        public long TickTimeStart;
        public long LastTickTime;
    }
    /// <summary>
    /// Base class for an Enchantment.
    /// </summary>
    public abstract class Enchantment : IEnchantment
    {
        public ICoreAPI Api { get; set; }
        // Toggles processing of this enchantment
        public bool Enabled { get; set; }
        // How the Enchantment is referenced in code
        public string Code { get; set; }
        // Define which registered class to instantiate with
        public string ClassName { get; set; }
        // The code used to lookup the enchantment's Lore in the lang file
        public string LoreCode { get; set; }
        // The ID of the chapter in the Lore config file
        public int LoreChapterID { get; set; }
        // The maximum functional Power of an Enchantment
        public int MaxTier { get; set; }
        // Configurable JSON multiplier for effects
        public object[] Modifiers { get; set; }
        // Used to manage generic ticks. You still have to register your tick method with the API.
        public Dictionary<long, EnchantTick> TickRegistry { get; set; }
        // 
        public EnchantmentProperties EnchantProps { get; set; }
        public JsonObject PropertiesObject { get; set; }

        public Enchantment(ICoreAPI api)
        {
            Api = api;
            TickRegistry = new Dictionary<long, EnchantTick>();
        }
        /// <summary>
        /// Called right after the Enchantment is created. Must call base method to load JSON Properties.
        /// </summary>
        /// <param name="properties"></param>
        public virtual void Initialize(EnchantmentProperties properties)
        {
            // EnchantProps = properties.AsObject<EnchantmentProperties>(null, collObj.Code.Domain);

            EnchantProps = properties;
            // Enabled = properties.Enabled;
            // Code = properties.Code;
            // LoreCode = properties.LoreCode;
            // LoreChapterID = properties.LoreChapterID;
            // MaxTier = properties.MaxTier;
            // Modifiers = properties.Modifiers;
        }
        /// <summary>
        /// Generic method to execute a method matching the Trigger parameter. Called by the TriggerEnchant event in KRPGEnchantmentSystem.
        /// </summary>
        /// <param name="enchant"></param>
        /// <param name="slot"></param>
        /// <param name="parameters"></param>
        public virtual void OnTrigger(EnchantmentSource enchant, ItemSlot slot, ref object[] parameters)
        {
            MethodInfo meth = this.GetType().GetMethod(enchant.Trigger, BindingFlags.Instance);
            if (parameters != null)
                meth?.Invoke(this, new object[3] { enchant, slot, parameters });
            else
                meth?.Invoke(this, new object[2] { enchant, slot });
        }

        public virtual void OnAttack(EnchantmentSource enchant, ItemSlot slot, ref float? damage)
        {
        
        }
        public virtual void OnHit(EnchantmentSource enchant, ItemSlot slot, ref float? damage)
        {
        
        }
        public virtual void OnToggle(EnchantmentSource enchant, ItemSlot slot)
        {
        
        }
        public virtual void OnStart(EnchantmentSource enchant, ItemSlot slot)
        {
        
        }
        public virtual void OnEnd(EnchantmentSource enchant, ItemSlot slot)
        {
        
        }
        protected static AdvancedParticleProperties[] HealParticleProps;
        protected static AdvancedParticleProperties[] FireParticleProps;
        protected static AdvancedParticleProperties[] FrostParticleProps;
        protected static AdvancedParticleProperties[] InjuryParticleProps;
        protected static AdvancedParticleProperties[] ElectricParticleProps;
        protected static AdvancedParticleProperties[] PoisonParticleProps;
        protected bool resetLightHsv;

        private void ConfigParticles()
        {
            FireParticleProps = new AdvancedParticleProperties[3];
            FireParticleProps[0] = new AdvancedParticleProperties
            {
                HsvaColor = new NatFloat[4]
            {
                NatFloat.createUniform(30f, 20f),
                NatFloat.createUniform(255f, 50f),
                NatFloat.createUniform(255f, 50f),
                NatFloat.createUniform(255f, 0f)
            },
                GravityEffect = NatFloat.createUniform(0f, 0f),
                Velocity = new NatFloat[3]
            {
                NatFloat.createUniform(0.2f, 0.05f),
                NatFloat.createUniform(0.5f, 0.1f),
                NatFloat.createUniform(0.2f, 0.05f)
            },
                Size = NatFloat.createUniform(0.25f, 0f),
                Quantity = NatFloat.createUniform(0.25f, 0f),
                VertexFlags = 128,
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -0.5f),
                SelfPropelled = true
            };
            FireParticleProps[1] = new AdvancedParticleProperties
            {
                HsvaColor = new NatFloat[4]
                {
                NatFloat.createUniform(30f, 20f),
                NatFloat.createUniform(255f, 50f),
                NatFloat.createUniform(255f, 50f),
                NatFloat.createUniform(255f, 0f)
                },
                OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f),
                GravityEffect = NatFloat.createUniform(0f, 0f),
                Velocity = new NatFloat[3]
                {
                NatFloat.createUniform(0f, 0.02f),
                NatFloat.createUniform(0f, 0.02f),
                NatFloat.createUniform(0f, 0.02f)
                },
                Size = NatFloat.createUniform(0.3f, 0.05f),
                Quantity = NatFloat.createUniform(0.25f, 0f),
                VertexFlags = 128,
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 1f),
                LifeLength = NatFloat.createUniform(0.5f, 0f),
                ParticleModel = EnumParticleModel.Quad
            };
            FireParticleProps[2] = new AdvancedParticleProperties
            {
                HsvaColor = new NatFloat[4]
                {
                NatFloat.createUniform(0f, 0f),
                NatFloat.createUniform(0f, 0f),
                NatFloat.createUniform(40f, 30f),
                NatFloat.createUniform(220f, 50f)
                },
                OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f),
                GravityEffect = NatFloat.createUniform(0f, 0f),
                Velocity = new NatFloat[3]
                {
                NatFloat.createUniform(0f, 0.05f),
                NatFloat.createUniform(0.2f, 0.3f),
                NatFloat.createUniform(0f, 0.05f)
                },
                Size = NatFloat.createUniform(0.3f, 0.05f),
                Quantity = NatFloat.createUniform(0.25f, 0f),
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 1.5f),
                LifeLength = NatFloat.createUniform(1.5f, 0f),
                ParticleModel = EnumParticleModel.Quad,
                SelfPropelled = true
            };

            HealParticleProps = new AdvancedParticleProperties[3];
            HealParticleProps[0] = new AdvancedParticleProperties
            {
                HsvaColor = new NatFloat[4]
            {
                NatFloat.createUniform(36f, 0f),
                NatFloat.createUniform(255f, 10f),
                NatFloat.createUniform(200f, 20f),
                NatFloat.createUniform(255f, 0f)
            },
                GravityEffect = NatFloat.createUniform(0f, 0f),
                Velocity = new NatFloat[3]
            {
                NatFloat.createUniform(0.2f, 0.05f),
                NatFloat.createUniform(0.5f, 0.1f),
                NatFloat.createUniform(0.2f, 0.05f)
            },
                Size = NatFloat.createUniform(0.25f, 0f),
                Quantity = NatFloat.createUniform(0.25f, 0f),
                VertexFlags = 128,
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -0.5f),
                SelfPropelled = true
            };
            HealParticleProps[1] = new AdvancedParticleProperties
            {
                HsvaColor = new NatFloat[4]
            {
                NatFloat.createUniform(36f, 0f),
                NatFloat.createUniform(255f, 10f),
                NatFloat.createUniform(200f, 20f),
                NatFloat.createUniform(255f, 0f)
            },
                OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f),
                GravityEffect = NatFloat.createUniform(0f, 0f),
                Velocity = new NatFloat[3]
                {
                NatFloat.createUniform(0f, 0.02f),
                NatFloat.createUniform(0f, 0.02f),
                NatFloat.createUniform(0f, 0.02f)
                },
                Size = NatFloat.createUniform(0.3f, 0.05f),
                Quantity = NatFloat.createUniform(0.25f, 0f),
                VertexFlags = 128,
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 1f),
                LifeLength = NatFloat.createUniform(0.5f, 0f),
                ParticleModel = EnumParticleModel.Quad
            };
            HealParticleProps[2] = new AdvancedParticleProperties
            {
                HsvaColor = new NatFloat[4]
                {
                NatFloat.createUniform(36f, 0f),
                NatFloat.createUniform(255f, 00f),
                NatFloat.createUniform(255f, 20f),
                NatFloat.createUniform(255f, 0f)
                },
                OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f),
                GravityEffect = NatFloat.createUniform(0f, 0f),
                Velocity = new NatFloat[3]
                {
                NatFloat.createUniform(0f, 0.05f),
                NatFloat.createUniform(0.2f, 0.3f),
                NatFloat.createUniform(0f, 0.05f)
                },
                Size = NatFloat.createUniform(0.3f, 0.05f),
                Quantity = NatFloat.createUniform(0.25f, 0f),
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 1.5f),
                LifeLength = NatFloat.createUniform(1.5f, 0f),
                ParticleModel = EnumParticleModel.Quad,
                SelfPropelled = true
            };

            FrostParticleProps = new AdvancedParticleProperties[3];
            FrostParticleProps[0] = new AdvancedParticleProperties
            {
                HsvaColor = new NatFloat[4]
            {
                NatFloat.createUniform(128f, 0f),
                NatFloat.createUniform(128f, 20f),
                NatFloat.createUniform(255f, 50f),
                NatFloat.createUniform(255f, 0f)
            },
                GravityEffect = NatFloat.createUniform(0f, 0f),
                Velocity = new NatFloat[3]
            {
                NatFloat.createUniform(0.2f, 0.05f),
                NatFloat.createUniform(0.5f, 0.1f),
                NatFloat.createUniform(0.2f, 0.05f)
            },
                Size = NatFloat.createUniform(0.25f, 0f),
                Quantity = NatFloat.createUniform(0.25f, 0f),
                VertexFlags = 128,
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -0.5f),
                SelfPropelled = true
            };
            FrostParticleProps[1] = new AdvancedParticleProperties
            {
                HsvaColor = new NatFloat[4]
            {
                NatFloat.createUniform(128f, 0f),
                NatFloat.createUniform(128f, 20f),
                NatFloat.createUniform(255f, 50f),
                NatFloat.createUniform(255f, 0f)
            },
                OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f),
                GravityEffect = NatFloat.createUniform(0f, 0f),
                Velocity = new NatFloat[3]
                {
                NatFloat.createUniform(0f, 0.02f),
                NatFloat.createUniform(0f, 0.02f),
                NatFloat.createUniform(0f, 0.02f)
                },
                Size = NatFloat.createUniform(0.3f, 0.05f),
                Quantity = NatFloat.createUniform(0.25f, 0f),
                VertexFlags = 128,
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 1f),
                LifeLength = NatFloat.createUniform(0.5f, 0f),
                ParticleModel = EnumParticleModel.Quad
            };
            FrostParticleProps[2] = new AdvancedParticleProperties
            {
                HsvaColor = new NatFloat[4]
                {
                NatFloat.createUniform(128f, 0f),
                NatFloat.createUniform(128f, 20f),
                NatFloat.createUniform(255f, 50f),
                NatFloat.createUniform(255f, 0f)
                },
                OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f),
                GravityEffect = NatFloat.createUniform(0f, 0f),
                Velocity = new NatFloat[3]
                {
                NatFloat.createUniform(0f, 0.05f),
                NatFloat.createUniform(0.2f, 0.3f),
                NatFloat.createUniform(0f, 0.05f)
                },
                Size = NatFloat.createUniform(0.3f, 0.05f),
                Quantity = NatFloat.createUniform(0.25f, 0f),
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 1.5f),
                LifeLength = NatFloat.createUniform(1.5f, 0f),
                ParticleModel = EnumParticleModel.Quad,
                SelfPropelled = true
            };

            ElectricParticleProps = new AdvancedParticleProperties[3];
            ElectricParticleProps[0] = new AdvancedParticleProperties
            {
                HsvaColor = new NatFloat[4]
            {
                NatFloat.createUniform(45f, 0f),
                NatFloat.createUniform(0f, 20f),
                NatFloat.createUniform(255f, 50f),
                NatFloat.createUniform(255f, 0f)
            },
                GravityEffect = NatFloat.createUniform(0f, 0f),
                Velocity = new NatFloat[3]
            {
                NatFloat.createUniform(0.5f, 0.05f),
                NatFloat.createUniform(0.75f, 0.1f),
                NatFloat.createUniform(0.5f, 0.05f)
            },
                Size = NatFloat.createUniform(0.25f, 0f),
                Quantity = NatFloat.createUniform(0.25f, 0f),
                VertexFlags = 128,
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -0.5f),
                SelfPropelled = true
            };
            ElectricParticleProps[1] = new AdvancedParticleProperties
            {
                HsvaColor = new NatFloat[4]
            {
                NatFloat.createUniform(45f, 0f),
                NatFloat.createUniform(0f, 20f),
                NatFloat.createUniform(255f, 50f),
                NatFloat.createUniform(255f, 0f)
            },
                OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f),
                GravityEffect = NatFloat.createUniform(0f, 0f),
                Velocity = new NatFloat[3]
                {
                NatFloat.createUniform(0f, 0.02f),
                NatFloat.createUniform(0f, 0.02f),
                NatFloat.createUniform(0f, 0.02f)
                },
                Size = NatFloat.createUniform(0.3f, 0.05f),
                Quantity = NatFloat.createUniform(0.25f, 0f),
                VertexFlags = 128,
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 1f),
                LifeLength = NatFloat.createUniform(0.5f, 0f),
                ParticleModel = EnumParticleModel.Quad
            };
            ElectricParticleProps[2] = new AdvancedParticleProperties
            {
                HsvaColor = new NatFloat[4]
                {
                NatFloat.createUniform(45f, 0f),
                NatFloat.createUniform(0f, 20f),
                NatFloat.createUniform(255f, 50f),
                NatFloat.createUniform(255f, 0f)
                },
                OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f),
                GravityEffect = NatFloat.createUniform(0f, 0f),
                Velocity = new NatFloat[3]
                {
                NatFloat.createUniform(0.2f, 0.05f),
                NatFloat.createUniform(0.5f, 0.3f),
                NatFloat.createUniform(0.2f, 0.05f)
                },
                Size = NatFloat.createUniform(0.3f, 0.05f),
                Quantity = NatFloat.createUniform(0.25f, 0f),
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 1.5f),
                LifeLength = NatFloat.createUniform(1.5f, 0f),
                ParticleModel = EnumParticleModel.Quad,
                SelfPropelled = true
            };

            InjuryParticleProps = new AdvancedParticleProperties[3];
            InjuryParticleProps[0] = new AdvancedParticleProperties
            {
                HsvaColor = new NatFloat[4]
            {
                NatFloat.createUniform(0f, 0f),
                NatFloat.createUniform(0f, 0f),
                NatFloat.createUniform(0f, 50f),
                NatFloat.createUniform(255f, 0f)
            },
                GravityEffect = NatFloat.createUniform(0f, 0f),
                Velocity = new NatFloat[3]
            {
                NatFloat.createUniform(0.2f, 0.05f),
                NatFloat.createUniform(0.5f, 0.1f),
                NatFloat.createUniform(0.2f, 0.05f)
            },
                Size = NatFloat.createUniform(0.25f, 0f),
                Quantity = NatFloat.createUniform(0.25f, 0f),
                VertexFlags = 128,
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -0.5f),
                SelfPropelled = true
            };
            InjuryParticleProps[1] = new AdvancedParticleProperties
            {
                HsvaColor = new NatFloat[4]
            {
                NatFloat.createUniform(0f, 0f),
                NatFloat.createUniform(0f, 0f),
                NatFloat.createUniform(0f, 50f),
                NatFloat.createUniform(255f, 0f)
            },
                OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f),
                GravityEffect = NatFloat.createUniform(0f, 0f),
                Velocity = new NatFloat[3]
                {
                NatFloat.createUniform(0f, 0.02f),
                NatFloat.createUniform(0f, 0.02f),
                NatFloat.createUniform(0f, 0.02f)
                },
                Size = NatFloat.createUniform(0.3f, 0.05f),
                Quantity = NatFloat.createUniform(0.25f, 0f),
                VertexFlags = 128,
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 1f),
                LifeLength = NatFloat.createUniform(0.5f, 0f),
                ParticleModel = EnumParticleModel.Quad
            };
            InjuryParticleProps[2] = new AdvancedParticleProperties
            {
                HsvaColor = new NatFloat[4]
                {
                NatFloat.createUniform(0f, 0f),
                NatFloat.createUniform(0f, 0f),
                NatFloat.createUniform(0f, 50f),
                NatFloat.createUniform(255f, 0f)
                },
                OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f),
                GravityEffect = NatFloat.createUniform(0f, 0f),
                Velocity = new NatFloat[3]
                {
                NatFloat.createUniform(0f, 0.05f),
                NatFloat.createUniform(0.2f, 0.3f),
                NatFloat.createUniform(0f, 0.05f)
                },
                Size = NatFloat.createUniform(0.3f, 0.05f),
                Quantity = NatFloat.createUniform(0.25f, 0f),
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 1.5f),
                LifeLength = NatFloat.createUniform(1.5f, 0f),
                ParticleModel = EnumParticleModel.Quad,
                SelfPropelled = true
            };

            PoisonParticleProps = new AdvancedParticleProperties[3];
            PoisonParticleProps[0] = new AdvancedParticleProperties
            {
                HsvaColor = new NatFloat[4]
            {
                NatFloat.createUniform(188f, 0f),
                NatFloat.createUniform(255f, 0f),
                NatFloat.createUniform(200f, 50f),
                NatFloat.createUniform(255f, 0f)
            },
                GravityEffect = NatFloat.createUniform(0f, 0f),
                Velocity = new NatFloat[3]
            {
                NatFloat.createUniform(0.2f, 0.05f),
                NatFloat.createUniform(0.5f, 0.1f),
                NatFloat.createUniform(0.2f, 0.05f)
            },
                Size = NatFloat.createUniform(0.25f, 0f),
                Quantity = NatFloat.createUniform(0.25f, 0f),
                VertexFlags = 128,
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -0.5f),
                SelfPropelled = true
            };
            PoisonParticleProps[1] = new AdvancedParticleProperties
            {
                HsvaColor = new NatFloat[4]
            {
                NatFloat.createUniform(188f, 0f),
                NatFloat.createUniform(255f, 0f),
                NatFloat.createUniform(200f, 50f),
                NatFloat.createUniform(255f, 0f)
            },
                OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f),
                GravityEffect = NatFloat.createUniform(0f, 0f),
                Velocity = new NatFloat[3]
                {
                NatFloat.createUniform(0f, 0.02f),
                NatFloat.createUniform(0f, 0.02f),
                NatFloat.createUniform(0f, 0.02f)
                },
                Size = NatFloat.createUniform(0.3f, 0.05f),
                Quantity = NatFloat.createUniform(0.25f, 0f),
                VertexFlags = 128,
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 1f),
                LifeLength = NatFloat.createUniform(0.5f, 0f),
                ParticleModel = EnumParticleModel.Quad
            };
            PoisonParticleProps[2] = new AdvancedParticleProperties
            {
                HsvaColor = new NatFloat[4]
                {
                NatFloat.createUniform(188f, 0f),
                NatFloat.createUniform(255f, 0f),
                NatFloat.createUniform(200f, 50f),
                NatFloat.createUniform(255f, 0f)
                },
                OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f),
                GravityEffect = NatFloat.createUniform(0f, 0f),
                Velocity = new NatFloat[3]
                {
                NatFloat.createUniform(0f, 0.05f),
                NatFloat.createUniform(0.2f, 0.3f),
                NatFloat.createUniform(0f, 0.05f)
                },
                Size = NatFloat.createUniform(0.3f, 0.05f),
                Quantity = NatFloat.createUniform(0.25f, 0f),
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 1.5f),
                LifeLength = NatFloat.createUniform(1.5f, 0f),
                ParticleModel = EnumParticleModel.Quad,
                SelfPropelled = true
            };
        }
        protected virtual void GenerateParticles(Entity entity, EnumDamageType damageType, float damage)
        {
            int power = (int)Math.Ceiling(damage);

            if (damageType == EnumDamageType.Fire)
            {
                int num = Math.Min(FireParticleProps.Length - 1, Api.World.Rand.Next(FireParticleProps.Length + 1));
                AdvancedParticleProperties advancedParticleProperties = FireParticleProps[num];
                advancedParticleProperties.basePos.Set(entity.SidedPos.X, entity.SidedPos.Y + (double)(entity.SelectionBox.YSize / 2f), entity.Pos.Z);
                advancedParticleProperties.PosOffset[0].var = entity.SelectionBox.XSize / 2f;
                advancedParticleProperties.PosOffset[1].var = entity.SelectionBox.YSize / 2f;
                advancedParticleProperties.PosOffset[2].var = entity.SelectionBox.ZSize / 2f;
                advancedParticleProperties.Velocity[0].avg = (float)entity.Pos.Motion.X * 10f;
                advancedParticleProperties.Velocity[1].avg = (float)entity.Pos.Motion.Y * 5f;
                advancedParticleProperties.Velocity[2].avg = (float)entity.Pos.Motion.Z * 10f;
                advancedParticleProperties.Quantity.avg = GameMath.Sqrt(advancedParticleProperties.PosOffset[0].var + advancedParticleProperties.PosOffset[1].var + advancedParticleProperties.PosOffset[2].var) * num switch
                {
                    1 => 3f,
                    0 => 0.5f,
                    _ => 1.25f,
                };
                for (int i = 0; i <= power; i++)
                {
                    Api.World.SpawnParticles(advancedParticleProperties);
                }
            }
            if (damageType == EnumDamageType.Frost)
            {
                int num = Math.Min(FrostParticleProps.Length - 1, Api.World.Rand.Next(FrostParticleProps.Length + 1));
                AdvancedParticleProperties advancedParticleProperties = FrostParticleProps[num];
                advancedParticleProperties.basePos.Set(entity.SidedPos.X, entity.SidedPos.Y + (double)(entity.SelectionBox.YSize / 2f), entity.Pos.Z);
                advancedParticleProperties.PosOffset[0].var = entity.SelectionBox.XSize / 2f;
                advancedParticleProperties.PosOffset[1].var = entity.SelectionBox.YSize / 2f;
                advancedParticleProperties.PosOffset[2].var = entity.SelectionBox.ZSize / 2f;
                advancedParticleProperties.Velocity[0].avg = (float)entity.Pos.Motion.X * 10f;
                advancedParticleProperties.Velocity[1].avg = (float)entity.Pos.Motion.Y * 5f;
                advancedParticleProperties.Velocity[2].avg = (float)entity.Pos.Motion.Z * 10f;
                advancedParticleProperties.Quantity.avg = GameMath.Sqrt(advancedParticleProperties.PosOffset[0].var + advancedParticleProperties.PosOffset[1].var + advancedParticleProperties.PosOffset[2].var) * num switch
                {
                    1 => 3f,
                    0 => 0.5f,
                    _ => 1.25f,
                };
                for (int i = 0; i <= power; i++)
                {
                    Api.World.SpawnParticles(advancedParticleProperties);
                }
            }
            if (damageType == EnumDamageType.Electricity)
            {
                int num = Math.Min(ElectricParticleProps.Length - 1, Api.World.Rand.Next(ElectricParticleProps.Length + 1));
                AdvancedParticleProperties advancedParticleProperties = ElectricParticleProps[num];
                advancedParticleProperties.basePos.Set(entity.SidedPos.X, entity.SidedPos.Y + (double)(entity.SelectionBox.YSize / 2f), entity.Pos.Z);
                advancedParticleProperties.PosOffset[0].var = entity.SelectionBox.XSize / 2f;
                advancedParticleProperties.PosOffset[1].var = entity.SelectionBox.YSize / 2f;
                advancedParticleProperties.PosOffset[2].var = entity.SelectionBox.ZSize / 2f;
                advancedParticleProperties.Velocity[0].avg = (float)entity.Pos.Motion.X * 10f;
                advancedParticleProperties.Velocity[1].avg = (float)entity.Pos.Motion.Y * 5f;
                advancedParticleProperties.Velocity[2].avg = (float)entity.Pos.Motion.Z * 10f;
                advancedParticleProperties.Quantity.avg = GameMath.Sqrt(advancedParticleProperties.PosOffset[0].var + advancedParticleProperties.PosOffset[1].var + advancedParticleProperties.PosOffset[2].var) * num switch
                {
                    1 => 3f,
                    0 => 0.5f,
                    _ => 1.25f,
                };
                for (int i = 0; i <= power; i++)
                {
                    Api.World.SpawnParticles(advancedParticleProperties);
                }
            }
            if (damageType == EnumDamageType.Heal)
            {
                int num = Math.Min(HealParticleProps.Length - 1, Api.World.Rand.Next(HealParticleProps.Length + 1));
                AdvancedParticleProperties advancedParticleProperties = HealParticleProps[num];
                advancedParticleProperties.basePos.Set(entity.SidedPos.X, entity.SidedPos.Y + (double)(entity.SelectionBox.YSize / 2f), entity.Pos.Z);
                advancedParticleProperties.PosOffset[0].var = entity.SelectionBox.XSize / 2f;
                advancedParticleProperties.PosOffset[1].var = entity.SelectionBox.YSize / 2f;
                advancedParticleProperties.PosOffset[2].var = entity.SelectionBox.ZSize / 2f;
                advancedParticleProperties.Velocity[0].avg = (float)entity.Pos.Motion.X * 10f;
                advancedParticleProperties.Velocity[1].avg = (float)entity.Pos.Motion.Y * 5f;
                advancedParticleProperties.Velocity[2].avg = (float)entity.Pos.Motion.Z * 10f;
                advancedParticleProperties.Quantity.avg = GameMath.Sqrt(advancedParticleProperties.PosOffset[0].var + advancedParticleProperties.PosOffset[1].var + advancedParticleProperties.PosOffset[2].var) * num switch
                {
                    1 => 3f,
                    0 => 0.5f,
                    _ => 1.25f,
                };
                for (int i = 0; i <= power; i++)
                {
                    Api.World.SpawnParticles(advancedParticleProperties);
                }
            }
            if (damageType == EnumDamageType.Injury)
            {
                int num = Math.Min(InjuryParticleProps.Length - 1, Api.World.Rand.Next(InjuryParticleProps.Length + 1));
                AdvancedParticleProperties advancedParticleProperties = InjuryParticleProps[num];
                advancedParticleProperties.basePos.Set(entity.SidedPos.X, entity.SidedPos.Y + (double)(entity.SelectionBox.YSize / 2f), entity.Pos.Z);
                advancedParticleProperties.PosOffset[0].var = entity.SelectionBox.XSize / 2f;
                advancedParticleProperties.PosOffset[1].var = entity.SelectionBox.YSize / 2f;
                advancedParticleProperties.PosOffset[2].var = entity.SelectionBox.ZSize / 2f;
                advancedParticleProperties.Velocity[0].avg = (float)entity.Pos.Motion.X * 10f;
                advancedParticleProperties.Velocity[1].avg = (float)entity.Pos.Motion.Y * 5f;
                advancedParticleProperties.Velocity[2].avg = (float)entity.Pos.Motion.Z * 10f;
                advancedParticleProperties.Quantity.avg = GameMath.Sqrt(advancedParticleProperties.PosOffset[0].var + advancedParticleProperties.PosOffset[1].var + advancedParticleProperties.PosOffset[2].var) * num switch
                {
                    1 => 3f,
                    0 => 0.5f,
                    _ => 1.25f,
                };
                for (int i = 0; i <= power; i++)
                {
                    Api.World.SpawnParticles(advancedParticleProperties);
                }
            }
            if (damageType == EnumDamageType.Poison)
            {
                int num = Math.Min(PoisonParticleProps.Length - 1, Api.World.Rand.Next(PoisonParticleProps.Length + 1));
                AdvancedParticleProperties advancedParticleProperties = PoisonParticleProps[num];
                advancedParticleProperties.basePos.Set(entity.SidedPos.X, entity.SidedPos.Y + (double)(entity.SelectionBox.YSize / 2f), entity.Pos.Z);
                advancedParticleProperties.PosOffset[0].var = entity.SelectionBox.XSize / 2f;
                advancedParticleProperties.PosOffset[1].var = entity.SelectionBox.YSize / 2f;
                advancedParticleProperties.PosOffset[2].var = entity.SelectionBox.ZSize / 2f;
                advancedParticleProperties.Velocity[0].avg = (float)entity.Pos.Motion.X * 10f;
                advancedParticleProperties.Velocity[1].avg = (float)entity.Pos.Motion.Y * 5f;
                advancedParticleProperties.Velocity[2].avg = (float)entity.Pos.Motion.Z * 10f;
                advancedParticleProperties.Quantity.avg = GameMath.Sqrt(advancedParticleProperties.PosOffset[0].var + advancedParticleProperties.PosOffset[1].var + advancedParticleProperties.PosOffset[2].var) * num switch
                {
                    1 => 3f,
                    0 => 0.5f,
                    _ => 1.25f,
                };
                for (int i = 0; i <= power; i++)
                {
                    Api.World.SpawnParticles(advancedParticleProperties);
                }
            }
        }
    }
}
