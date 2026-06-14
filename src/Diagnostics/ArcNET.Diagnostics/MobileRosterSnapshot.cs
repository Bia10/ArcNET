namespace ArcNET.Diagnostics;

public sealed record class MobileRosterSnapshot(
    DateTimeOffset GeneratedAtUtc,
    bool IsAvailable,
    string Status,
    string Summary,
    IReadOnlyList<MobileRosterEntrySnapshot> Mobiles,
    IReadOnlyList<string> Notes
);
