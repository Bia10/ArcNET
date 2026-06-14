namespace ArcNET.Diagnostics;

public sealed record class LogbookCatalogEntrySnapshot(
    string CategoryToken,
    int EntryId,
    int AuxiliaryId,
    string DisplayName,
    string DetailText
);
