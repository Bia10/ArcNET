namespace ArcNET.Diagnostics;

public sealed record SaveDynamicMobileFileDetailSnapshot(string FileName, SaveDynamicMobileAnalysisSnapshot Analysis)
    : SaveEmbeddedFileDetailSnapshot(SaveEmbeddedFileDetailKind.DynamicMobiles, FileName);
