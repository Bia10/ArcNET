using ArcNET.Core.Primitives;
using ArcNET.GameObjects;

namespace ArcNET.Editor;

/// <summary>
/// Auxiliary persisted art layer projected around one placed object.
/// </summary>
public sealed class EditorMapObjectAuxiliaryRenderItem
{
    /// <summary>
    /// Normalized sector asset path that owns the parent object.
    /// </summary>
    public required string SectorAssetPath { get; init; }

    /// <summary>
    /// Parent object identifier.
    /// </summary>
    public required GameObjectGuid ParentObjectId { get; init; }

    /// <summary>
    /// Parent object type.
    /// </summary>
    public required ObjectType ParentObjectType { get; init; }

    /// <summary>
    /// CE-style committed scene layer classification inherited from the parent object.
    /// </summary>
    public required EditorMapCommittedRenderLayer CommittedRenderLayer { get; init; }

    /// <summary>
    /// Auxiliary art identifier.
    /// </summary>
    public required ArtId ArtId { get; init; }

    /// <summary>
    /// CE auxiliary layer classification.
    /// </summary>
    public required EditorMapObjectAuxiliaryRenderLayer Layer { get; init; }

    /// <summary>
    /// Map-local tile X coordinate of the parent anchor tile.
    /// </summary>
    public required int MapTileX { get; init; }

    /// <summary>
    /// Map-local tile Y coordinate of the parent anchor tile.
    /// </summary>
    public required int MapTileY { get; init; }

    /// <summary>
    /// Sector-local anchor tile coordinate.
    /// </summary>
    public required Location Tile { get; init; }

    /// <summary>
    /// Stable draw order among auxiliary layers.
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
    /// Returns <see langword="true"/> when this auxiliary should use CE-style light-mask tinting.
    /// </summary>
    public bool UseLightMaskTint { get; init; }

    /// <summary>
    /// Conservative CE-style light tint sampled at the parent object's render anchor.
    /// </summary>
    public uint? SuggestedTintColor { get; init; }

    /// <summary>
    /// Effective CE rotation index used to resolve the auxiliary sprite.
    /// </summary>
    public required int RotationIndex { get; init; }

    /// <summary>
    /// Effective CE scale percent used to resolve the auxiliary sprite.
    /// </summary>
    public required int ScalePercent { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when CE shrunk rendering applies to the auxiliary sprite.
    /// </summary>
    public required bool IsShrunk { get; init; }

    /// <summary>
    /// Host-facing blend mode derived from CE/TIG auxiliary art behavior.
    /// </summary>
    public EditorMapSpriteBlendMode BlendMode { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when CE's roof coverage matrix hides this committed auxiliary layer.
    /// </summary>
    public bool IsRoofCovered { get; init; }
}
