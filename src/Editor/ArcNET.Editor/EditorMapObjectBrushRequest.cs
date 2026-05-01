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
    /// or <see cref="EditorMapObjectBrushMode.ReplaceWithProto"/>. Ignored for erase requests.
    /// </summary>
    public int ProtoNumber { get; init; }

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
}
