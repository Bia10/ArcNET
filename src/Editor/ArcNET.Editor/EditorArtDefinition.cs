using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// One loaded ART asset plus browser-friendly preview metadata derived from its frame headers.
/// </summary>
public sealed class EditorArtDefinition
{
    /// <summary>
    /// Defining asset.
    /// </summary>
    public required EditorAssetEntry Asset { get; init; }

    /// <summary>
    /// Parsed format of the defining asset.
    /// </summary>
    public FileFormat Format => Asset.Format;

    /// <summary>
    /// ART flags copied from the source file.
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
    /// Number of rotation directions exposed by this ART.
    /// </summary>
    public required int RotationCount { get; init; }

    /// <summary>
    /// Number of frames per rotation direction.
    /// </summary>
    public required int FramesPerRotation { get; init; }

    /// <summary>
    /// Number of palette slots defined by this ART.
    /// </summary>
    public required int PaletteCount { get; init; }

    /// <summary>
    /// Maximum frame width observed across all rotations and frames.
    /// </summary>
    public required int MaxFrameWidth { get; init; }

    /// <summary>
    /// Maximum frame height observed across all rotations and frames.
    /// </summary>
    public required int MaxFrameHeight { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when the ART exposes multiple frames or a non-zero frame rate.
    /// </summary>
    public bool IsAnimated => FramesPerRotation > 1 || FrameRate > 0;
}
