namespace ArcNET.Diagnostics;

public sealed record SaveTypedContextSnapshot(
    DateTimeOffset CapturedAt,
    SaveTypedPlayerStateSnapshot? Player,
    SaveTownMapFogSnapshot TownMapFogs
);
