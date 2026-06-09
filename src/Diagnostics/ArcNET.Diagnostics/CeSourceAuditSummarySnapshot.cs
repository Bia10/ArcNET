namespace ArcNET.Diagnostics;

public sealed record class CeSourceAuditSummarySnapshot(
    int FunctionCount,
    int UniqueNameCount,
    int DuplicateNameCount,
    int SelectionCount,
    int WatchHookCoverageCount,
    int DebuggerFunctionCoverageCount,
    int SignatureCoverageCount,
    int AnyCatalogCoverageCount,
    int MissingCoverageCount,
    int UniqueSymbolMatchCount,
    int AmbiguousSymbolMatchCount
);
