namespace ArcNET.Diagnostics;

public sealed record SaveTypedContextDeltaSnapshot(
    SaveTypedPlayerDeltaSnapshot Player,
    SaveTownMapFogDeltaSnapshot TownMapFogs
);
