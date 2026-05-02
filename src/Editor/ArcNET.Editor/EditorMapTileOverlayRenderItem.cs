using ArcNET.Core.Primitives;

namespace ArcNET.Editor;

/// <summary>
/// Render-ready tile-semantic overlay projected into the normalized scene render space.
/// </summary>
public sealed class EditorMapTileOverlayRenderItem
{
    /// <summary>
    /// Normalized sector asset path that owns the tile.
    /// </summary>
    public required string SectorAssetPath { get; init; }

    /// <summary>
    /// Map-local tile X coordinate.
    /// </summary>
    public required int MapTileX { get; init; }

    /// <summary>
    /// Map-local tile Y coordinate.
    /// </summary>
    public required int MapTileY { get; init; }

    /// <summary>
    /// Sector-local tile coordinate.
    /// </summary>
    public required Location Tile { get; init; }

    /// <summary>
    /// Overlay semantic kind.
    /// </summary>
    public required EditorMapTileOverlayKind Kind { get; init; }

    /// <summary>
    /// Stable draw order within the overlay layer.
    /// </summary>
    public required int DrawOrder { get; init; }

    /// <summary>
    /// Projected screen-space tile center X coordinate in the normalized preview bounds.
    /// </summary>
    public required double CenterX { get; init; }

    /// <summary>
    /// Projected screen-space tile center Y coordinate in the normalized preview bounds.
    /// </summary>
    public required double CenterY { get; init; }

    /// <summary>
    /// Suggested overlay opacity for hosts that do not supply their own styling.
    /// </summary>
    public required double SuggestedOpacity { get; init; }

    /// <summary>
    /// Suggested overlay tint color for hosts that do not supply their own styling.
    /// </summary>
    public required uint SuggestedTintColor { get; init; }
}
