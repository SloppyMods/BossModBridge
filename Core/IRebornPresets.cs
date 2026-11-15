namespace BossModBridge.Core;

public interface IRebornPresets
{
    bool Available { get; }
    string? GetActive();
    bool SetActive(string name);
    bool ClearActive();
    bool Exists(string name);
}
