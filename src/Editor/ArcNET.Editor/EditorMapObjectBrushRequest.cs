namespace ArcNET.Editor;

/// <summary>
/// Host-neutral request that applies one object-brush operation to grouped scene-sector hits.
/// </summary>
public sealed class EditorMapObjectBrushRequest
{
    /// <summary>
    /// Requested brush operation.
    /// </summary>
    public required EditorMapObjectBrushMode Mode { get; init; }

    /// <summary>
    /// Proto number to instantiate when <see cref="Mode"/> is <see cref="EditorMapObjectBrushMode.StampFromProto"/>
    /// or <see cref="EditorMapObjectBrushMode.ReplaceWithProto"/>. Ignored for erase/rotate/move requests.
    /// </summary>
    public int ProtoNumber { get; init; }

    /// <summary>
    /// Primary rotation to apply when <see cref="Mode"/> is <see cref="EditorMapObjectBrushMode.Rotate"/>.
    /// Ignored for stamp/replace/erase/pitch-rotate requests.
    /// </summary>
    public float Rotation { get; init; }

    /// <summary>
    /// Pitch rotation to apply when <see cref="Mode"/> is <see cref="EditorMapObjectBrushMode.RotatePitch"/>.
    /// Ignored for stamp/replace/erase/primary-rotate/move requests.
    /// </summary>
    public float RotationPitch { get; init; }

    /// <summary>
    /// Sector-local tile X offset to apply when <see cref="Mode"/> is <see cref="EditorMapObjectBrushMode.MoveByOffset"/>.
    /// Ignored for stamp/replace/erase/rotate requests.
    /// </summary>
    public int DeltaTileX { get; init; }

    /// <summary>
    /// Sector-local tile Y offset to apply when <see cref="Mode"/> is <see cref="EditorMapObjectBrushMode.MoveByOffset"/>.
    /// Ignored for stamp/replace/erase/rotate requests.
    /// </summary>
    public int DeltaTileY { get; init; }

    /// <summary>
    /// Creates one proto-stamp request.
    /// </summary>
    public static EditorMapObjectBrushRequest StampFromProto(int protoNumber) =>
        new() { Mode = EditorMapObjectBrushMode.StampFromProto, ProtoNumber = protoNumber };

    /// <summary>
    /// Creates one replace-with-proto request.
    /// </summary>
    public static EditorMapObjectBrushRequest ReplaceWithProto(int protoNumber) =>
        new() { Mode = EditorMapObjectBrushMode.ReplaceWithProto, ProtoNumber = protoNumber };

    /// <summary>
    /// Creates one erase request.
    /// </summary>
    public static EditorMapObjectBrushRequest Erase() => new() { Mode = EditorMapObjectBrushMode.Erase };

    /// <summary>
    /// Creates one rotation request.
    /// </summary>
    public static EditorMapObjectBrushRequest Rotate(float rotation) =>
        new() { Mode = EditorMapObjectBrushMode.Rotate, Rotation = rotation };

    /// <summary>
    /// Creates one pitch-rotation request.
    /// </summary>
    public static EditorMapObjectBrushRequest RotatePitch(float rotationPitch) =>
        new() { Mode = EditorMapObjectBrushMode.RotatePitch, RotationPitch = rotationPitch };

    /// <summary>
    /// Creates one move-by-offset request.
    /// </summary>
    public static EditorMapObjectBrushRequest MoveByOffset(int deltaTileX, int deltaTileY) =>
        new()
        {
            Mode = EditorMapObjectBrushMode.MoveByOffset,
            DeltaTileX = deltaTileX,
            DeltaTileY = deltaTileY,
        };
}
