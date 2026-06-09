namespace ArcNET.Diagnostics;

public sealed record class ReputationLogbookPageSnapshot(
    IReadOnlyList<ReputationLogbookEntrySnapshot> Entries,
    NativeReadSnapshot NativeRead
);
