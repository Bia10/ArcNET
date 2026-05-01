using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// One loaded facade-walk asset plus browser-friendly facade metadata.
/// </summary>
public sealed class EditorFacadeWalkDefinition
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
    /// Base terrain message index referenced by the facade.
    /// </summary>
    public required uint Terrain { get; init; }

    /// <summary>
    /// Whether the facade is marked as outdoor.
    /// </summary>
    public required bool Outdoor { get; init; }

    /// <summary>
    /// Whether the facade is marked as horizontally flippable.
    /// </summary>
    public required bool Flippable { get; init; }

    /// <summary>
    /// Facade width.
    /// </summary>
    public required uint Width { get; init; }

    /// <summary>
    /// Facade height.
    /// </summary>
    public required uint Height { get; init; }

    /// <summary>
    /// Total walkability-entry count in the file.
    /// </summary>
    public required int EntryCount { get; init; }

    /// <summary>
    /// Number of walkable entries in the file.
    /// </summary>
    public required int WalkableEntryCount { get; init; }
}
