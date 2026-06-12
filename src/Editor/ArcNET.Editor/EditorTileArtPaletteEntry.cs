using ArcNET.Core.Primitives;
using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// One tile-art asset that can be painted directly onto sector floor tiles.
/// </summary>
public sealed class EditorTileArtPaletteEntry
{
    /// <summary>
    /// Defining ART asset.
    /// </summary>
    public required EditorAssetEntry Asset { get; init; }

    /// <summary>
    /// Parsed format of the defining asset.
    /// </summary>
    public FileFormat Format => Asset.Format;

    /// <summary>
    /// Browser-friendly label for the tile art entry.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Canonical tile art identifier that resolves to <see cref="Asset"/>.
    /// </summary>
    public required ArtId ArtId { get; init; }

    /// <summary>
    /// ART asset path used by preview controls.
    /// </summary>
    public string ArtAssetPath => Asset.AssetPath;

    /// <summary>
    /// Browser-friendly ART detail resolved from the workspace index.
    /// </summary>
    public EditorArtDefinition? ArtDetail { get; init; }

    /// <summary>
    /// Preview-ready ART projection for the tile art entry.
    /// </summary>
    public EditorArtPreview? ArtPreview { get; init; }

    /// <summary>
    /// Creates one tile-paint brush request for this tile art.
    /// </summary>
    public EditorMapLayerBrushRequest CreateTileArtBrushRequest() => EditorMapLayerBrushRequest.SetTileArt(ArtId.Value);
}
