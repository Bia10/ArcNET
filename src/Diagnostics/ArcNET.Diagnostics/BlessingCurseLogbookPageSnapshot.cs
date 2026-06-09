namespace ArcNET.Diagnostics;

public sealed record class BlessingCurseLogbookPageSnapshot(
    IReadOnlyList<BlessingCurseLogbookEntrySnapshot> Entries,
    IReadOnlyList<NativeReadSnapshot> NativeReads
);
