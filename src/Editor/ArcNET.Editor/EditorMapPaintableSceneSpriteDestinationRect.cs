namespace ArcNET.Editor;

/// <summary>
/// Normalized destination rectangle in scene render space.
/// </summary>
/// <param name="Left">Left anchor in scene render space.</param>
/// <param name="Top">Top anchor in scene render space.</param>
/// <param name="Width">Rectangle width.</param>
/// <param name="Height">Rectangle height.</param>
public readonly record struct EditorMapPaintableSceneSpriteDestinationRect(
    double Left,
    double Top,
    double Width,
    double Height
);
