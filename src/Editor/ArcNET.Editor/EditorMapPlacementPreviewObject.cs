using ArcNET.Core.Primitives;
using ArcNET.GameObjects;

namespace ArcNET.Editor;

/// <summary>
/// One live object-placement ghost projected into normalized scene render space.
/// </summary>
public sealed class EditorMapPlacementPreviewObject
{
    /// <summary>
    /// Normalized sector asset path that would own the placed object.
    /// </summary>
    public required string SectorAssetPath { get; init; }

    /// <summary>
    /// Referenced prototype identifier for the ghost object.
    /// </summary>
    public required GameObjectGuid ProtoId { get; init; }

    /// <summary>
    /// Parsed object type for the ghost object.
    /// </summary>
    public required ObjectType ObjectType { get; init; }

    /// <summary>
    /// Current art identifier for the ghost object.
    /// </summary>
    public required ArtId CurrentArtId { get; init; }

    /// <summary>
    /// Map-local tile X coordinate of the anchor tile.
    /// </summary>
    public required int MapTileX { get; init; }

    /// <summary>
    /// Map-local tile Y coordinate of the anchor tile.
    /// </summary>
    public required int MapTileY { get; init; }

    /// <summary>
    /// Sector-local anchor tile coordinate.
    /// </summary>
    public required Location Tile { get; init; }

    /// <summary>
    /// Stable draw order among live placement preview objects.
    /// </summary>
    public required int DrawOrder { get; init; }

    /// <summary>
    /// Normalized screen-space anchor X coordinate.
    /// </summary>
    public required double AnchorX { get; init; }

    /// <summary>
    /// Normalized screen-space anchor Y coordinate.
    /// </summary>
    public required double AnchorY { get; init; }

    /// <summary>
    /// Conservative projected sprite bounds derived from resolved ART metadata when available.
    /// </summary>
    public EditorMapObjectSpriteBounds? SpriteBounds { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when the ghost object is anchored directly to the tile grid.
    /// </summary>
    public required bool IsTileGridSnapped { get; init; }

    /// <summary>
    /// Conservative placement validity state for the ghost object.
    /// </summary>
    public required EditorMapPlacementPreviewState State { get; init; }

    /// <summary>
    /// Optional host hint describing the validity state.
    /// </summary>
    public string? ValidationMessage { get; init; }

    /// <summary>
    /// Suggested preview opacity for host rendering.
    /// </summary>
    public required double SuggestedOpacity { get; init; }

    /// <summary>
    /// Suggested ARGB tint color for host rendering, or <see langword="null"/> when no tint is suggested.
    /// </summary>
    public uint? SuggestedTintColor { get; init; }

    /// <summary>
    /// Preview rotation copied from the placement request or proto defaults.
    /// </summary>
    public required float Rotation { get; init; }

    /// <summary>
    /// Preview pitch rotation copied from the placement request or proto defaults.
    /// </summary>
    public required float RotationPitch { get; init; }
}
