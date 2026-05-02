namespace ArcNET.Editor;

/// <summary>
/// Host-neutral palette placement request that instantiates one proto and applies optional initial transforms.
/// </summary>
public sealed class EditorObjectPalettePlacementRequest
{
    /// <summary>
    /// Proto number to instantiate.
    /// </summary>
    public required int ProtoNumber { get; init; }

    /// <summary>
    /// Sector-local tile X offset to apply to the created object location.
    /// </summary>
    public int DeltaTileX { get; init; }

    /// <summary>
    /// Sector-local tile Y offset to apply to the created object location.
    /// </summary>
    public int DeltaTileY { get; init; }

    /// <summary>
    /// Primary rotation to apply to created objects when specified; <see langword="null"/> preserves the proto value.
    /// </summary>
    public float? Rotation { get; init; }

    /// <summary>
    /// Pitch rotation to apply to created objects when specified; <see langword="null"/> preserves the proto value.
    /// </summary>
    public float? RotationPitch { get; init; }

    /// <summary>
    /// Returns created objects to their tile anchor by zeroing screen-space tile offsets after proto instantiation.
    /// </summary>
    public bool AlignToTileGrid { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when the request applies one or more initial transforms during placement.
    /// </summary>
    public bool HasInitialTransform =>
        DeltaTileX != 0 || DeltaTileY != 0 || Rotation.HasValue || RotationPitch.HasValue || AlignToTileGrid;

    /// <summary>
    /// Creates one placement request for the supplied proto and optional initial transforms.
    /// </summary>
    public static EditorObjectPalettePlacementRequest Place(
        int protoNumber,
        int deltaTileX = 0,
        int deltaTileY = 0,
        float? rotation = null,
        float? rotationPitch = null,
        bool alignToTileGrid = false
    ) =>
        new()
        {
            ProtoNumber = protoNumber,
            DeltaTileX = deltaTileX,
            DeltaTileY = deltaTileY,
            Rotation = rotation,
            RotationPitch = rotationPitch,
            AlignToTileGrid = alignToTileGrid,
        };
}
