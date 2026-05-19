using ArcNET.Core.Primitives;
using ArcNET.GameObjects;

namespace ArcNET.Editor;

/// <summary>
/// Preview-ready placement metadata for one placed object in a sector.
/// </summary>
public sealed class EditorMapObjectPreview
{
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
    /// Object flags when the preview source resolved them.
    /// </summary>
    public ObjectFlags Flags { get; init; }

    /// <summary>
    /// Backing asset path for this object when the scene preview can resolve it.
    /// Sector-embedded objects use their parent sector asset path; loose map mobs use their mob asset path.
    /// </summary>
    public string? SourceAssetPath { get; init; }

    /// <summary>
    /// Map-tile location when present on the loaded object.
    /// </summary>
    public Location? Location { get; init; }

    /// <summary>
    /// Screen-space X offset taken from <see cref="ObjectField.OffsetX"/>.
    /// </summary>
    public int OffsetX { get; init; }

    /// <summary>
    /// Screen-space Y offset taken from <see cref="ObjectField.OffsetY"/>.
    /// </summary>
    public int OffsetY { get; init; }

    /// <summary>
    /// Z-axis offset taken from <see cref="ObjectField.OffsetZ"/>.
    /// </summary>
    public float OffsetZ { get; init; }

    /// <summary>
    /// Collision height taken from <see cref="ObjectField.Height"/> when present.
    /// </summary>
    public float CollisionHeight { get; init; }

    /// <summary>
    /// Conservative resolved sprite bounds when scene preview construction had access to the object's ART data.
    /// </summary>
    public EditorMapObjectSpriteBounds? SpriteBounds { get; init; }

    /// <summary>
    /// Primary rotation taken from the legacy <see cref="ObjectField.PadIas1"/> field that stores
    /// <c>Rotation</c>.
    /// </summary>
    public float Rotation { get; init; }

    /// <summary>
    /// Effective rotation index derived from <see cref="Rotation"/> or the source art identifier.
    /// </summary>
    public int RotationIndex { get; init; }

    /// <summary>
    /// Effective CE blit scale percentage.
    /// </summary>
    public int BlitScale { get; init; } = 100;

    /// <summary>
    /// Pitch rotation taken from <see cref="ObjectField.RotationPitch"/>.
    /// </summary>
    public required float RotationPitch { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when the object is anchored directly on its tile without screen-space offsets.
    /// </summary>
    public bool IsTileGridSnapped => OffsetX == 0 && OffsetY == 0;

    /// <summary>
    /// Returns <see langword="true"/> when CE shrunk rendering applies to this object.
    /// </summary>
    public bool IsShrunk => Flags.HasFlag(ObjectFlags.Shrunk);
}
