namespace ArcNET.Diagnostics;

public sealed record class RumorLogbookPageSnapshot(
    int Intelligence,
    bool UsesDumbText,
    IReadOnlyList<RumorLogbookEntrySnapshot> Entries,
    NativeReadSnapshot NativeRead
);
