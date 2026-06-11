using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// One terrain preset backed by one or more reusable terrain template sectors.
/// </summary>
public sealed class EditorTerrainPresetEntry
{
    /// <summary>
    /// Message-table terrain type index from <c>terrain/terrain.mes</c>.
    /// </summary>
    public required int TerrainTypeId { get; init; }

    /// <summary>
    /// Parsed terrain type when the index maps to a known enum value.
    /// </summary>
    public TerrainType? TerrainType =>
        Enum.IsDefined(typeof(TerrainType), (byte)TerrainTypeId) ? (TerrainType)TerrainTypeId : null;

    /// <summary>
    /// Browser-friendly label for the preset.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Normalized terrain-directory name that owns the template sectors.
    /// </summary>
    public required string TerrainDirectoryName { get; init; }

    /// <summary>
    /// Template sector asset paths available for this preset.
    /// </summary>
    public required IReadOnlyList<string> TemplateSectorAssetPaths { get; init; }

    /// <summary>
    /// Preview ART asset path resolved from one representative template sector tile.
    /// </summary>
    public string? PreviewArtAssetPath { get; init; }

    /// <summary>
    /// Number of distinct non-zero tile art identifiers present in the representative template sector.
    /// </summary>
    public required int DistinctTileArtCount { get; init; }

    /// <summary>
    /// Number of scenery objects staged by the representative template sector.
    /// </summary>
    public required int SceneryObjectCount { get; init; }

    /// <summary>
    /// Primary representative template sector asset path.
    /// </summary>
    public string PrimaryTemplateSectorAssetPath => TemplateSectorAssetPaths[0];
}
