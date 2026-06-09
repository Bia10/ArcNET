namespace ArcNET.Diagnostics;

public sealed record class GuidedActionSnapshot(
    DateTimeOffset GeneratedAtUtc,
    bool IsAvailable,
    string Status,
    string Summary,
    string ActionKey,
    string ActionDisplayName,
    string FunctionKey,
    string FunctionSite,
    string DispatcherText,
    string ExecutionDetailText,
    string ResultText
);
