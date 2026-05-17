namespace ArcNET.Editor;

/// <summary>
/// CE horizontal object alpha interpolation values for wall transparency.
/// Values use direct source-alpha scaling semantics: 0 makes the source fully transparent and 255 keeps it fully opaque.
/// </summary>
public readonly record struct EditorMapObjectAlphaLerp(byte Left, byte Right);
