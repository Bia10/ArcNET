namespace ArcNET.Editor;

/// <summary>
/// Persisted terrain-paint tool state for one project map view.
/// </summary>
public sealed class EditorProjectMapTerrainToolState
{
    /// <summary>
    /// Map-properties asset backing the terrain palette selection.
    /// </summary>
    public string? MapPropertiesAssetPath { get; init; }

    /// <summary>
    /// Selected X coordinate inside the terrain palette.
    /// </summary>
    public ulong PaletteX { get; init; }

    /// <summary>
    /// Selected Y coordinate inside the terrain palette.
    /// </summary>
    public ulong PaletteY { get; init; }
}
