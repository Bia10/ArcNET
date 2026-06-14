namespace ArcNET.Diagnostics;

public sealed record class SpellTechMutationSnapshot(
    DateTimeOffset GeneratedAtUtc,
    bool IsAvailable,
    string Status,
    string Summary,
    string OperationText,
    string TargetHandleText,
    string TargetText,
    string SubjectText,
    string ValueText,
    string DispatcherText,
    string ExecutionDetailText,
    string ResultText,
    IReadOnlyList<string> Notes
);
