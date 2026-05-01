using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// Preview-ready projection of one <see cref="ArtFile"/>.
/// </summary>
public sealed class EditorArtPreview
{
    /// <summary>
    /// ART flags from the source file.
    /// </summary>
    public required ArtFlags Flags { get; init; }

    /// <summary>
    /// Animation frame rate copied from the source file.
    /// </summary>
    public required uint FrameRate { get; init; }

    /// <summary>
    /// Action-frame index copied from the source file.
    /// </summary>
    public required uint ActionFrame { get; init; }

    /// <summary>
    /// Number of rotation directions exposed by this preview.
    /// </summary>
    public required int RotationCount { get; init; }

    /// <summary>
    /// Number of frames per rotation direction.
    /// </summary>
    public required int FramesPerRotation { get; init; }

    /// <summary>
    /// Palette slot used to resolve palette-indexed ART pixels.
    /// </summary>
    public required int PaletteSlot { get; init; }

    /// <summary>
    /// Packed output pixel format used by every frame in <see cref="Frames"/>.
    /// </summary>
    public required EditorArtPreviewPixelFormat PixelFormat { get; init; }

    /// <summary>
    /// Flattened frame list ordered by rotation first, then frame index.
    /// </summary>
    public required IReadOnlyList<EditorArtPreviewFrame> Frames { get; init; }

    /// <summary>
    /// Duration of one frame at <see cref="FrameRate"/>.
    /// </summary>
    public TimeSpan FrameDuration => FrameRate == 0 ? TimeSpan.Zero : TimeSpan.FromSeconds(1d / FrameRate);
}
