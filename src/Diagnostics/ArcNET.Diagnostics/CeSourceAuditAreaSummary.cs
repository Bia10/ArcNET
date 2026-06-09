namespace ArcNET.Diagnostics;

public sealed record class CeSourceAuditAreaSummary(
    string Area,
    int FunctionCount,
    int CoveredCount,
    int MissingCount,
    int UniqueSymbolCount
);
