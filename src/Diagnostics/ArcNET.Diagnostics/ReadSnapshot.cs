namespace ArcNET.Diagnostics;

public sealed record class ReadSnapshot(
    DateTimeOffset GeneratedAtUtc,
    bool IsAvailable,
    string Status,
    string Summary,
    string AdapterKey,
    IReadOnlyList<string> RequestedArguments,
    string? TargetHandleText,
    string? TargetText,
    IReadOnlyList<ReadValueSnapshot> Values,
    NativeReadSnapshot? NativeRead,
    IReadOnlyList<string> Notes
);
