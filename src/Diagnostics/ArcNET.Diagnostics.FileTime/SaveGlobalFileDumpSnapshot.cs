namespace ArcNET.Diagnostics;

public sealed record SaveGlobalFileDumpSnapshot(
    int Header0,
    int Header1,
    int TotalInts,
    int TrailingBytes,
    int BeefCafeCount,
    int MinusOneCount,
    IReadOnlyList<SaveGlobalQuadPreviewRowSnapshot> QuadPreviewRows,
    IReadOnlyList<SaveGlobalHexPreviewRowSnapshot> HexRows,
    int HexOmittedBytes,
    IReadOnlyList<SaveGlobalIntPreviewRowSnapshot> IntRows,
    SaveGlobalNonZeroSummarySnapshot NonZeroSummary,
    IReadOnlyList<SaveGlobalAsciiCandidateSnapshot> AsciiCandidates,
    SaveGlobalSaveIdPairDetailsSnapshot? SaveIdPairDetails
);
