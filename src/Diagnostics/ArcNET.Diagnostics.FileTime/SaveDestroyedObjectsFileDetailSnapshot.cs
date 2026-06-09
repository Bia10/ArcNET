namespace ArcNET.Diagnostics;

public sealed record SaveDestroyedObjectsFileDetailSnapshot(
    string FileName,
    SaveDestroyedObjectsAnalysisSnapshot Analysis
) : SaveEmbeddedFileDetailSnapshot(SaveEmbeddedFileDetailKind.DestroyedObjects, FileName);
