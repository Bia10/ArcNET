namespace ArcNET.Editor;

/// <summary>
/// Packed 32-bit pixel formats exposed by editor preview surfaces.
/// </summary>
public enum EditorArtPreviewPixelFormat
{
    /// <summary>
    /// Pixels are stored as red, green, blue, alpha bytes.
    /// </summary>
    Rgba32 = 0,

    /// <summary>
    /// Pixels are stored as blue, green, red, alpha bytes.
    /// </summary>
    Bgra32 = 1,
}
