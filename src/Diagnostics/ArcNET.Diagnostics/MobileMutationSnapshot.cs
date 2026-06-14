namespace ArcNET.Diagnostics;

public sealed record class MobileMutationSnapshot(
    DateTimeOffset GeneratedAtUtc,
    bool IsAvailable,
    string Status,
    string Summary,
    string TargetHandleText,
    string TargetText,
    string PrototypeHandleText,
    string StatNameText,
    string StatValueText,
    string DispatcherText,
    string ExecutionDetailText,
    string ResultText,
    IReadOnlyList<string> Notes
);
