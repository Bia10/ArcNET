namespace ArcNET.Diagnostics;

public sealed record SaveCharacterSarDumpSnapshot(
    DateTimeOffset CapturedAt,
    string LeaderName,
    int LeaderLevel,
    IReadOnlyList<SaveCharacterSarRecordSnapshot> Records
);
