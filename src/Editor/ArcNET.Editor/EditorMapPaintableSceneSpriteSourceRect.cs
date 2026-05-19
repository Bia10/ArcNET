namespace ArcNET.Editor;

/// <summary>
/// Normalized source rectangle within a sprite atlas.
/// </summary>
/// <param name="X">Left offset in sprite atlas space.</param>
/// <param name="Y">Top offset in sprite atlas space.</param>
/// <param name="Width">Rectangle width.</param>
/// <param name="Height">Rectangle height.</param>
public readonly record struct EditorMapPaintableSceneSpriteSourceRect(int X, int Y, int Width, int Height);
