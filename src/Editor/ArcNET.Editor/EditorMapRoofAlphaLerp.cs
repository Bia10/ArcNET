namespace ArcNET.Editor;

/// <summary>
/// Captures the two pairs of alpha values that CE roof rendering
/// interpolates between when drawing the top and bottom halves
/// of a roof column.
/// </summary>
/// <param name="TopOuter">Alpha at the outer edge of the top roof half.</param>
/// <param name="TopInner">Alpha at the inner edge of the top roof half.</param>
/// <param name="BottomInner">Alpha at the inner edge of the bottom roof half.</param>
/// <param name="BottomOuter">Alpha at the outer edge of the bottom roof half.</param>
public readonly record struct EditorMapRoofAlphaLerp(byte TopOuter, byte TopInner, byte BottomInner, byte BottomOuter);
