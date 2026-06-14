namespace ArcNET.GameData.Workspace;

public sealed record class TileArtCatalogEntry(
    uint ArtIdValue,
    string ArtIdText,
    string DisplayName,
    string ArtTypeText,
    int ArtNumber,
    int FrameIndex,
    int PaletteIndex,
    string AssetPath,
    string SummaryText
);
