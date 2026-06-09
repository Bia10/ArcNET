namespace ArcNET.Diagnostics.Windows;

internal readonly record struct ResolvedTarget(
    ulong Handle,
    string HandleText,
    string TargetText,
    IReadOnlyList<string> Notes
);
