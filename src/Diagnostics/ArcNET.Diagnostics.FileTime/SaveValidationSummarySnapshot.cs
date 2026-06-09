namespace ArcNET.Diagnostics;

public sealed record SaveValidationSummarySnapshot(
    int IssueCount,
    int ErrorCount,
    int WarningCount,
    int InfoCount,
    int FileCountWithIssues
);
