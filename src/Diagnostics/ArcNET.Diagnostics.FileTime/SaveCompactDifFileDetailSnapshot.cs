namespace ArcNET.Diagnostics;

public sealed record SaveCompactDifFileDetailSnapshot(string FileName, SaveCompactDifAnalysisSnapshot Analysis)
    : SaveEmbeddedFileDetailSnapshot(SaveEmbeddedFileDetailKind.CompactDif, FileName);
