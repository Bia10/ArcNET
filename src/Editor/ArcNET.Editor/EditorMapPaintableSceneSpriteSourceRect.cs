namespace ArcNET.Editor;

/// <summary>
/// Normalized source rectangle within a sprite atlas.
/// </summary>
/// <param name="Left">Left offset in sprite atlas space.</param>
/// <param name="Top">Top offset in sprite atlas space.</param>
/// <param name="Width">Rectangle width.</param>
/// <param name="Height">Rectangle height.</param>
public readonly record struct EditorMapPaintableSceneSpriteSourceRect(int Left, int Top, int Width, int Height);
