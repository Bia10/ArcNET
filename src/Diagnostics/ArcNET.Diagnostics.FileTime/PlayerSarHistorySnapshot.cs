namespace ArcNET.Diagnostics;

public sealed record PlayerSarHistorySnapshot(
    IReadOnlyList<PlayerSarSlotSnapshot> Slots,
    IReadOnlyList<PlayerSarTrackSnapshot> Tracks
);
