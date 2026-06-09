namespace ArcNET.Diagnostics;

public sealed record CharacterSarFullDumpEntrySnapshot(
    int Offset,
    int BitsetId,
    int ElementSize,
    int ElementCount,
    int BitsetWordCount,
    string Fingerprint,
    string Annotation,
    bool IsFiller,
    IReadOnlyList<CharacterSarInt32RowSnapshot> Int32Rows,
    string? ByteHex,
    string? ByteAscii,
    IReadOnlyList<CharacterSarElementHexSnapshot> ElementHexes,
    int OmittedElementCount,
    IReadOnlyList<int> BitSlots
);
