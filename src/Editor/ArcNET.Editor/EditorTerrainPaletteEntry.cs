using ArcNET.Core.Primitives;
using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// One map-properties-backed terrain palette entry that hosts can browse and turn into tile-paint requests.
/// </summary>
public sealed class EditorTerrainPaletteEntry
{
    /// <summary>
    /// Defining map-properties asset.
    /// </summary>
    public required EditorAssetEntry Asset { get; init; }

    /// <summary>
    /// Parsed format of the defining asset.
    /// </summary>
    public FileFormat Format => Asset.Format;

    /// <summary>
    /// Base terrain art identifier stored by the source map-properties asset.
    /// </summary>
    public required int BaseArtId { get; init; }

    /// <summary>
    /// Tile count along the X axis of the source terrain grid.
    /// </summary>
    public required ulong LimitX { get; init; }

    /// <summary>
    /// Tile count along the Y axis of the source terrain grid.
    /// </summary>
    public required ulong LimitY { get; init; }

    /// <summary>
    /// Zero-based X coordinate inside the source terrain grid.
    /// </summary>
    public required ulong PaletteX { get; init; }

    /// <summary>
    /// Zero-based Y coordinate inside the source terrain grid.
    /// </summary>
    public required ulong PaletteY { get; init; }

    /// <summary>
    /// Linear zero-based tile index inside the source terrain grid.
    /// </summary>
    public required ulong PaletteIndex { get; init; }

    /// <summary>
    /// Derived terrain art identifier that can be painted onto one map tile.
    /// </summary>
    public required ArtId ArtId { get; init; }

    /// <summary>
    /// Bound ART asset path resolved through an optional resolver, or <see langword="null"/> when no binding exists.
    /// </summary>
    public string? ArtAssetPath { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when <see cref="ArtAssetPath"/> is populated.
    /// </summary>
    public bool HasArtBinding => !string.IsNullOrWhiteSpace(ArtAssetPath);

    /// <summary>
    /// Browser-friendly ART detail resolved through an optional resolver, or <see langword="null"/> when unavailable.
    /// </summary>
    public EditorArtDefinition? ArtDetail { get; init; }

    /// <summary>
    /// Preview-ready ART projection resolved through an optional resolver, or <see langword="null"/> when unavailable.
    /// </summary>
    public EditorArtPreview? ArtPreview { get; init; }

    /// <summary>
    /// Creates one tile-paint brush request for this terrain palette entry.
    /// </summary>
    public EditorMapLayerBrushRequest CreateTileArtBrushRequest() => EditorMapLayerBrushRequest.SetTileArt(ArtId.Value);
}
