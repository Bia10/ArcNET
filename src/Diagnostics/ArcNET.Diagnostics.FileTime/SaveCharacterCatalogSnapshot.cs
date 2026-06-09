namespace ArcNET.Diagnostics;

public sealed record SaveCharacterCatalogSnapshot(
    DateTimeOffset CapturedAt,
    string LeaderName,
    int LeaderLevel,
    IReadOnlyList<SaveCharacterCatalogRecordSnapshot> Records
);
