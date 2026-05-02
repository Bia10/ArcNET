namespace ArcNET.Editor;

/// <summary>
/// Higher-level host-neutral request that applies one or more object transforms to grouped scene-sector hits.
/// </summary>
public sealed class EditorMapObjectTransformRequest
{
    /// <summary>
    /// Sector-local tile X offset to apply to selected objects.
    /// </summary>
    public int DeltaTileX { get; init; }

    /// <summary>
    /// Sector-local tile Y offset to apply to selected objects.
    /// </summary>
    public int DeltaTileY { get; init; }

    /// <summary>
    /// Primary rotation to apply when specified; <see langword="null"/> preserves the current rotation.
    /// </summary>
    public float? Rotation { get; init; }

    /// <summary>
    /// Pitch rotation to apply when specified; <see langword="null"/> preserves the current pitch rotation.
    /// </summary>
    public float? RotationPitch { get; init; }

    /// <summary>
    /// Returns objects to their stored tile anchor by clearing screen-space tile offsets
    /// such as <see cref="ArcNET.GameObjects.ObjectField.ObjFOffsetX"/> and
    /// <see cref="ArcNET.GameObjects.ObjectField.ObjFOffsetY"/>.
    /// </summary>
    public bool AlignToTileGrid { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when the request changes object position.
    /// </summary>
    public bool HasMoveOffset => DeltaTileX != 0 || DeltaTileY != 0;

    /// <summary>
    /// Returns <see langword="true"/> when the request changes one or more object fields.
    /// </summary>
    public bool HasChanges => HasMoveOffset || Rotation.HasValue || RotationPitch.HasValue || AlignToTileGrid;

    /// <summary>
    /// Creates one transform request from the supplied optional move and rotation values.
    /// </summary>
    public static EditorMapObjectTransformRequest Transform(
        int deltaTileX = 0,
        int deltaTileY = 0,
        float? rotation = null,
        float? rotationPitch = null,
        bool alignToTileGrid = false
    ) =>
        new()
        {
            DeltaTileX = deltaTileX,
            DeltaTileY = deltaTileY,
            Rotation = rotation,
            RotationPitch = rotationPitch,
            AlignToTileGrid = alignToTileGrid,
        };

    /// <summary>
    /// Creates one move-only transform request.
    /// </summary>
    public static EditorMapObjectTransformRequest MoveByOffset(int deltaTileX, int deltaTileY) =>
        new() { DeltaTileX = deltaTileX, DeltaTileY = deltaTileY };

    /// <summary>
    /// Creates one primary-rotation-only transform request.
    /// </summary>
    public static EditorMapObjectTransformRequest Rotate(float rotation) => new() { Rotation = rotation };

    /// <summary>
    /// Creates one pitch-rotation-only transform request.
    /// </summary>
    public static EditorMapObjectTransformRequest RotatePitch(float rotationPitch) =>
        new() { RotationPitch = rotationPitch };

    /// <summary>
    /// Creates one snap-to-tile-grid transform request.
    /// </summary>
    public static EditorMapObjectTransformRequest SnapToTileGrid() => new() { AlignToTileGrid = true };
}
