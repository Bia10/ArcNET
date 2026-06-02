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
    /// Zero-based object index inside the owning sector when the render item originated from a sector-backed object.
    /// </summary>
    public int? SourceObjectIndex { get; init; }

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
    /// Optional CE-style committed scene layer classification for the object.
    /// </summary>
    public EditorMapCommittedRenderLayer? CommittedRenderLayer { get; init; }

    /// <summary>
    /// Current art identifier.
    /// </summary>
    public required ArtId CurrentArtId { get; init; }

    /// <summary>
    /// CE object flags carried forward from the source preview.
    /// </summary>
    public ObjectFlags Flags { get; init; }

    /// <summary>
    /// CE wall flags carried forward from the source preview.
    /// </summary>
    public int WallFlags { get; init; }

    /// <summary>
    /// CE scenery flags carried forward from the source preview.
    /// </summary>
    public SceneryFlags SceneryFlags { get; init; }

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

    /// <summary>
    /// Primary rotation copied from the source object preview.
    /// </summary>
    public required float Rotation { get; init; }

    /// <summary>
    /// Effective CE rotation index used by hosts that need frame-stable selection overlays.
    /// </summary>
    public int RotationIndex { get; init; }

    /// <summary>
    /// Effective CE blit scale percentage.
    /// </summary>
    public int BlitScale { get; init; } = 100;

    /// <summary>
    /// CE per-object blit flags.
    /// </summary>
    public int BlitFlags { get; init; }

    /// <summary>
    /// CE per-object blit color tint.
    /// </summary>
    public uint BlitColor { get; init; }

    /// <summary>
    /// CE per-object blit alpha constant.
    /// </summary>
    public int BlitAlpha { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when CE shrunk rendering applies to this sprite.
    /// </summary>
    public bool IsShrunk { get; init; }

    /// <summary>
    /// Pitch rotation copied from the source object preview.
    /// </summary>
    public required float RotationPitch { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when the object is visually covered by a roof tile.
    /// </summary>
    public bool IsRoofCovered { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when the object's anchor tile uses indoor floor lighting rules.
    /// </summary>
    public bool IsIndoorTile { get; init; }

    /// <summary>
    /// Light flags from <see cref="ObjectField.LightFlags"/>.
    /// </summary>
    public int LightFlags { get; init; }

    /// <summary>
    /// Light art identifier from <see cref="ObjectField.LightAid"/>.
    /// </summary>
    public ArtId LightAid { get; init; }

    /// <summary>
    /// Light color from <see cref="ObjectField.LightColor"/>.
    /// </summary>
    public Color? LightColor { get; init; }
}
