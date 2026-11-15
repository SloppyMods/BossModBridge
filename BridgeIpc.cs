using System;
using System.Collections.Generic;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using BossModBridge.Core;

namespace BossModBridge;

public sealed class BridgeIpc : IDisposable
{
    private readonly List<Action> unregister = [];

    public BridgeIpc(IDalamudPluginInterface pi, IPluginLog log, PresetBridge bridge)
    {
        Register<string, bool>(pi, log, "BossMod.Presets.Activate", bridge.Activate);
        Register<string, bool>(pi, log, "BossMod.Presets.Deactivate", bridge.Deactivate);
        Register<List<string>>(pi, log, "BossMod.Presets.GetActiveList", bridge.GetActiveList);
        Register<List<string>, bool>(pi, log, "BossMod.Presets.SetActiveList", bridge.SetActiveList);
        Register<string, bool, bool>(pi, log, "BossMod.Configuration.DisableModule", bridge.DisableModule);
    }

    public void Dispose()
    {
        foreach (var action in unregister)
            action();
        unregister.Clear();
    }

    private void Register<TRet>(IDalamudPluginInterface pi, IPluginLog log, string name, Func<TRet> func)
    {
        if (pi.GetIpcSubscriber<TRet>(name).HasFunction)
        {
            log.Warning($"{name} is already provided by another plugin; not registering");
            return;
        }
        var provider = pi.GetIpcProvider<TRet>(name);
        provider.RegisterFunc(func);
        unregister.Add(provider.UnregisterFunc);
    }

    private void Register<T1, TRet>(IDalamudPluginInterface pi, IPluginLog log, string name, Func<T1, TRet> func)
    {
        if (pi.GetIpcSubscriber<T1, TRet>(name).HasFunction)
        {
            log.Warning($"{name} is already provided by another plugin; not registering");
            return;
        }
        var provider = pi.GetIpcProvider<T1, TRet>(name);
        provider.RegisterFunc(func);
        unregister.Add(provider.UnregisterFunc);
    }

    private void Register<T1, T2, TRet>(IDalamudPluginInterface pi, IPluginLog log, string name, Func<T1, T2, TRet> func)
    {
        if (pi.GetIpcSubscriber<T1, T2, TRet>(name).HasFunction)
        {
            log.Warning($"{name} is already provided by another plugin; not registering");
            return;
        }
        var provider = pi.GetIpcProvider<T1, T2, TRet>(name);
        provider.RegisterFunc(func);
        unregister.Add(provider.UnregisterFunc);
    }
}
