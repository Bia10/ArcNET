namespace ArcNET.Editor;

/// <summary>
/// Layer-brush operation applied to grouped scene-sector hits.
/// </summary>
public enum EditorMapLayerBrushMode
{
    /// <summary>
    /// Set the ground-tile art ID on each unique hit tile.
    /// </summary>
    SetTileArt,

    /// <summary>
    /// Set the roof art ID on each unique hit roof cell.
    /// </summary>
    SetRoofArt,

    /// <summary>
    /// Set the blocked state on each unique hit tile.
    /// </summary>
    SetBlocked,
}
