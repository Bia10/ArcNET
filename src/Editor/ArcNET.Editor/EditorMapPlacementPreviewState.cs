namespace ArcNET.Editor;

/// <summary>
/// Host-facing placement validity state for one live preview ghost.
/// </summary>
public enum EditorMapPlacementPreviewState
{
    /// <summary>
    /// Placement is currently valid according to the conservative editor heuristics.
    /// </summary>
    Valid = 0,

    /// <summary>
    /// Placement targets one blocked tile.
    /// </summary>
    BlockedTile = 1,

    /// <summary>
    /// Placement targets one tile that is already occupied by another object or preview ghost.
    /// </summary>
    OccupiedTile = 2,
}
