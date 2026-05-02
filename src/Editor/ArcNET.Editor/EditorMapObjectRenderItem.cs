using ArcNET.Core.Primitives;
using ArcNET.GameObjects;

namespace ArcNET.Editor;

/// <summary>
/// Render-ready anchor projection for one placed object in the normalized map render space.
/// </summary>
public sealed class EditorMapObjectRenderItem
{
    /// <summary>
    /// Normalized sector asset path that owns the object.
    /// </summary>
    public required string SectorAssetPath { get; init; }

    /// <summary>
    /// Unique object identifier.
    /// </summary>
    public required GameObjectGuid ObjectId { get; init; }

    /// <summary>
    /// Referenced prototype identifier.
    /// </summary>
    public required GameObjectGuid ProtoId { get; init; }

    /// <summary>
    /// Parsed object type.
    /// </summary>
    public required ObjectType ObjectType { get; init; }

    /// <summary>
    /// Current art identifier.
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
    /// Stable draw order among projected objects.
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
    /// Returns <see langword="true"/> when the object is anchored directly to the tile grid.
    /// </summary>
    public required bool IsTileGridSnapped { get; init; }
}
