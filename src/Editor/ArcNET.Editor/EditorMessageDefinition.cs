using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// One loaded message asset plus browser-friendly entry metadata.
/// </summary>
public sealed class EditorMessageDefinition
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
    /// Total message-entry count in the file.
    /// </summary>
    public required int EntryCount { get; init; }

    /// <summary>
    /// Lowest message index in the file, or <see langword="null"/> when the file is empty.
    /// </summary>
    public int? MinEntryIndex { get; init; }

    /// <summary>
    /// Highest message index in the file, or <see langword="null"/> when the file is empty.
    /// </summary>
    public int? MaxEntryIndex { get; init; }

    /// <summary>
    /// Stable message entries in file order.
    /// </summary>
    public required IReadOnlyList<MessageEntry> Entries { get; init; }
}
