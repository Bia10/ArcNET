namespace ArcNET.Diagnostics;

public sealed record CharacterSarDumpEntrySnapshot(
    int BitsetId,
    int ElementSize,
    int ElementCount,
    int BitsetWordCount,
    string Annotation,
    string ValuePreview,
    bool IsFiller
);
