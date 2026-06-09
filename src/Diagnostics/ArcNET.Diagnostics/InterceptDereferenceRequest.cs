namespace ArcNET.Diagnostics;

public sealed record class InterceptDereferenceRequest(
    string Source,
    InterceptDereferenceSourceKind SourceKind,
    int Index,
    int ByteCount
);
