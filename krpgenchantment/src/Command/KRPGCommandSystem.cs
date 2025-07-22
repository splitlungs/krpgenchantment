using System;
using System.Linq;
using System.Numerics;
using System.Runtime.ConstrainedExecution;
using KRPGLib.Enchantment;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace KRPGLib.Enchantment
{

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class KRPGCmdResponse
    {
        public string response;
    }

    /// <summary>
    /// Core library for the Kronos RPG System
    /// </summary>
    public class KRPGCommandSystem : ModSystem
    {
        public ICoreServerAPI sApi { get; private set; }
        public override double ExecuteOrder()
        {
            return 1;
        }
        public override bool ShouldLoad(EnumAppSide side)
        {
            return side == EnumAppSide.Server;
        }
        public override void StartServerSide(ICoreServerAPI api)
        {
            sApi = api;

            // Register the main KRPG command
            sApi.ChatCommands.GetOrCreate("krpg")
            .WithDescription(Lang.Get("krpgenchantment:dsc-cmd-krpg"))
            .RequiresPrivilege(Privilege.controlserver)
            // .HandleWith((args) =>
            // {
            //     return TextCommandResult.Success(KRPGCommands.helpMessage);
            // })
            // .Validate()
            ;

            RegisterEnchantmentCommands();
            RegisterPlayerCommands();
            RegisterReloadCommands();
            RegisterVersionCommands();
        }
        private void RegisterEnchantmentCommands()
        {
            EnchantingConfigLoader configLoader = sApi.ModLoader.GetModSystem<EnchantingConfigLoader>();

            sApi.ChatCommands.GetOrCreate("krpg")
            .BeginSubCommand("enchantment")
            .WithDescription(Lang.Get("krpgenchantment:dsc-cmd-enchantment"))
            .RequiresPrivilege(Privilege.controlserver)
            .BeginSubCommand("enchant")
            .WithDescription(Lang.Get("krpgenchantment:dsc-cmd-enchantments-enchant"))
            .RequiresPrivilege(Privilege.controlserver)
            .HandleWith(_ =>
            {
                if (configLoader.ReloadConfig())
                {
                    return TextCommandResult.Success(Lang.Get("krpgenchantment:cmd-reloadcfg-msg"));
                }

                return TextCommandResult.Error(Lang.Get("krpgenchantment:cmd-reloadcfg-fail"));
            })
            .BeginSubCommand("reload")
            .WithDescription(Lang.Get("krpgenchantment:dsc-cmd-reload-config"))
            .RequiresPrivilege(Privilege.controlserver)
            .HandleWith(_ =>
            {
                if (configLoader.ReloadConfig())
                {
                    return TextCommandResult.Success(Lang.Get("krpgenchantment:cmd-reloadcfg-msg"));
                }

                return TextCommandResult.Error(Lang.Get("krpgenchantment:cmd-reloadcfg-fail"));
            })
            .EndSubCommand()
            .EndSubCommand()
            .EndSubCommand()
            .Validate();
        }
        private void RegisterPlayerCommands()
        {
            EnchantingConfigLoader configLoader = sApi.ModLoader.GetModSystem<EnchantingConfigLoader>();

            sApi.ChatCommands.GetOrCreate("krpg")
            .BeginSubCommand("player")
            .WithDescription(Lang.Get("krpgenchantment:dsc-cmd-player"))
            .HandleWith((args) => { return TextCommandResult.Success(KRPGCommands.helpPlayerMessage); })
            .EndSubCommand()
            .Validate();
        }
        private void RegisterReloadCommands()
        {
            EnchantingConfigLoader configLoader = sApi.ModLoader.GetModSystem<EnchantingConfigLoader>();

            sApi.ChatCommands.GetOrCreate("krpg")
            .BeginSubCommand("reload")
            .WithDescription(Lang.Get("krpgenchantment:dsc-cmd-reload"))
            .HandleWith((args) => { return TextCommandResult.Success("All KRPG configs reloaded."); })
            .EndSubCommand()
            .Validate();
        }
        private void RegisterVersionCommands()
        {
            EnchantingConfigLoader configLoader = sApi.ModLoader.GetModSystem<EnchantingConfigLoader>();

            sApi.ChatCommands.GetOrCreate("krpg")
            .BeginSubCommand("version")
            .WithDescription(Lang.Get("krpgenchantment:dsc-cmd-version"))
            .HandleWith((args) =>
            {
                string ver = null;
                string verResults = "";
                // Default catch-all
                if (args.ArgCount != 1)
                {
                    // All are manually registered here since they don't change often.
                    for (int i = 0; i < KRPGCommands.allKRPGMods.Length; i++)
                    {
                        // Set our Version
                        if (sApi.ModLoader.GetMod(KRPGCommands.allKRPGMods[i]) != null)
                            ver = sApi.ModLoader.GetMod(KRPGCommands.allKRPGMods[i]).Info.Version.ToString();
                        // Add a new line to our response
                        if (ver != null)
                            verResults += ("\n" + KRPGCommands.allKRPGMods[i] + " version: " + ver);
                        ver = null;
                    }
                }
                else
                {
                    if (KRPGCommands.allKRPGMods.Contains(args[0]))
                        verResults = (KRPGCommands.allKRPGMods.IndexOf(args[0]) + " version: " + ver);
                }
                return TextCommandResult.Success(verResults);
            })
            .EndSubCommand()
            .Validate();
        }
    }
}