using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// One loaded map-properties asset plus browser-friendly terrain-grid metadata.
/// </summary>
public sealed class EditorMapPropertiesDefinition
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
    /// Base terrain art identifier.
    /// </summary>
    public required int ArtId { get; init; }

    /// <summary>
    /// Tile count along the X axis.
    /// </summary>
    public required ulong LimitX { get; init; }

    /// <summary>
    /// Tile count along the Y axis.
    /// </summary>
    public required ulong LimitY { get; init; }
}
