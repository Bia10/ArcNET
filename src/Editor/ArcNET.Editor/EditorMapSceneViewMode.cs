namespace ArcNET.Editor;

/// <summary>
/// Host-facing scene view mode used when projecting render-ready map tiles.
/// </summary>
public enum EditorMapSceneViewMode
{
    /// <summary>
    /// Orthographic tile grid with one axis-aligned tile rectangle per map tile.
    /// </summary>
    TopDown = 0,

    /// <summary>
    /// Isometric diamond projection suitable for classic world-editor floor rendering.
    /// </summary>
    Isometric = 1,
}
