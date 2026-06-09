namespace ArcNET.Diagnostics;

public sealed record PlayerSarTransitionReportEntrySnapshot(
    int FromSlot,
    int ToSlot,
    int FromLevel,
    int ToLevel,
    int FromRawBytesLength,
    int ToRawBytesLength,
    bool IsDiscontinuous,
    PlayerSarTransitionSummarySnapshot Summary,
    IReadOnlyList<PlayerSarTransitionListEntrySnapshot> Added,
    IReadOnlyList<PlayerSarTransitionListEntrySnapshot> Removed,
    IReadOnlyList<PlayerSarTransitionListEntrySnapshot> Moved,
    IReadOnlyList<PlayerSarTransitionChangedEntrySnapshot> Changed
);
