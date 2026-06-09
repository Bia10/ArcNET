namespace ArcNET.Diagnostics;

public sealed record CharacterSarEntrySnapshot(
    int Offset,
    int TotalBytes,
    int DataOffset,
    int ElementSize,
    int ElementCount,
    int BitsetWordCount,
    int BitsetId,
    int BitSlotCount,
    string Fingerprint,
    IReadOnlyList<int> Values,
    IReadOnlyList<int> BitSlots,
    bool IsFiller
)
{
    public string ValueSummary =>
        ElementSize == 4 ? CharacterSarDiagnostics.FormatInt32Preview(Values, 4) : "(non-int32)";
}
