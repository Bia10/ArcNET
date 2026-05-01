namespace ArcNET.Editor;

/// <summary>
/// Selects the normalized preview overlay to build for a projected map.
/// </summary>
public enum EditorMapPreviewMode
{
    /// <summary>
    /// Marks every occupied sector cell.
    /// </summary>
    Occupancy,

    /// <summary>
    /// Visualizes object-density bands.
    /// </summary>
    Objects,

    /// <summary>
    /// Visualizes the highest-priority semantic trait per occupied sector.
    /// </summary>
    Combined,

    /// <summary>
    /// Highlights sectors that include roofs.
    /// </summary>
    Roofs,

    /// <summary>
    /// Highlights sectors that include lights.
    /// </summary>
    Lights,

    /// <summary>
    /// Highlights sectors that include blocked tiles.
    /// </summary>
    Blocked,

    /// <summary>
    /// Highlights sectors that include sector or tile scripts.
    /// </summary>
    Scripts,
}
