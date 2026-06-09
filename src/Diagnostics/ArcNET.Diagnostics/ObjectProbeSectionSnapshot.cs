namespace ArcNET.Diagnostics;

public sealed record class ObjectProbeSectionSnapshot(
    string Key,
    string Title,
    string SourceText,
    IReadOnlyList<ObjectProbeDetailSnapshot> Details
);
