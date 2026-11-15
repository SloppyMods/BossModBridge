using System;
using System.Collections.Generic;

namespace BossModBridge.Core;

/// <summary>Maps BossMod's multi-active preset API onto BMR's single active preset.</summary>
public sealed class PresetBridge(IRebornPresets reborn, Action<string>? log = null)
{
    public List<string> GetActiveList()
    {
        if (!reborn.Available)
            return [];
        var active = reborn.GetActive();
        return active == null ? [] : [active];
    }

    public bool Activate(string name)
    {
        if (!reborn.Available || name == null || !reborn.Exists(name))
            return false;
        var active = reborn.GetActive();
        if (string.Equals(active, name, StringComparison.Ordinal))
            return false;
        if (active != null)
            log?.Invoke($"Activate '{name}' replaces active preset '{active}' (BossMod Reborn supports one active preset)");
        return reborn.SetActive(name);
    }

    public bool Deactivate(string name)
    {
        if (!reborn.Available || name == null || !reborn.Exists(name))
            return false;
        if (!string.Equals(reborn.GetActive(), name, StringComparison.Ordinal))
            return false;
        return reborn.ClearActive();
    }

    public bool SetActiveList(List<string> names)
    {
        if (!reborn.Available || names == null)
            return false;
        foreach (var name in names)
            if (name == null || !reborn.Exists(name))
                return false;
        if (names.Count == 0)
        {
            reborn.ClearActive();
            return true;
        }
        if (names.Count > 1)
            log?.Invoke($"SetActiveList: ignoring {names.Count - 1} extra preset(s); BossMod Reborn supports one active preset");
        return reborn.SetActive(names[0]);
    }

    public bool DisableModule(string name, bool disable)
    {
        log?.Invoke($"DisableModule('{name}', {disable}) is not supported by BossMod Reborn");
        return false;
    }
}
