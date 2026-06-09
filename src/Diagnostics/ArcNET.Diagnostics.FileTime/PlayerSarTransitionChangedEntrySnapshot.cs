namespace ArcNET.Diagnostics;

public sealed record PlayerSarTransitionChangedEntrySnapshot(
    string Label,
    string Fingerprint,
    string Annotation,
    int? BeforeElementCount,
    int? AfterElementCount,
    string? BeforeValueSummary,
    string? AfterValueSummary,
    IReadOnlyList<PlayerSarTransitionFieldDiffSnapshot> FieldDiffs,
    IReadOnlyList<int> BeforeBitSlots,
    IReadOnlyList<int> AfterBitSlots,
    int PointerNoiseSuppressedCount
);
