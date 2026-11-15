using System;
using System.Collections.Generic;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using BossModBridge.Core;

namespace BossModBridge;

public sealed class Plugin : IDalamudPlugin
{
    private readonly ICommandManager commandManager;
    private readonly IPluginLog log;
    private readonly BridgeIpc ipc;
    private readonly List<string> registeredCommands = [];

    public Plugin(IDalamudPluginInterface pluginInterface, ICommandManager commandManager, IPluginLog log)
    {
        this.commandManager = commandManager;
        this.log = log;

        var bridge = new PresetBridge(new RebornPresetsIpc(pluginInterface, log), msg => log.Information(msg));
        ipc = new BridgeIpc(pluginInterface, log, bridge);

        foreach (var alias in CommandAlias.Aliases)
        {
            if (commandManager.Commands.ContainsKey(alias))
            {
                log.Warning($"{alias} is already registered by another plugin; not aliasing");
                continue;
            }
            var target = CommandAlias.Map(alias, null)!;
            var added = commandManager.AddHandler(alias, new CommandInfo((command, args) => OnAlias(command, args))
            {
                HelpMessage = $"Alias for {target} (BossMod bridge)",
                ShowInHelp = false,
            });
            if (added)
                registeredCommands.Add(alias);
            else
                log.Warning($"Could not register alias {alias}");
        }
    }

    public void Dispose()
    {
        foreach (var alias in registeredCommands)
            commandManager.RemoveHandler(alias);
        registeredCommands.Clear();
        ipc.Dispose();
    }

    private void OnAlias(string command, string args)
    {
        var mapped = CommandAlias.Map(command, args);
        if (mapped == null)
            return;
        if (!commandManager.ProcessCommand(mapped))
            log.Debug($"Alias target not handled: {mapped}");
    }
}
