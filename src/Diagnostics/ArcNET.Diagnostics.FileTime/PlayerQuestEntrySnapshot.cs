namespace ArcNET.Diagnostics;

public sealed record PlayerQuestEntrySnapshot(
    int ProtoId,
    string? Label,
    int Context,
    int Timestamp,
    int State,
    string StateDescription
);
