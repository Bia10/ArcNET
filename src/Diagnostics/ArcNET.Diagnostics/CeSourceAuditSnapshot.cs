namespace ArcNET.Diagnostics;

public sealed record class CeSourceAuditSnapshot(
    DateTimeOffset GeneratedAtUtc,
    string SourceRoot,
    bool AutoDetectedSourceRoot,
    string? Filter,
    string? Area,
    int Limit,
    bool MissingOnly,
    bool CoveredOnly,
    CeSourceAuditSymbolCatalogSnapshot? SymbolCatalog,
    CeSourceAuditSummarySnapshot Summary,
    IReadOnlyList<CeSourceAuditAreaSummary> Areas,
    IReadOnlyList<CeSourceFunctionSnapshot> Functions
);
