namespace ArcNET.Diagnostics;

public sealed record class FunctionAuditSnapshot(
    int TotalFunctions,
    int ResolvedFunctions,
    int FailedFunctions,
    IReadOnlyList<FunctionAuditResultSnapshot> Results
);
