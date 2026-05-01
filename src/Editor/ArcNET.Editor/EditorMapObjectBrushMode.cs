namespace ArcNET.Editor;

/// <summary>
/// Object-brush operation applied to grouped scene-sector hits.
/// </summary>
public enum EditorMapObjectBrushMode
{
    /// <summary>
    /// Instantiate one object from a proto definition on each unique hit tile.
    /// </summary>
    StampFromProto,

    /// <summary>
    /// Replace hit objects with one new proto instance on each unique hit tile.
    /// </summary>
    ReplaceWithProto,

    /// <summary>
    /// Remove hit objects identified by grouped scene hits.
    /// </summary>
    Erase,
}
