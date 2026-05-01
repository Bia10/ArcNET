using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// One preview-ready ART frame with packed 32-bit color pixels.
/// </summary>
public sealed class EditorArtPreviewFrame
{
    /// <summary>
    /// Zero-based rotation index that owns this frame.
    /// </summary>
    public required int RotationIndex { get; init; }

    /// <summary>
    /// Zero-based frame index within <see cref="RotationIndex"/>.
    /// </summary>
    public required int FrameIndex { get; init; }

    /// <summary>
    /// Original ART frame metadata.
    /// </summary>
    public required ArtFrameHeader Header { get; init; }

    /// <summary>
    /// Packed 32-bit pixel data using the parent preview's <see cref="EditorArtPreview.PixelFormat"/>.
    /// </summary>
    public required byte[] PixelData { get; init; }

    /// <summary>
    /// Frame width in pixels.
    /// </summary>
    public int Width => checked((int)Header.Width);

    /// <summary>
    /// Frame height in pixels.
    /// </summary>
    public int Height => checked((int)Header.Height);

    /// <summary>
    /// Number of bytes per output row.
    /// </summary>
    public int Stride => checked(Width * 4);
}
