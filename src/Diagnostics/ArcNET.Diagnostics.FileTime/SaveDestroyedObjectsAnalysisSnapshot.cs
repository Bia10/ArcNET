namespace ArcNET.Diagnostics;

public sealed record SaveDestroyedObjectsAnalysisSnapshot(
    int ByteLength,
    bool HasAlignmentWarning,
    IReadOnlyList<string> ObjectIds
);
