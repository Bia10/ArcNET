namespace ArcNET.Diagnostics.Contracts;

public sealed record class ProbeProfile(
    string Key,
    string DisplayName,
    string Description,
    IReadOnlyList<string> Selectors
);
