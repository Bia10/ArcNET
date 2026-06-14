namespace ArcNET.Diagnostics;

public sealed record class LogbookMutationSnapshot(
    DateTimeOffset GeneratedAtUtc,
    bool IsAvailable,
    string Status,
    string Summary,
    LogbookMutationKind Kind,
    string OperationText,
    string TargetHandleText,
    string TargetText,
    string SubjectText,
    string ValueText,
    string AuxiliaryText,
    string DispatcherText,
    string ExecutionDetailText,
    string ResultText,
    IReadOnlyList<string> Notes
);
