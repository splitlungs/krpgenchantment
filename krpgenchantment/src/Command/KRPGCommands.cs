using System;
using System.Collections.Generic;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using KRPGLib.Enchantment.API;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.API.Datastructures;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace KRPGLib.Enchantment
{
    public static class KRPGCommands
    {
        public static bool EnchantsHandler()
        {
            return true;
        }
        /// <summary>
        /// Adds a given enchantment to the currently held item. Returns false if it fails to add the enchantment in any way.
        /// </summary>
        /// <param name="api"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static bool EnchantsAddHandler(ICoreServerAPI api, TextCommandCallingArgs args)
        {
            // Try to Enchant the item
            ItemSlot activeSlot =  args.Caller.Player.InventoryManager.ActiveHotbarSlot;
            if (activeSlot?.Empty != true) 
            {
                string eCode = args[0].ToString().ToLower();
                int ePower = args[1].ToString().ToInt();

                if (EnchantingConfigLoader.Config?.Debug == true)
                    api.Logger.Event("[KRPGEnchantment] {0} is attempting to add {1} {2} to {3} through commands.", args.Caller.GetName(), eCode, ePower, activeSlot?.Itemstack?.GetName());
                
                ItemStack outStack = activeSlot.Itemstack;
                IEnchantment ench = api.EnchantAccessor().GetEnchantment(eCode);
                if (ench == null) return false;
                bool didEnchant = ench.TryEnchantItem(ref outStack, ePower, api);
                if (EnchantingConfigLoader.Config?.Debug == true)
                    api.Logger.Event("[KRPGEnchantment] Write completed with status: {0}.", didEnchant);
                if (didEnchant == true)
                {
                    // Update cache
                    IPlayer player = args.Caller.Player;
                    int slotId = player.InventoryManager.GetHotbarInventory().GetSlotId(activeSlot);
                    EnchantmentEntityBehavior eeb = player.Entity.GetBehavior<EnchantmentEntityBehavior>();
                    eeb.GenerateHotbarEnchantCache(slotId);
                    // Then update client
                    activeSlot.MarkDirty();
                    return true;
                }
            }

            return false;
        }
        /// <summary>
        /// Returns a comma separated list of all of the registered enchantment codes. Returns null if none are found.
        /// </summary>
        /// <param name="api"></param>
        /// <returns></returns>
        public static string EnchantsListHandler(ICoreServerAPI api)
        {
            string eList = null;
            KRPGEnchantmentSystem sys = api.ModLoader.GetModSystem<KRPGEnchantmentSystem>();
            foreach (KeyValuePair<string, Enchantment> pair in sys.EnchantAccessor.EnchantmentRegistry)
            {
                if (eList != null) eList += ", " + pair.Value.Code;
                else eList += pair.Value.Code;
            }
            return eList;
        }
        /// <summary>
        /// Removes the given enchantment from the currently held item. Returns false if it fails to remove an enchantment in any way.
        /// </summary>
        /// <param name="api"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static bool EnchantsRemoveHandler(ICoreServerAPI api, TextCommandCallingArgs args)
        {
            ItemSlot activeSlot = args.Caller.Player.InventoryManager.ActiveHotbarSlot;
            if (activeSlot?.Empty != true)
            {
                string eCode = args[0].ToString().ToLower();

                if (EnchantingConfigLoader.Config?.Debug == true)
                    api.Logger.Event("[KRPGEnchantment] {0} is attempting to remove {1} from {2} through commands.", args.Caller.GetName(), eCode, activeSlot?.Itemstack?.GetName());
                bool didEnchant = api.EnchantAccessor().RemoveEnchantFromItem(eCode, activeSlot, args.Caller.Entity);
                if (EnchantingConfigLoader.Config?.Debug == true)
                    api.Logger.Event("[KRPGEnchantment] Write completed with status: {0}.", didEnchant);
                if (didEnchant == true)
                {
                    // Update cache
                    IPlayer player = args.Caller.Player;
                    int slotId = player.InventoryManager.GetHotbarInventory().GetSlotId(activeSlot);
                    EnchantmentEntityBehavior eeb = player.Entity.GetBehavior<EnchantmentEntityBehavior>();
                    eeb.GenerateHotbarEnchantCache(slotId);
                    // Then update client
                    activeSlot.MarkDirty();
                    return true;
                }
            }

            return false;
        }
        /// <summary>
        /// Removes all enchantments on the currently held item. Returns false if it fails to remove an enchantment in any way.
        /// </summary>
        /// <param name="api"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static bool EnchantsRemoveAllHandler(ICoreServerAPI api, TextCommandCallingArgs args)
        {
            ItemSlot activeSlot = args.Caller.Player.InventoryManager.ActiveHotbarSlot;
            if (activeSlot?.Empty != true)
            {
                if (EnchantingConfigLoader.Config?.Debug == true)
                    api.Logger.Event("[KRPGEnchantment] {0} is attempting to remove all enchantments from {1} through commands.", args.Caller.GetName(), activeSlot?.Itemstack?.GetName());
                bool didEnchant = api.EnchantAccessor().RemoveAllEnchantsFromItem(activeSlot, args.Caller.Entity);
                if (EnchantingConfigLoader.Config?.Debug == true)
                    api.Logger.Event("[KRPGEnchantment] Write completed with status: {0}.", didEnchant);
                if (didEnchant == true)
                {
                    // Update cache
                    IPlayer player = args.Caller.Player;
                    int slotId = player.InventoryManager.GetHotbarInventory().GetSlotId(activeSlot);
                    EnchantmentEntityBehavior eeb = player.Entity.GetBehavior<EnchantmentEntityBehavior>();
                    eeb.GenerateHotbarEnchantCache(slotId);
                    // Then update client
                    activeSlot.MarkDirty();
                    return true;
                }
            }

            return false;
        }
        /// <summary>
        /// Removes all enchantments on the currently held item. Returns false if it fails to remove an enchantment in any way.
        /// </summary>
        /// <param name="api"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static bool LatentResetHandler(ICoreServerAPI api, TextCommandCallingArgs args)
        {
            ItemSlot activeSlot = args.Caller.Player.InventoryManager.ActiveHotbarSlot;
            if (activeSlot?.Empty != true)
            {
                if (EnchantingConfigLoader.Config?.Debug == true)
                    api.Logger.Event("[KRPGEnchantment] {0} is attempting to reset the Latent Enchants from {1} through commands.", args.Caller.GetName(), activeSlot?.Itemstack?.GetName());
                bool didEnchant = api.EnchantAccessor().ResetLatentEnchantsOnItem(activeSlot);
                if (EnchantingConfigLoader.Config?.Debug == true)
                    api.Logger.Event("[KRPGEnchantment] Reset completed with status: {0}.", didEnchant);
                if (didEnchant == true)
                {
                    // Update cache
                    IPlayer player = args.Caller.Player;
                    int slotId = player.InventoryManager.GetHotbarInventory().GetSlotId(activeSlot);
                    EnchantmentEntityBehavior eeb = player.Entity.GetBehavior<EnchantmentEntityBehavior>();
                    eeb.GenerateHotbarEnchantCache(slotId);
                    // Then update client
                    activeSlot.MarkDirty();
                    return true;
                }
            }

            return false;
        }
        /// <summary>
        /// Learn Enchanter's Manual journal entries for the given player.
        /// </summary>
        /// <param name="api"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static bool LearnEnchantsHandler(ICoreServerAPI api, TextCommandCallingArgs args)
        {
            if (!(args.Caller.Player is IServerPlayer player)) return false;
            if (EnchantingConfigLoader.Config?.Debug == true)
                api.Logger.Event("[KRPGEnchantment] {0} is attempting to learn all Enchanter's Manuals through commands.", args.Caller.GetName());
            bool discovered = api.EnchantAccessor().LearnAllEnchantersManuals(player);
            return discovered;
        }
        // Legacy command responses
        [Obsolete]
        public static string helpMessage =
        "------------------ Kronos RPG Help ---------------------" +
        "\nkrpg help : Display help information on a specified command. Default display all commands." +
        "\nkrpg enchantment : View or manage enchantments." +
        "\nkrpg player : View or configure player info." +
        "\nkrpg reload : Reload the config file of a mod. WARNING: This could be dangerous!" +
        "\nkrpg version : Display currently installed version of a mod." +
        "\n--------------------------------------------------------------"
        ;
        [Obsolete]
        public static string helpPlayerMessage =
        "-------------- Kronos RPG Player Help ----------------" +
        "\nkrpg player list : Display a list of players in the server database." +
        "\nkrpg player Player delete : Delete all of the specified player's KRPG data from the server." +
        "\nkrpg player Player stats : Display all stats of the specificed player." +
        "\nkrpg player Player get Stat : Get a specified stat for the player." +
        "\nkrpg player Player get Stat Value: Set a specified stat for the player to a specified value." +
        "\n--------------------------------------------------------------"
        ;
        [Obsolete]
        public static string helpReloadMessage =
        "-------------- Kronos RPG Reload Help ----------------" +
        "\nkrpg reload : Reload the config files for all KRPG Mods. WARNING: This could be dangerous!" +
        "\nkrpg reload ModID : Reload the config file for a specific KRPG Mod. WARNING: This could be dangerous!" +
        "\n--------------------------------------------------------------"
        ;
        [Obsolete]
        public static string helpVersionMessage =
        "-------------- Kronos RPG Version Help ---------------" +
        "\nkrpg version : Display currently installed version of all KRPG Mods." +
        "\nkrpg version ModID : Display currently installed version a specified mod." +
        "\n--------------------------------------------------------------"
        ;
        public static string[] allKRPGMods = { "krpgarmory", "krpgclasses", "krpgenchantment", "krpglib", "krpgstats", "krpgwands" };
    }
}
