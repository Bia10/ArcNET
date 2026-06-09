namespace ArcNET.Diagnostics;

public sealed record CharacterSarAuditSnapshot(
    int Offset,
    int TotalBytes,
    int ElementSize,
    int ElementCount,
    int BitsetWordCount,
    int BitsetId,
    int BitSlotCount,
    string Fingerprint,
    string Annotation,
    IReadOnlyList<int> SampleValues,
    IReadOnlyList<int> BitSlots
);
