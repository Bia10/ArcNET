namespace ArcNET.Diagnostics;

public sealed record class ActionPointsMutationSnapshot(
    DateTimeOffset GeneratedAtUtc,
    int ProcessId,
    string ProcessName,
    string ModulePath,
    string Address,
    int Before,
    int After
);
