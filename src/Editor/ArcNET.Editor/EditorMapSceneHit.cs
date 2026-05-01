using ArcNET.Core.Primitives;

namespace ArcNET.Editor;

/// <summary>
/// One scene-preview hit resolved from a viewport point.
/// </summary>
public sealed class EditorMapSceneHit
{
    /// <summary>
    /// Map-local tile X coordinate hit by the pointer.
    /// </summary>
    public required int MapTileX { get; init; }

    /// <summary>
    /// Map-local tile Y coordinate hit by the pointer.
    /// </summary>
    public required int MapTileY { get; init; }

    /// <summary>
    /// Normalized sector asset path hit by the pointer.
    /// </summary>
    public required string SectorAssetPath { get; init; }

    /// <summary>
    /// Sector-local tile coordinate hit by the pointer.
    /// </summary>
    public required Location Tile { get; init; }

    /// <summary>
    /// All preview objects positioned on the hit tile, ordered by the scene-hit depth heuristic and then preview order.
    /// </summary>
    public required IReadOnlyList<EditorMapObjectPreview> ObjectHits { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when one or more preview objects occupy the hit tile.
    /// </summary>
    public bool HasObjectHits => ObjectHits.Count > 0;
}
