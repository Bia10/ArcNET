namespace ArcNET.Editor;

/// <summary>
/// Options that control how an <see cref="ArcNET.Formats.ArtFile"/> is projected into
/// preview-ready pixel data.
/// </summary>
public sealed class EditorArtPreviewOptions
{
    /// <summary>
    /// Palette slot to apply when converting palette-indexed ART pixels into packed colors.
    /// </summary>
    public int PaletteSlot { get; init; }

    /// <summary>
    /// Packed output pixel format.
    /// </summary>
    public EditorArtPreviewPixelFormat PixelFormat { get; init; } = EditorArtPreviewPixelFormat.Rgba32;

    /// <summary>
    /// When <see langword="true"/>, output rows are flipped relative to the native ART row order.
    /// When <see langword="false"/>, output preserves the native CE-compatible top-to-bottom row order.
    /// </summary>
    public bool FlipVertically { get; init; }

    /// <summary>
    /// When <see langword="true"/>, the asset is treated as a light mask/bloom where the pixel's palette index
    /// represents the light intensity (decoded as the alpha channel if the palette has no explicit alpha).
    /// </summary>
    public bool IsLightMask { get; init; }
}
