namespace ArcNET.Diagnostics;

public sealed record class ObjectProbeRequest(
    AttachedSessionSnapshot Session,
    IReadOnlyList<string> HandleTexts,
    string SourceLabel,
    int MaxObjects = 4
);
