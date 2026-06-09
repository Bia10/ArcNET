namespace ArcNET.Diagnostics;

public sealed record SaveGoldItemInspectionSnapshot(
    string LeaderName,
    int LeaderLevel,
    IReadOnlyList<SaveGoldItemFileSnapshot> Files
);
