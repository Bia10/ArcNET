namespace ArcNET.Editor;

/// <summary>
/// Normalized destination rectangle in scene render space.
/// </summary>
/// <param name="X">Left anchor in scene render space.</param>
/// <param name="Y">Top anchor in scene render space.</param>
/// <param name="Width">Rectangle width.</param>
/// <param name="Height">Rectangle height.</param>
public readonly record struct EditorMapPaintableSceneSpriteDestinationRect(
    double X,
    double Y,
    double Width,
    double Height
);
