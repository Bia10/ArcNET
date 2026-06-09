namespace ArcNET.Diagnostics;

public sealed record class CeSourceCoverageSnapshot(
    bool WatchHookCoverage,
    bool DebuggerFunctionCoverage,
    bool SignatureCoverage,
    bool AnyCatalogCoverage
);
