namespace ArcNET.Diagnostics;

public sealed record class BackgroundLogbookPageSnapshot(
    int BackgroundId,
    int BackgroundTextId,
    string? Name,
    string? Body,
    string? CatalogName,
    string? CatalogBody,
    NativeReadSnapshot BackgroundRead,
    NativeReadSnapshot BackgroundTextRead
);
