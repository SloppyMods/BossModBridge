using System.Collections.Generic;
using BossModBridge.Core;
using Xunit;

namespace BossModBridge.Tests;

public class PresetBridgeTests
{
    private sealed class FakePresets : IRebornPresets
    {
        public bool Available { get; set; } = true;
        public string? Active { get; set; }
        public HashSet<string> Known { get; } = new() { "A", "B", "C" };
        public List<string> Calls { get; } = new();

        public string? GetActive() { Calls.Add("GetActive"); return Active; }
        public bool Exists(string name) { Calls.Add($"Exists:{name}"); return Known.Contains(name); }
        public bool SetActive(string name)
        {
            Calls.Add($"SetActive:{name}");
            if (!Known.Contains(name)) return false;
            Active = name;
            return true;
        }
        public bool ClearActive()
        {
            Calls.Add("ClearActive");
            if (Active == null) return false;
            Active = null;
            return true;
        }

        public bool Mutated => Calls.Exists(c => c.StartsWith("SetActive") || c == "ClearActive");
    }

    private static (PresetBridge bridge, FakePresets fake, List<string> log) Make(bool available = true)
    {
        var fake = new FakePresets { Available = available };
        var log = new List<string>();
        return (new PresetBridge(fake, log.Add), fake, log);
    }

    [Fact]
    public void Unavailable_ReturnsSafeDefaultsWithoutCalls()
    {
        var (b, f, _) = Make(available: false);
        Assert.Empty(b.GetActiveList());
        Assert.False(b.Activate("A"));
        Assert.False(b.Deactivate("A"));
        Assert.False(b.SetActiveList(new List<string> { "A" }));
        Assert.Empty(f.Calls);
    }

    [Fact]
    public void GetActiveList_EmptyWhenNoneActive()
    {
        var (b, _, _) = Make();
        Assert.Empty(b.GetActiveList());
    }

    [Fact]
    public void GetActiveList_ReturnsActiveAsSingleItem()
    {
        var (b, f, _) = Make();
        f.Active = "B";
        Assert.Equal(new[] { "B" }, b.GetActiveList());
    }

    [Fact]
    public void Activate_UnknownPreset_ReturnsFalseWithoutSet()
    {
        var (b, f, _) = Make();
        Assert.False(b.Activate("Nope"));
        Assert.False(f.Mutated);
    }

    [Fact]
    public void Activate_AlreadyActive_ReturnsFalseWithoutSet()
    {
        var (b, f, _) = Make();
        f.Active = "A";
        Assert.False(b.Activate("A"));
        Assert.False(f.Mutated);
    }

    [Fact]
    public void Activate_IsCaseSensitiveForAlreadyActive()
    {
        var (b, f, _) = Make();
        f.Known.Add("a");
        f.Active = "A";
        Assert.True(b.Activate("a"));
        Assert.Equal("a", f.Active);
    }

    [Fact]
    public void Activate_Valid_SetsAndLogsReplacement()
    {
        var (b, f, log) = Make();
        f.Active = "A";
        Assert.True(b.Activate("B"));
        Assert.Equal("B", f.Active);
        Assert.NotEmpty(log);
    }

    [Fact]
    public void Activate_WhenNothingActive_Sets()
    {
        var (b, f, _) = Make();
        Assert.True(b.Activate("A"));
        Assert.Equal("A", f.Active);
    }

    [Fact]
    public void Deactivate_Unknown_ReturnsFalseWithoutClear()
    {
        var (b, f, _) = Make();
        f.Active = "A";
        Assert.False(b.Deactivate("Nope"));
        Assert.False(f.Mutated);
    }

    [Fact]
    public void Deactivate_NotActive_ReturnsFalseWithoutClear()
    {
        var (b, f, _) = Make();
        f.Active = "A";
        Assert.False(b.Deactivate("B"));
        Assert.False(f.Mutated);
        Assert.Equal("A", f.Active);
    }

