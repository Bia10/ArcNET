namespace ArcNET.Diagnostics;

public sealed record class CeSourceAuditRequest(
    string? SourceRoot,
    string? Filter,
    string? Area,
    int Limit = 200,
    bool MissingOnly = false,
    bool CoveredOnly = false
);
