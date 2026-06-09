namespace ArcNET.Diagnostics;

public sealed record SaveTimeEventAnalysisSnapshot(
    bool IsTooShort,
    int ByteLength,
    int DeclaredCount,
    IReadOnlyList<SaveTimeEventEntrySnapshot> Entries,
    bool HasMoreEntries
);
