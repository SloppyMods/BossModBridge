using System;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Plugin.Ipc;
using BossModBridge.Core;

namespace BossModBridge;

public sealed class RebornPresetsIpc : IRebornPresets
{
    private readonly IPluginLog log;
    private readonly ICallGateSubscriber<string?> getActive;
    private readonly ICallGateSubscriber<string, bool> setActive;
    private readonly ICallGateSubscriber<bool> clearActive;
    private readonly ICallGateSubscriber<string, string?> get;

    public RebornPresetsIpc(IDalamudPluginInterface pluginInterface, IPluginLog log)
    {
        this.log = log;
        getActive = pluginInterface.GetIpcSubscriber<string?>("BossMod.Presets.GetActive");
        setActive = pluginInterface.GetIpcSubscriber<string, bool>("BossMod.Presets.SetActive");
        clearActive = pluginInterface.GetIpcSubscriber<bool>("BossMod.Presets.ClearActive");
        get = pluginInterface.GetIpcSubscriber<string, string?>("BossMod.Presets.Get");
    }

    public bool Available
        => Safe(() => getActive.HasFunction && setActive.HasFunction && clearActive.HasFunction && get.HasFunction, false, nameof(Available));

    public string? GetActive() => Safe(getActive.InvokeFunc, null, nameof(GetActive));
    public bool SetActive(string name) => Safe(() => setActive.InvokeFunc(name), false, nameof(SetActive));
    public bool ClearActive() => Safe(clearActive.InvokeFunc, false, nameof(ClearActive));
    public bool Exists(string name) => Safe(() => get.InvokeFunc(name) != null, false, nameof(Exists));

    private T Safe<T>(Func<T> call, T fallback, string what)
    {
        try
        {
            return call();
        }
        catch (Exception ex)
        {
            log.Debug($"BMR preset IPC {what} failed: {ex.Message}");
            return fallback;
        }
    }
}
