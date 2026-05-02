namespace ArcNET.Editor;

/// <summary>
/// Host-neutral request that applies one layer-brush operation to grouped scene-sector hits.
/// </summary>
public sealed class EditorMapLayerBrushRequest
{
    /// <summary>
    /// Requested brush operation.
    /// </summary>
    public required EditorMapLayerBrushMode Mode { get; init; }

    /// <summary>
    /// Art ID to apply when <see cref="Mode"/> is <see cref="EditorMapLayerBrushMode.SetTileArt"/>
    /// or <see cref="EditorMapLayerBrushMode.SetRoofArt"/>. Ignored for blocked-state requests.
    /// </summary>
    public uint ArtId { get; init; }

    /// <summary>
    /// Blocked state to apply when <see cref="Mode"/> is <see cref="EditorMapLayerBrushMode.SetBlocked"/>.
    /// Ignored for art requests.
    /// </summary>
    public bool Blocked { get; init; }

    /// <summary>
    /// Creates one tile-art request.
    /// </summary>
    public static EditorMapLayerBrushRequest SetTileArt(uint artId) =>
        new() { Mode = EditorMapLayerBrushMode.SetTileArt, ArtId = artId };

    /// <summary>
    /// Creates one roof-art request.
    /// </summary>
    public static EditorMapLayerBrushRequest SetRoofArt(uint artId) =>
        new() { Mode = EditorMapLayerBrushMode.SetRoofArt, ArtId = artId };

    /// <summary>
    /// Creates one blocked-state request.
    /// </summary>
    public static EditorMapLayerBrushRequest SetBlocked(bool blocked) =>
        new() { Mode = EditorMapLayerBrushMode.SetBlocked, Blocked = blocked };
}
