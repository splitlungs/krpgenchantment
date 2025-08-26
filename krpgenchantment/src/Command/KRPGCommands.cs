using System;
using System.Collections.Generic;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using KRPGLib.Enchantment.API;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace KRPGLib.Enchantment
{
    public static class KRPGCommands
    {
        public static bool EnchantsHandler()
        {
            return true;
        }
        public static bool EnchantsAddHandler(ICoreServerAPI api, TextCommandCallingArgs args)
        {
            // Try to Enchant the item
            ItemSlot activeSlot =  args.Caller.Player.InventoryManager.ActiveHotbarSlot;
            if (activeSlot?.Empty != true) {
                string eCode = args.RawArgs[0].ToString().ToLower();
                int ePower = args.RawArgs[1].ToInt();
                ItemStack outStack = activeSlot.Itemstack;
                IEnchantment ench = api.EnchantAccessor().GetEnchantment(eCode);
                bool didEnchant = ench.TryEnchantItem(ref outStack, ePower, api);
                if (EnchantingConfigLoader.Config?.Debug == true)
                    api.Logger.Event("[KRPGEnchantment] Write completed with status: {0}.", didEnchant);
                if (didEnchant == true) return true;
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
        public static bool EnchantsRemoveHandler(ICoreServerAPI api)
        {
            return true;
        }

        public static string helpMessage =
        "------------------ Kronos RPG Help ---------------------" +
        "\nkrpg help : Display help information on a specified command. Default display all commands." +
        "\nkrpg enchantment : View or manage enchantments." +
        "\nkrpg player : View or configure player info." +
        "\nkrpg reload : Reload the config file of a mod. WARNING: This could be dangerous!" +
        "\nkrpg version : Display currently installed version of a mod." +
        "\n--------------------------------------------------------------"
        ;

        public static string helpPlayerMessage =
        "-------------- Kronos RPG Player Help ----------------" +
        "\nkrpg player list : Display a list of players in the server database." +
        "\nkrpg player Player delete : Delete all of the specified player's KRPG data from the server." +
        "\nkrpg player Player stats : Display all stats of the specificed player." +
        "\nkrpg player Player get Stat : Get a specified stat for the player." +
        "\nkrpg player Player get Stat Value: Set a specified stat for the player to a specified value." +
        "\n--------------------------------------------------------------"
        ;

        public static string helpReloadMessage =
        "-------------- Kronos RPG Reload Help ----------------" +
        "\nkrpg reload : Reload the config files for all KRPG Mods. WARNING: This could be dangerous!" +
        "\nkrpg reload ModID : Reload the config file for a specific KRPG Mod. WARNING: This could be dangerous!" +
        "\n--------------------------------------------------------------"
        ;

        public static string helpVersionMessage =
        "-------------- Kronos RPG Version Help ---------------" +
        "\nkrpg version : Display currently installed version of all KRPG Mods." +
        "\nkrpg version ModID : Display currently installed version a specified mod." +
        "\n--------------------------------------------------------------"
        ;

        public static string[] allKRPGMods = { "krpgarmory", "krpgclasses", "krpgenchantment", "krpglib", "krpgstats", "krpgwands" };
    }
}
