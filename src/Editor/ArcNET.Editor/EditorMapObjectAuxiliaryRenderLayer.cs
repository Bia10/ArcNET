namespace ArcNET.Editor;

/// <summary>
/// CE auxiliary render layers around a committed object sprite.
/// </summary>
public enum EditorMapObjectAuxiliaryRenderLayer
{
    /// <summary>
    /// Underlay rendered before the primary object sprite.
    /// </summary>
    Underlay,

    /// <summary>
    /// Shadow pass rendered below non-flat primary object sprites.
    /// </summary>
    Shadow,

    /// <summary>
    /// Back overlay carried in the overlay pass.
    /// </summary>
    OverlayBack,

    /// <summary>
    /// Front overlay carried in the overlay pass.
    /// </summary>
    OverlayFore,
}
