namespace ArcNET.Editor;

/// <summary>
/// Normalized preview traits derived from one projected sector.
/// </summary>
[System.Flags]
public enum EditorMapSectorPreviewFlags
{
    /// <summary>
    /// No preview traits are present.
    /// </summary>
    None = 0,

    /// <summary>
    /// The projection cell contains a sector.
    /// </summary>
    Occupied = 1 << 0,

    /// <summary>
    /// The sector includes roof data.
    /// </summary>
    HasRoofs = 1 << 1,

    /// <summary>
    /// The sector includes one or more lights.
    /// </summary>
    HasLights = 1 << 2,

    /// <summary>
    /// The sector includes one or more blocked tiles.
    /// </summary>
    HasBlockedTiles = 1 << 3,

    /// <summary>
    /// The sector includes a sector script or one or more tile scripts.
    /// </summary>
    HasScripts = 1 << 4,
}
