namespace ArcNET.Diagnostics;

public sealed record class SheetMutationSnapshot(
    DateTimeOffset GeneratedAtUtc,
    bool IsAvailable,
    string Status,
    string Summary,
    string TargetHandleText,
    string TargetText,
    string FieldToken,
    string FieldDisplayName,
    SheetRoute Route,
    string ValueText,
    string TrainingText,
    string DispatcherText,
    string ExecutionDetailText,
    string ResultText,
    IReadOnlyList<string> Notes
);
