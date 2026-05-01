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
    /// When <see langword="true"/>, output rows are flipped so the packed pixel data is top-to-bottom.
    /// When <see langword="false"/>, output preserves the native bottom-to-top ART row order.
    /// </summary>
    public bool FlipVertically { get; init; } = true;
}
