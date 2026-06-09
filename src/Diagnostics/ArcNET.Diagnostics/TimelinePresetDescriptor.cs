namespace ArcNET.Diagnostics;

public sealed record class TimelinePresetDescriptor(
    string Key,
    string DisplayName,
    string Description,
    IReadOnlyList<string> Selectors,
    IReadOnlyList<string> HookKeys,
    IReadOnlyList<string> Areas,
    bool UsesHighVolumeHooks
);
