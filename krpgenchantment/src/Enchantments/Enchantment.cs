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
using Newtonsoft.Json.Linq;
using SkiaSharp;
using System.Collections;

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
        // Used for organizing the types of enchantments. Limited in the Config file
        public string Category = "Universal";
        // The code used to lookup the enchantment's Lore in the lang file
        public string LoreCode = "enchantment-lore";
        // The ID of the chapter in the Lore config file
        public int LoreChapterID = 0;
        // The maximum functional Power of an Enchantment
        public int MaxTier = 5;
        // The EnumTool types in string format which can receive this enchantment
        public List<string> ValidToolTypes = new List<string>();
        // Configurable JSON multiplier for effects
        public EnchantModifiers Modifiers;
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
                Category = Category,
                LoreCode = LoreCode,
                LoreChapterID = LoreChapterID,
                MaxTier = MaxTier,
                ValidToolTypes = ValidToolTypes,
                Modifiers = Modifiers
            };

            return val;
        }
        public void ToBytes(BinaryWriter writer)
        {
            writer.Write(Enabled);
            writer.Write(Code);
            writer.Write(Category);
            writer.Write(LoreCode);
            writer.Write(LoreChapterID);
            writer.Write(MaxTier);
            writer.Write(ValidToolTypes.Count);
            foreach (string s in ValidToolTypes)
                writer.Write(s);
            writer.Write(Modifiers.Count);
            foreach (KeyValuePair<string, object> enchant in Modifiers)
            {
                writer.Write(enchant.Key);
                writer.Write(enchant.Value.ToString());
            }
        }
        public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
        {
            Enabled = reader.ReadBoolean();
            Code = reader.ReadString();
            Category = reader.ReadString();
            LoreCode = reader.ReadString();
            LoreChapterID = reader.ReadInt32();
            MaxTier = reader.ReadInt32();
            int count1 = reader.ReadInt32();
            ValidToolTypes = new List<string>();
            for (int i = 0; i < count1; i++)
            {
                ValidToolTypes[i] = reader.ReadString();
            }
            int count2 = reader.ReadInt32();
            Modifiers = new EnchantModifiers();
            for (int i = 0; i < count2; i++)
            {
                string key = reader.ReadString();
                Modifiers[key] = reader.ReadString();
            }
        }
    }
    /// <summary>
    /// Generic for creating Tick Registries
    /// </summary>
    public class EnchantTick : IDisposable
    {
        public EnchantmentSource Source;
        public int TicksRemaining;
        public long LastTickTime;

        public void Dispose()
        {
            Source = null;
            TicksRemaining = 0;
            LastTickTime = 0;
        }
    }
    /// <summary>
    /// Base class for an Enchantment.
    /// </summary>
    public abstract class Enchantment : IEnchantment
    {
        // Set during instantiation
        public ICoreAPI Api { get; set; }
        // Define which registered class to instantiate with
        public string ClassName { get; set; }
        // Toggles processing of this enchantment
        public bool Enabled { get; set; }
        // How the Enchantment is referenced in code
        public string Code { get; set; }
        // Used for organizing the types of enchantments. Limited in the Config file
        public string Category { get; set; }
        // The code used to lookup the enchantment's Lore in the lang file
        public string LoreCode { get; set; }
        // The ID of the chapter in the Lore config file
        public int LoreChapterID { get; set; }
        // The maximum functional Power of an Enchantment
        public int MaxTier { get; set; }
        // The EnumTool types in string format which can receive this enchantment
        public List<string> ValidToolTypes { get; set; }
        // Similar to "Attributes". You can set your own serializable values here
        public EnchantModifiers Modifiers { get; set; }
        // Used to manage generic ticks. You still have to register your tick method with the API.
        public Dictionary<long, EnchantTick> TickRegistry { get; set; }
        // Properties loaded from JSON
        // public EnchantmentProperties Properties = new EnchantmentProperties();
        // Raw JSON of the Properties
        public JsonObject PropObject { get; set; }

        public Enchantment(ICoreAPI api)
        {
            // Setup the default config
            Api = api;
        }
        /// <summary>
        /// Called right after the Enchantment is created. Must call base method to load JSON Properties.
        /// </summary>
        /// <param name="properties"></param>
        public virtual void Initialize(EnchantmentProperties properties)
        {
            Enabled = properties.Enabled;
            Code = properties.Code;
            Category = properties.Category;
            LoreCode = properties.LoreCode;
            LoreChapterID = properties.LoreChapterID;
            MaxTier = properties.MaxTier;
            ValidToolTypes = properties.ValidToolTypes;
            Modifiers = properties.Modifiers;
            TickRegistry = new Dictionary<long, EnchantTick>();
            ConfigParticles();
        }
        /// <summary>
        /// Attempt to write this Enchantment to provided ItemStack. Returns null if it cannot enchant the item.
        /// </summary>
        /// <param name="inStack"></param>
        /// <param name="enchantPower"></param>
        /// <returns></returns>
        public virtual bool TryEnchantItem(ref ItemStack inStack, int enchantPower)
        {
            if (inStack == null) return false;

            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] Enchantment {0} is attempting to Enchant Item {1}.", Code, inStack.GetName());

            // Setup Enchants
            Dictionary<string, int> enchants = Api.EnchantAccessor().GetEnchantments(inStack);
            if (enchants != null)
            {
                OverwriteHealOrDamage(ref enchants);
                LimitEnchantCategory(ref enchants);
            }
            else
                enchants = new Dictionary<string, int>();
            // Write Enchant
            enchants.Add(Code.ToLower(), enchantPower);
            string enchantString = "";
            foreach (KeyValuePair<string, int> pair in enchants)
            {
                enchantString += pair.Key + ":" + pair.Value + ";";
            }
            ITreeAttribute tree = inStack.Attributes.GetOrAddTreeAttribute("enchantments");
            tree.SetString("active", enchantString);
            tree.RemoveAttribute("latentEnchantTime");
            tree.RemoveAttribute("latentEnchants");
            inStack.Attributes.MergeTree(tree);

            return true;

        }
        /// <summary>
        /// Overwrites "Healing" or a "Damage" category enchant
        /// </summary>
        /// <param name="enchants"></param>
        public virtual void OverwriteHealOrDamage(ref Dictionary<string, int> enchants)
        {
            List<string> damageEnchants = Api.EnchantAccessor().GetEnchantmentsInCategory("damage");
            if (damageEnchants != null)
            {
                // Overwrite Healing
                if (Code == "healing")
                {
                    foreach (string s in damageEnchants)
                        if (enchants.ContainsKey(s)) enchants.Remove(s);
                }
                // Overwrite Alternate Damage
                else if (damageEnchants.Contains(Code))
                    enchants.Remove("healing");
            }
        }
        /// <summary>
        /// Removes a random Enchantment of a type as limited by MaxEnchantsByCategory in the main config file.
        /// </summary>
        /// <param name="enchants"></param>
        public virtual void LimitEnchantCategory(ref Dictionary<string, int> enchants)
        {
            foreach (KeyValuePair<string, int> pair1 in EnchantingConfigLoader.Config.MaxEnchantsByCategory)
            {
                List<string> categoryEnchants = Api.EnchantAccessor().GetEnchantmentsInCategory(pair1.Key);
                if (categoryEnchants != null && pair1.Value >= 0)
                {
                    List<string> activeEnchants = new List<string>();
                    foreach (KeyValuePair<string, int> pair2 in enchants)
                    {
                        if (categoryEnchants.Contains(pair2.Key))
                            activeEnchants.Add(pair2.Key);
                    }
                    while (activeEnchants.Count >= pair1.Value)
                    {
                        int roll = Api.World.Rand.Next(0, activeEnchants.Count);
                        enchants.Remove(activeEnchants[roll]);
                    }
                }
            }
        }
        #nullable enable
        /// <summary>
        /// Generic method to execute a method matching the Trigger parameter. Called by the TriggerEnchant event in KRPGEnchantmentSystem.
        /// </summary>
        /// <param name="enchant"></param>
        /// <param name="parameters"></param>
        public virtual void OnTrigger(EnchantmentSource enchant, ref EnchantModifiers? parameters)
        {
            try
            {
                MethodInfo? meth = this.GetType().GetMethod(enchant.Trigger,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (parameters != null)
                    meth?.Invoke(this, new object[2] { enchant, parameters });
                else
                    meth?.Invoke(this, new object[1] { enchant });
            }
            catch(Exception ex) 
            {
                Api.Logger.Error("[KRPGEnchantment] Error attempting to trigger an Enchantment: {0}", ex);
            }
        }
        #nullable disable
        /// <summary>
        /// Generic method to execute a method matching the Trigger parameter. Called by the TriggerEnchant event in KRPGEnchantmentSystem.
        /// </summary>
        /// <param name="enchant"></param>
        /// <param name="parameters"></param>
        public virtual void OnTrigger(EnchantmentSource enchant)
        {
            try
            {
                MethodInfo meth = this.GetType().GetMethod(enchant.Trigger,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                meth.Invoke(this, new object[1] { enchant });
            }
            catch (Exception ex)
            {
                Api.Logger.Error("[KRPGEnchantment] Error attempting to trigger an Enchantment: {0}", ex);
            }
        }
        /// <summary>
        /// Triggered from an enchanted item when it successfully attacks an entity.
        /// </summary>
        /// <param name="enchant"></param>
        /// <param name="parameters"></param>
        public virtual void OnAttack(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
        
        }
        /// <summary>
        /// Triggered when an entity wearing an enchanted item is successfully attacked.
        /// </summary>
        /// <param name="enchant"></param>
        /// <param name="parameters"></param>
        public virtual void OnHit(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
        
        }

        protected AdvancedParticleProperties[] ParticleProps;
        protected static AdvancedParticleProperties[] PoisonParticleProps;
        protected bool resetLightHsv;

        public virtual void ConfigParticles()
        {
            ParticleProps = new AdvancedParticleProperties[3];
            ParticleProps[0] = new AdvancedParticleProperties
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
            ParticleProps[1] = new AdvancedParticleProperties
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
            ParticleProps[2] = new AdvancedParticleProperties
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
        public virtual void GenerateParticles(Entity entity, EnumDamageType damageType, float damage)
        {
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] Enchantment is generating particles for entity {0}.", entity.EntityId);

            int power = (int)MathF.Ceiling(damage);

            if (damageType == EnumDamageType.Fire)
            {
                int num = Math.Min(ParticleProps.Length - 1, Api.World.Rand.Next(ParticleProps.Length + 1));
                AdvancedParticleProperties advancedParticleProperties = ParticleProps[num];
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
                int num = Math.Min(ParticleProps.Length - 1, Api.World.Rand.Next(ParticleProps.Length + 1));
                AdvancedParticleProperties advancedParticleProperties = ParticleProps[num];
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
                int num = Math.Min(ParticleProps.Length - 1, Api.World.Rand.Next(ParticleProps.Length + 1));
                AdvancedParticleProperties advancedParticleProperties = ParticleProps[num];
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
                int num = Math.Min(ParticleProps.Length - 1, Api.World.Rand.Next(ParticleProps.Length + 1));
                AdvancedParticleProperties advancedParticleProperties = ParticleProps[num];
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
                int num = Math.Min(ParticleProps.Length - 1, Api.World.Rand.Next(ParticleProps.Length + 1));
                AdvancedParticleProperties advancedParticleProperties = ParticleProps[num];
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
