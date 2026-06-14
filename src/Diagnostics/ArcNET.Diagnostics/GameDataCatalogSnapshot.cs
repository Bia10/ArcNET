using ArcNET.GameData.Workspace;

namespace ArcNET.Diagnostics;

public sealed record class GameDataCatalogSnapshot(
    DateTimeOffset GeneratedAtUtc,
    bool IsAvailable,
    string Status,
    string Summary,
    IReadOnlyList<PrototypePaletteEntry> PrototypeEntries,
    IReadOnlyList<WorldMapCatalogEntry> WorldMapEntries,
    IReadOnlyList<TileArtCatalogEntry> TileArtEntries,
    IReadOnlyList<StaticObjectCatalogEntry> StaticObjectEntries,
    IReadOnlyList<string> Notes
);
