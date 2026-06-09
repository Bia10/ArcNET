namespace ArcNET.Diagnostics;

public sealed record SaveTimeEventFileDetailSnapshot(string FileName, SaveTimeEventAnalysisSnapshot Analysis)
    : SaveEmbeddedFileDetailSnapshot(SaveEmbeddedFileDetailKind.TimeEvents, FileName);
