namespace ArcNET.Diagnostics;

public sealed record class QuestLogbookPageSnapshot(
    int Intelligence,
    bool UsesDumbText,
    IReadOnlyList<QuestLogbookEntrySnapshot> Entries,
    NativeReadSnapshot NativeRead
);
