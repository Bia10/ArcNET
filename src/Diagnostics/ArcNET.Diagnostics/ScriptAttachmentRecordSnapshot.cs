namespace ArcNET.Diagnostics;

public sealed record class ScriptAttachmentRecordSnapshot(
    int ScriptNumber,
    uint Flags,
    string FlagsText,
    uint CountersPacked,
    string CountersPackedText,
    IReadOnlyList<int> Counters,
    bool IsEmpty
);
