namespace ArcNET.Diagnostics.Windows;

public readonly record struct InterceptMemoryReadResult(
    bool Success,
    int RequestedByteCount,
    int ReadByteCount,
    byte[] Bytes,
    string? Error
);
