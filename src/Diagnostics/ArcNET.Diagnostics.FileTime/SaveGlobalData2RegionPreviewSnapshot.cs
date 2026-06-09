namespace ArcNET.Diagnostics;

public sealed record SaveGlobalData2RegionPreviewSnapshot(
    int IntCount,
    IReadOnlyList<int> HeadValues,
    IReadOnlyList<int> TailValues
);
