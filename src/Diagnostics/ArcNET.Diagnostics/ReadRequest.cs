namespace ArcNET.Diagnostics;

public sealed record class ReadRequest(
    AttachedSessionSnapshot Session,
    string AdapterKey,
    IReadOnlyList<string> Arguments
);
