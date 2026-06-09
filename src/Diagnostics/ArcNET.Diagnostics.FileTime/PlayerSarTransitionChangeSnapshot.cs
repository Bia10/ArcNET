namespace ArcNET.Diagnostics;

public sealed record PlayerSarTransitionChangeSnapshot(
    PlayerSarTransitionChangeKind Kind,
    string Label,
    string Fingerprint,
    string Annotation,
    int? BeforeElementCount,
    int? AfterElementCount,
    string? BeforeValueSummary,
    string? AfterValueSummary,
    IReadOnlyList<CharacterSarElementValueDiffSnapshot> ElementDiffs,
    IReadOnlyList<int> BeforeBitSlots,
    IReadOnlyList<int> AfterBitSlots,
    int PointerNoiseSuppressedCount
);
