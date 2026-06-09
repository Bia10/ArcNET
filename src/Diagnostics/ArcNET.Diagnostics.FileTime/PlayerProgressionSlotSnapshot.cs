namespace ArcNET.Diagnostics;

public sealed record PlayerProgressionSlotSnapshot(
    int Slot,
    string SlotStem,
    string LeaderName,
    int LeaderLevel,
    bool IsBaseline,
    PlayerProgressionStateSnapshot? State,
    IReadOnlyList<PlayerProgressionChangeSnapshot> Changes
);
