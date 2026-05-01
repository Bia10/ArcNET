using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// One loaded jump asset plus browser-friendly destination metadata.
/// </summary>
public sealed class EditorJumpDefinition
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
    /// Total jump-entry count in the file.
    /// </summary>
    public required int JumpCount { get; init; }

    /// <summary>
    /// Distinct destination map identifiers referenced by this jump file.
    /// </summary>
    public required IReadOnlyList<int> DestinationMapIds { get; init; }
}
