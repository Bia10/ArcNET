namespace ArcNET.Diagnostics;

public readonly record struct ResolvedTarget(
    ulong Handle,
    string HandleText,
    string TargetText,
    IReadOnlyList<string> Notes
);
