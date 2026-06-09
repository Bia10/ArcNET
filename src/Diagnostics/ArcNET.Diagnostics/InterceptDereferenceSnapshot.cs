namespace ArcNET.Diagnostics;

public sealed record class InterceptDereferenceSnapshot(
    string Source,
    string AddressText,
    int RequestedByteCount,
    int ReadByteCount,
    string Hex,
    string Ascii,
    IReadOnlyList<string> UInt32Preview,
    string? Error
);
