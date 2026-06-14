namespace ArcNET.Diagnostics;

public sealed record class WorldMapDiscoveryExecutionResult(
    string DispatcherMode,
    string DispatcherSite,
    string ExecutionDetailText,
    string ResultText,
    int ProcessedLocationCount,
    int VisitedLocationCount,
    bool IsTravelerOnWorldMap,
    int CurrentMapId,
    int StartMapId,
    int TownMapId
);
