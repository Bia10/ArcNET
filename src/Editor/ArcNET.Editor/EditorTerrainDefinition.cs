using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// One loaded terrain-definition asset plus browser-friendly sheet metadata.
/// </summary>
public sealed class EditorTerrainDefinition
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
    /// File format version.
    /// </summary>
    public required float Version { get; init; }

    /// <summary>
    /// Base terrain type for the sheet.
    /// </summary>
    public required TerrainType BaseTerrainType { get; init; }

    /// <summary>
    /// Width in tiles.
    /// </summary>
    public required long Width { get; init; }

    /// <summary>
    /// Height in tiles.
    /// </summary>
    public required long Height { get; init; }

    /// <summary>
    /// Whether the stored tile rows are compressed.
    /// </summary>
    public required bool Compressed { get; init; }

    /// <summary>
    /// Number of distinct tile values present in the terrain grid.
    /// </summary>
    public required int DistinctTileCount { get; init; }
}
