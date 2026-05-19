namespace ArcNET.Editor;

/// <summary>
/// Captures the two pairs of alpha values that CE roof rendering
/// interpolates between when drawing the top and bottom halves
/// of a roof column.
/// </summary>
/// <param name="TopLeft">Alpha at the outer edge of the top roof half.</param>
/// <param name="TopRight">Alpha at the inner edge of the top roof half.</param>
/// <param name="BottomLeft">Alpha at the inner edge of the bottom roof half.</param>
/// <param name="BottomRight">Alpha at the outer edge of the bottom roof half.</param>
public readonly record struct EditorMapRoofAlphaLerp(byte TopLeft, byte TopRight, byte BottomLeft, byte BottomRight);
