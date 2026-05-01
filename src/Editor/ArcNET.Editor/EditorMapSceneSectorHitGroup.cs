namespace ArcNET.Editor;

/// <summary>
/// Ordered scene hits grouped under one positioned sector.
/// </summary>
public sealed class EditorMapSceneSectorHitGroup
{
    /// <summary>
    /// Normalized sector asset path shared by every hit in <see cref="Hits"/>.
    /// </summary>
    public required string SectorAssetPath { get; init; }

    /// <summary>
    /// Map-local sector X coordinate.
    /// </summary>
    public required int LocalX { get; init; }

    /// <summary>
    /// Map-local sector Y coordinate.
    /// </summary>
    public required int LocalY { get; init; }

    /// <summary>
    /// Stable screen-order hits that landed inside this sector.
    /// </summary>
    public required IReadOnlyList<EditorMapSceneHit> Hits { get; init; }

    /// <summary>
    /// Number of map tiles from the resolved area that landed inside this sector.
    /// </summary>
    public int TileCount => Hits.Count;
}
