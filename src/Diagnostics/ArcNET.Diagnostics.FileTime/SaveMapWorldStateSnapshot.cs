namespace ArcNET.Diagnostics;

public sealed record SaveMapWorldStateSnapshot(
    string MapName,
    int DestroyedObjectCount,
    int ModifiedObjectCount,
    int DynamicMobileCount,
    int ObjectDiffCount
);
