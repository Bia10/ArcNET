namespace ArcNET.Diagnostics;

public sealed record SaveObjectFieldAuditSnapshot(
    int ObjectCount,
    int MobileFileCount,
    int MobileMdyMobCount,
    int DistinctFieldCount,
    int TotalPropertyCount,
    int ParseNoteCount,
    IReadOnlyList<ObjectFieldUsageSnapshot> TopFields,
    IReadOnlyList<ObjectFieldUsageSnapshot> LinkFields
);
