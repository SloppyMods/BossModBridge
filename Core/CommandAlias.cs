using System;
using System.Collections.Generic;

namespace BossModBridge.Core;

public static class CommandAlias
{
    private static readonly Dictionary<string, string> Targets = new(StringComparer.OrdinalIgnoreCase)
    {
        ["/vbm"] = "/bmr",
        ["/vbmai"] = "/bmrai",
    };

    public static IReadOnlyCollection<string> Aliases => Targets.Keys;

    public static string? Map(string command, string? args)
    {
        if (!Targets.TryGetValue(command, out var target))
            return null;
        var trimmed = args?.Trim();
        return string.IsNullOrEmpty(trimmed) ? target : $"{target} {trimmed}";
    }
}
