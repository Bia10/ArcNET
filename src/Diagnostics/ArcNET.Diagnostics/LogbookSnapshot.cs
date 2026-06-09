namespace ArcNET.Diagnostics;

public sealed record class LogbookSnapshot(
    DateTimeOffset GeneratedAtUtc,
    bool IsAvailable,
    string Status,
    string Summary,
    string RequestedPageToken,
    LogbookPage? Page,
    string TargetHandleText,
    string TargetText,
    LogbookPayload Data,
    IReadOnlyList<string> Notes
);
