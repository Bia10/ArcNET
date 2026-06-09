namespace ArcNET.Diagnostics;

public sealed record CharacterSarDiffEntrySnapshot(
    CharacterSarDiffKind Kind,
    string Fingerprint,
    int OccurrenceIndex,
    int OccurrenceCount,
    string Annotation,
    int? BeforeElementCount,
    int? AfterElementCount,
    string? BeforeValueSummary,
    string? AfterValueSummary,
    IReadOnlyList<CharacterSarElementValueDiffSnapshot> ChangedElements
);
