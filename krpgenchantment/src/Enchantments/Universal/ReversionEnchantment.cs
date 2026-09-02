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
using HarmonyLib;
using Vintagestory.API.Server;

namespace KRPGLib.Enchantment
{
    public class ReversionEnchantment : Enchantment
    {
        long TickDuration { get { return Modifiers.GetLong("TickDuration"); } }
        int PowerMultiplier { get { return Modifiers.GetInt("PowerMultiplier"); } }
        bool AllowRifts { get { return Modifiers.GetBool("AllowRifts"); } }
        bool UseHunger { get { return Modifiers.GetBool("UseHunger"); } }
        float HungerRate { get { return Modifiers.GetFloat("HungerRate"); } }
        /// <summary>
        /// Restores item durability in temporally unstable areas.
        /// </summary>
        /// <param name="api"></param>
        public ReversionEnchantment(ICoreAPI api) : base(api)
        {
            // Setup the default config
            Enabled = true;
            Code = "reversion";
            Category = "Universal";
            LoreCode = "enchantment-reversion";
            LoreChapterID = 19;
            MaxTier = 5;
            ValidToolTypes = new List<string>() {
                "Armor-Head", "Armor-Body", "Armor-Legs",
                "ArmorHead", "ArmorBody", "ArmorLegs",
                "Shield",
                "Chisel", "Cleaver", "Hammer", "Hoe", "Meter", "Pickaxe", "Probe", "Saw", "Scythe", "Shears", "Shovel", "Sickle", "Wrench",
                "Knife", "Axe",
                "Club", "Sword",
                "Spear",
                "Bow", "Sling",
                "Drill",
                "Halberd", "Mace", "Pike", "Polearm", "Poleaxe", "Quarterstaff", "Sabre", "Staff", "Warhammer",
                "Javelin",
                "Crossbow", "Firearm",
                "Wand",
                "vanillaarmory:Buckler", "vanillaarmory:Forlorn", "vanillaarmory:Club",
                "forgottenfirearms:gearlock-doublebarrel", "forgottenfirearms:gearlock-pistol", "forgottenfirearms:gearlock-repeater", "forgottenfirearms:gearlock-rifle"
            };
            Modifiers = new EnchantModifiers 
            {
                { "TickDuration", 10000 }, { "PowerMultiplier", 1 }, { "AllowRifts", true }, { "UseHunger", false }, { "HungerRate", 3f }
            };
            Version = 1.06f;
        }
        private ModSystemRifts riftSys;
        private SystemTemporalStability tempStabilitySys;
        public override void Initialize(EnchantmentProperties properties)
        {
            base.Initialize(properties);
            tempStabilitySys = Api.ModLoader.GetModSystem<SystemTemporalStability>();
            riftSys = Api.ModLoader.GetModSystem<ModSystemRifts>();
        }
        public override void OnEquip(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            EnchantmentEntityBehavior eeb = enchant.CauseEntity.GetBehavior<EnchantmentEntityBehavior>();
            if (eeb == null) return;
            // Get ID's
            int stackID = enchant.SourceStack.Id;
            int slotID = enchant.SourceSlot.Inventory.GetSlotId(enchant.SourceSlot);
            string codeID = Code + ":" + slotID + ":" + stackID;
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] CodeID for Reversion Tick is {0}.", codeID);
            if (!enchant.SourceSlot.Empty)
            {
                // Toggle On
                if (!eeb.TickRegistry.ContainsKey(codeID))
                {
                    EnchantTick eTick = enchant.ToEnchantTick();
                    eTick.SlotID = slotID;
                    eTick.Persistent = true;
                    eTick.IsHotbar = parameters.GetBool("IsHotbar");
                    eTick.IsOffhand = parameters.GetBool("IsOffhand");
                    eTick.TickDuration = TickDuration;
                    // { LastTickTime = 0, Source = enchant, TicksRemaining = 0, Persistent = true, IsHotbar = parameters.GetBool("IsHotbar") };
                    eeb.TickRegistry.Add(codeID, eTick);
                }
            }
            // Toggle Off - If Empty
            else
            {
                eeb.TickRegistry[codeID].Dispose();
            }
        }
        // TODO: Setup for UnEquip to control the tick dispose
        //
            public override void OnUnEquip(EnchantmentSource enchant, ref EnchantModifiers parameters)
        {
            // TEMP FOR TESTING
            EnchantmentEntityBehavior eeb = enchant?.CauseEntity?.GetBehavior<EnchantmentEntityBehavior>();
            if (eeb == null) return;
            // Get ID
            string codeID = parameters.GetString("tickID");
            if (EnchantingConfigLoader.Config?.Debug == true)
                Api.Logger.Event("[KRPGEnchantment] CodeID for Reversion Tick is {0}.", codeID);
            if (enchant.SourceSlot.Empty)
                eeb.TickRegistry[codeID].Dispose();
        }
        public override void OnTick(ref EnchantTick eTick)
        {
            if (!(Api is ICoreServerAPI api))
            {
                Api.Logger.Event("[KRPGEnchantment] Failed to get ICoreServerAPI for a Reversion tick. Disposing.");
                eTick.Dispose();
                return;
            }
            bool? debug = EnchantingConfigLoader.Config?.Debug;
            Entity entity = api.World.GetEntityById(eTick.CauseEntityID);
            if (entity == null)
            {
                Api.Logger.Event("[KRPGEnchantment] Failed to get the Entity for a Reversion tick. Disposing.");
                eTick.Dispose();
                return;
            }
            EnchantmentEntityBehavior eeb = entity.GetBehavior<EnchantmentEntityBehavior>();
            IInventory inventory;
            if (eTick.IsHotbar == true)
                inventory = eeb.hotbarInventory;
            else
                inventory = eeb.gearInventory;
            if (inventory == null)
            {
                Api.Logger.Event("[KRPGEnchantment] Failed to get the IInventory for a Reversion tick. Disposing.");
                eTick.Dispose();
                return;
            }
            ItemSlot slot = inventory[eTick.SlotID];
            if (slot == null)
            {
                Api.Logger.Event("[KRPGEnchantment] Failed to get the ItemSlot for a Reversion tick. Disposing.");
                eTick.Dispose();
                return;
            }
            if (slot.Empty == true)
            {
                Api.Logger.Event("[KRPGEnchantment] Failed to get the ItemStack for a Reversion tick. Disposing.");
                eTick.Dispose();
                return;
            }

            if (debug == true)
                Api.Logger.Event("[KRPGEnchantment] {0} is being affected by a Reversion enchantment.", slot.Itemstack.GetName());
            // Check for Hunger
            if (UseHunger == true)
            {
                EntityBehaviorHunger ebh = entity?.GetBehavior<EntityBehaviorHunger>();
                if (ebh.Saturation <= 0) 
                {
                    eTick.LastTickTime = api.World.ElapsedMilliseconds;
                    return;
                }
                float hungerRate;
                if (PowerMultiplier == 0)
                    hungerRate = HungerRate;
                else
                    hungerRate = PowerMultiplier * eTick.Power * HungerRate;
                ebh.ConsumeSaturation(hungerRate);
                ProcessReversion(slot, eTick.Power);
                eTick.LastTickTime = api.World.ElapsedMilliseconds;
                slot.MarkDirty();
                return;
            }
            // Check for Rifts nearby
            EntityPos causePos = entity.Pos;
            bool riftNear = false;
            if (AllowRifts && !UseHunger)
            {
                foreach (Rift r in riftSys.ServerRifts)
                {
                    double dt = causePos.DistanceTo(r.Position);
                    if (dt < 5)
                    {
                        if (debug == true)
                            Api.Logger.Event("[KRPGEnchantment] Rift found. Distance to player is {0}.", dt);
                        riftNear = true;
                        break;
                    }
                }
                // Process Reversion if a Rift is nearby
                if (riftNear == true)
                {
                    ProcessReversion(slot, eTick.Power);
                    eTick.LastTickTime = api.World.ElapsedMilliseconds;
                    slot.MarkDirty();
                    return;
                }
            }
            // Process Reversion if the Temporal Stability is below 1
            float stabf = tempStabilitySys.GetTemporalStability(causePos.AsBlockPos);
            if (stabf < 1 && !UseHunger)
            {
                ProcessReversion(slot, eTick.Power);
            }
            eTick.LastTickTime = api.World.ElapsedMilliseconds;
            slot.MarkDirty();
        }
        private void ProcessReversion(ItemSlot slot, int power)
        {
            if (slot.Empty == true) return;
            int amount = power * PowerMultiplier;
            int remDur = slot.Itemstack.Collectible.GetRemainingDurability(slot.Itemstack);
            int maxDur = slot.Itemstack.Collectible.GetMaxDurability(slot.Itemstack);
            if (remDur < maxDur)
            {
                remDur += amount;
                remDur = Math.Min(remDur, maxDur);
                slot.Itemstack.Attributes.SetInt("durability", remDur);
                if (EnchantingConfigLoader.Config?.Debug == true)
                    Api.Logger.Event("[KRPGEnchantment] Restoring {0} durability.", amount);
            }
        }
    }
}
