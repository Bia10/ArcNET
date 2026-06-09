namespace ArcNET.Diagnostics;

public sealed record SaveBinaryDiffRegionSnapshot(
    int Offset,
    byte[] BeforeBytes,
    byte[] AfterBytes,
    int ChangedByteCount
);
