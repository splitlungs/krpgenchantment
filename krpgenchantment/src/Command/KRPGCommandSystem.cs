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
        public ICoreAPI Api { get; private set; }
        public ICoreServerAPI sApi { get; private set; }
        public ICoreClientAPI cApi { get; private set; }
        public override double ExecuteOrder()
        {
            return 1;
        }
        public override bool ShouldLoad(EnumAppSide side)
        {
            return side == EnumAppSide.Server;
        }
        public override void Start(ICoreAPI api)
        {
            Api = api;
            cApi = api as ICoreClientAPI;
            sApi = api as ICoreServerAPI;

            // Register the main KRPG command
            Api.ChatCommands.GetOrCreate("krpg")
            .WithDescription(Lang.Get("krpgenchantment:cmd-krpg-help"))
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
            KRPGEnchantmentSystem eSystem = sApi.ModLoader.GetModSystem<KRPGEnchantmentSystem>();
            CommandArgumentParsers parsers = sApi.ChatCommands.Parsers;

            Api.ChatCommands.GetOrCreate("krpg")
            .BeginSubCommand("enchantment")
            .WithAlias("e")
            .WithDescription(Lang.Get("krpgenchantment:cmd-enchantment-help"))
            .RequiresPrivilege(Privilege.controlserver)
            // .HandleWith(_ =>
            // {
            //     if (KRPGCommands.EnchantsHandler())
            //     {
            //         return TextCommandResult.Success(Lang.Get("krpgenchantment:cmd-e-success"));
            //     }
            // 
            //     return TextCommandResult.Error(Lang.Get("krpgenchantment:cmd-e-fail"));
            // })
            .BeginSubCommand("add")
            .WithDescription(Lang.Get("krpgenchantment:cmd-e-add-help"))
            .RequiresPrivilege(Privilege.give)
            .WithArgs(parsers.OptionalWord("name"), parsers.OptionalInt("power"))
            .HandleWith(args =>
            {
                // sApi.Logger.Event("Arg0 is {0} and Arg1 is {1}", args[0].ToString().ToLower(), args[1]);
                if (KRPGCommands.EnchantsAddHandler(sApi, args))
                {
                    return TextCommandResult.Success(Lang.Get("krpgenchantment:cmd-e-add-success"));
                }
                
                return TextCommandResult.Error(Lang.Get("krpgenchantment:cmd-e-add-fail"));
            })
            .EndSubCommand()
            .BeginSubCommand("list")
            .WithAlias("l")
            .WithDescription(Lang.Get("krpgenchantment:cmd-e-list-help"))
            .RequiresPrivilege(Privilege.controlserver)
            .HandleWith(_ =>
            {
                string s = KRPGCommands.EnchantsListHandler(sApi);
                if (s != null)
                {
                    return TextCommandResult.Success(Lang.Get("krpgenchantment:cmd-e-list-success") + s);
                }

                return TextCommandResult.Error(Lang.Get("krpgenchantment:cmd-e-list-fail"));
            })
            .EndSubCommand()
            .BeginSubCommand("remove")
            .WithDescription(Lang.Get("krpgenchantment:cmd-e-remove-help"))
            .RequiresPrivilege(Privilege.give)
            .WithArgs(parsers.OptionalWord("name"))
            .HandleWith(args =>
            {
                if (KRPGCommands.EnchantsRemoveHandler(sApi, args))
                {
                    return TextCommandResult.Success(Lang.Get("krpgenchantment:cmd-e-remove-success"));
                }

                return TextCommandResult.Error(Lang.Get("krpgenchantment:cmd-remove-fail"));
            })
            .EndSubCommand()
            .BeginSubCommand("removeall")
            .WithDescription(Lang.Get("krpgenchantment:cmd-e-removeall-help"))
            .RequiresPrivilege(Privilege.give)
            .HandleWith(args =>
            {
                if (KRPGCommands.EnchantsRemoveAllHandler(sApi, args))
                {
                    return TextCommandResult.Success(Lang.Get("krpgenchantment:cmd-e-removeall-success"));
                }

                return TextCommandResult.Error(Lang.Get("krpgenchantment:cmd-e-removeall-fail"));
            })
            .EndSubCommand()
            .BeginSubCommand("latentreset")
            .WithDescription(Lang.Get("krpgenchantment:cmd-e-latentreset-help"))
            .RequiresPrivilege(Privilege.give)
            .HandleWith(args =>
            {
                if (KRPGCommands.LatentResetHandler(sApi, args))
                {
                    return TextCommandResult.Success(Lang.Get("krpgenchantment:cmd-e-latentreset-success"));
                }

                return TextCommandResult.Error(Lang.Get("krpgenchantment:cmd-e-latentreset-fail"));
            })
            .EndSubCommand()
            .BeginSubCommand("reload")
            .WithAlias("r")
            .WithDescription(Lang.Get("krpgenchantment:cmd-reload-config-help"))
            .RequiresPrivilege(Privilege.controlserver)
            .HandleWith(_ =>
            {
                if (configLoader.ReloadConfig())
                {
                    return TextCommandResult.Success(Lang.Get("krpgenchantment:cmd-reloadcfg-success"));
                }

                return TextCommandResult.Error(Lang.Get("krpgenchantment:cmd-reloadcfg-fail"));
            })
            .EndSubCommand()
            .EndSubCommand()
            .Validate();
        }
        private void RegisterPlayerCommands()
        {
            EnchantingConfigLoader configLoader = sApi.ModLoader.GetModSystem<EnchantingConfigLoader>();

            Api.ChatCommands.GetOrCreate("krpg")
            .BeginSubCommand("player")
            .WithDescription(Lang.Get("krpgenchantment:cmd-player-help"))
            .HandleWith((args) => { return TextCommandResult.Success(KRPGCommands.helpPlayerMessage); })
            .EndSubCommand()
            .Validate();
        }
        private void RegisterReloadCommands()
        {
            EnchantingConfigLoader configLoader = sApi.ModLoader.GetModSystem<EnchantingConfigLoader>();

            Api.ChatCommands.GetOrCreate("krpg")
            .BeginSubCommand("reload")
            .WithDescription(Lang.Get("krpgenchantment:cmd-reload-help"))
            .HandleWith((args) => 
            {
                if (configLoader.ReloadConfig())
                {
                    return TextCommandResult.Success("All KRPG configs reloaded.");
                }

                return TextCommandResult.Error(Lang.Get("krpgenchantment:cmd-reloadcfg-fail"));
            })
            .EndSubCommand()
            .Validate();
        }
        private void RegisterVersionCommands()
        {
            EnchantingConfigLoader configLoader = sApi.ModLoader.GetModSystem<EnchantingConfigLoader>();

            Api.ChatCommands.GetOrCreate("krpg")
            .BeginSubCommand("version")
            .WithDescription(Lang.Get("krpgenchantment:cmd-version-help"))
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