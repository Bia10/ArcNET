namespace ArcNET.Editor;

/// <summary>
/// Conservative sprite-bounds metadata derived from all resolved ART frame headers for one object.
/// These maxima are suitable for coarse preview heuristics, not exact engine render-order emulation.
/// </summary>
public sealed class EditorMapObjectSpriteBounds
{
    /// <summary>
    /// Largest frame width across the resolved ART frames.
    /// </summary>
    public required int MaxFrameWidth { get; init; }

    /// <summary>
    /// Largest frame height across the resolved ART frames.
    /// </summary>
    public required int MaxFrameHeight { get; init; }

    /// <summary>
    /// Largest raw center X value across the resolved ART frames.
    /// </summary>
    public required int MaxFrameCenterX { get; init; }

    /// <summary>
    /// Largest raw center Y value across the resolved ART frames.
    /// </summary>
    public required int MaxFrameCenterY { get; init; }
}
