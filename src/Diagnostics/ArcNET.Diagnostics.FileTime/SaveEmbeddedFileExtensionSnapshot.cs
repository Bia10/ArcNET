namespace ArcNET.Diagnostics;

public sealed record SaveEmbeddedFileExtensionSnapshot(
    string Extension,
    string DisplayExtension,
    int Count,
    long TotalBytes
);
