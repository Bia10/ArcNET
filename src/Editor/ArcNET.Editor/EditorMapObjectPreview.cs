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
    /// Map-tile location when present on the loaded object.
    /// </summary>
    public Location? Location { get; init; }

    /// <summary>
    /// Screen-space X offset taken from <see cref="ObjectField.ObjFOffsetX"/>.
    /// </summary>
    public int OffsetX { get; init; }

    /// <summary>
    /// Screen-space Y offset taken from <see cref="ObjectField.ObjFOffsetY"/>.
    /// </summary>
    public int OffsetY { get; init; }

    /// <summary>
    /// Z-axis offset taken from <see cref="ObjectField.ObjFOffsetZ"/>.
    /// </summary>
    public float OffsetZ { get; init; }

    /// <summary>
    /// Collision height taken from <see cref="ObjectField.ObjFHeight"/> when present.
    /// </summary>
    public float CollisionHeight { get; init; }

    /// <summary>
    /// Conservative resolved sprite bounds when scene preview construction had access to the object's ART data.
    /// </summary>
    public EditorMapObjectSpriteBounds? SpriteBounds { get; init; }

    /// <summary>
    /// Pitch rotation taken from <see cref="ObjectField.ObjFRotationPitch"/>.
    /// </summary>
    public required float RotationPitch { get; init; }
}
