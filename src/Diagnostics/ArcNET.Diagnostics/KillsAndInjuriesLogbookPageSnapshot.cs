namespace ArcNET.Diagnostics;

public sealed record class KillsAndInjuriesLogbookPageSnapshot(
    IReadOnlyList<KillLogbookSummaryEntrySnapshot> Summary,
    IReadOnlyList<InjuryLogbookEntrySnapshot> Injuries,
    NativeReadSnapshot NativeRead
);
