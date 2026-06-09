namespace ArcNET.Diagnostics;

public sealed record SaveModifiedObjectsFileDetailSnapshot(
    string FileName,
    SaveModifiedObjectsAnalysisSnapshot Analysis
) : SaveEmbeddedFileDetailSnapshot(SaveEmbeddedFileDetailKind.ModifiedObjects, FileName);
