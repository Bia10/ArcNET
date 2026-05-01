using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// One parsed sector asset plus higher-level counts and environment metadata.
/// </summary>
public sealed class EditorSectorSummary
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
    /// Map directory name that owns the sector asset.
    /// </summary>
    public required string MapName { get; init; }

    /// <summary>
    /// Number of static objects placed in the sector.
    /// </summary>
    public required int ObjectCount { get; init; }

    /// <summary>
    /// Number of light sources in the sector.
    /// </summary>
    public required int LightCount { get; init; }

    /// <summary>
    /// Number of tile-script nodes in the sector.
    /// </summary>
    public required int TileScriptCount { get; init; }

    /// <summary>
    /// Sector-level script identifier, or <see langword="null"/> when the sector has no script.
    /// </summary>
    public int? SectorScriptId { get; init; }

    /// <summary>
    /// Whether the sector includes a roof grid.
    /// </summary>
    public required bool HasRoofs { get; init; }

    /// <summary>
    /// Number of distinct tile art identifiers present in the tile grid.
    /// </summary>
    public required int DistinctTileArtCount { get; init; }

    /// <summary>
    /// Number of blocked tiles derived from the sector block mask.
    /// </summary>
    public required int BlockedTileCount { get; init; }

    /// <summary>
    /// Sector light-scheme index.
    /// </summary>
    public required int LightSchemeIndex { get; init; }

    /// <summary>
    /// Sector music-scheme index, or <c>-1</c> when unset.
    /// </summary>
    public required int MusicSchemeIndex { get; init; }

    /// <summary>
    /// Sector ambient-sound scheme index, or <c>-1</c> when unset.
    /// </summary>
    public required int AmbientSchemeIndex { get; init; }
}