    [Fact]
    public void Deactivate_Active_ClearsAndReturnsTrue()
    {
        var (b, f, _) = Make();
        f.Active = "A";
        Assert.True(b.Deactivate("A"));
        Assert.Null(f.Active);
    }

    [Fact]
    public void SetActiveList_Null_ReturnsFalseWithoutChanges()
    {
        var (b, f, _) = Make();
        Assert.False(b.SetActiveList(null!));
        Assert.False(f.Mutated);
    }

    [Fact]
    public void SetActiveList_UnknownName_ReturnsFalseAndChangesNothing()
    {
        var (b, f, _) = Make();
        f.Active = "C";
        Assert.False(b.SetActiveList(new List<string> { "A", "Nope" }));
        Assert.False(f.Mutated);
        Assert.Equal("C", f.Active);
    }

    [Fact]
    public void SetActiveList_Empty_ClearsAndReturnsTrue()
    {
        var (b, f, _) = Make();
        f.Active = "A";
        Assert.True(b.SetActiveList(new List<string>()));
        Assert.Null(f.Active);
        Assert.Contains("ClearActive", f.Calls);
    }

    [Fact]
    public void SetActiveList_EmptyWithNothingActive_StillReturnsTrue()
    {
        var (b, f, _) = Make();
        Assert.True(b.SetActiveList(new List<string>()));
        Assert.Null(f.Active);
    }

    [Fact]
    public void SetActiveList_Single_ActivatesIt()
    {
        var (b, f, log) = Make();
        Assert.True(b.SetActiveList(new List<string> { "B" }));
        Assert.Equal("B", f.Active);
        Assert.Empty(log);
    }

    [Fact]
    public void SetActiveList_Multiple_ActivatesFirstOnlyAndLogs()
    {
        var (b, f, log) = Make();
        Assert.True(b.SetActiveList(new List<string> { "B", "C", "A" }));
        Assert.Equal("B", f.Active);
        Assert.Single(f.Calls, c => c.StartsWith("SetActive"));
        Assert.NotEmpty(log);
    }

    [Fact]
    public void DisableModule_ReturnsFalseAndLogs()
    {
        var (b, _, log) = Make();
        Assert.False(b.DisableModule("Some.Module", true));
        Assert.Contains(log, l => l.Contains("not supported by BossMod Reborn"));
    }

    [Fact]
    public void NullLogger_IsAllowed()
    {
        var f = new FakePresets();
        var b = new PresetBridge(f);
        Assert.False(b.DisableModule("X", false));
    }
}

public class CommandAliasTests
{
    [Theory]
    [InlineData("/vbm", "", "/bmr")]
    [InlineData("/vbm", "   ", "/bmr")]
    [InlineData("/vbm", "cfg", "/bmr cfg")]
    [InlineData("/vbm", "  presets list  ", "/bmr presets list")]
    [InlineData("/vbmai", "", "/bmrai")]
    [InlineData("/vbmai", "on", "/bmrai on")]
    public void Map_KnownCommands(string command, string args, string expected)
        => Assert.Equal(expected, CommandAlias.Map(command, args));

    [Theory]
    [InlineData("/VBM", "cfg X Y", "/bmr cfg X Y")]
    [InlineData("/VbmAi", "off", "/bmrai off")]
    public void Map_IsCaseInsensitive(string command, string args, string expected)
        => Assert.Equal(expected, CommandAlias.Map(command, args));

    [Fact]
    public void Map_NullArgs_TreatedAsEmpty()
        => Assert.Equal("/bmr", CommandAlias.Map("/vbm", null));

    [Theory]
    [InlineData("/bmr")]
    [InlineData("/other")]
    [InlineData("")]
    public void Map_UnknownCommand_ReturnsNull(string command)
        => Assert.Null(CommandAlias.Map(command, "x"));

    [Fact]
    public void Aliases_ListsBothCommands()
        => Assert.Equal(new[] { "/vbm", "/vbmai" }, CommandAlias.Aliases);
}
