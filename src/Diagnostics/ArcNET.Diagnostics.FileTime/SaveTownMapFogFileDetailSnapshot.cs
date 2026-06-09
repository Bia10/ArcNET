namespace ArcNET.Diagnostics;

public sealed record SaveTownMapFogFileDetailSnapshot(string FileName, SaveTownMapFogFileAnalysisSnapshot Analysis)
    : SaveEmbeddedFileDetailSnapshot(SaveEmbeddedFileDetailKind.TownMapFog, FileName);
