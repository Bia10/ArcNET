namespace ArcNET.Diagnostics;

public sealed record SaveValidationIssueSnapshot(DiagnosticIssueSeverity Severity, string? FilePath, string Message);
