namespace ArcNET.Diagnostics;

public sealed record class LogbookEditorCatalogSnapshot(
    DateTimeOffset GeneratedAtUtc,
    bool IsAvailable,
    string Status,
    string Summary,
    IReadOnlyList<LogbookCatalogEntrySnapshot> Entries,
    IReadOnlyList<string> Notes
);
