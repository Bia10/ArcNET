namespace ArcNET.Editor;

internal sealed record EditorAssetIndexData
{
    public static EditorAssetIndexData Empty { get; } =
        new()
        {
            MapNames = [],
            MapAssetsByName = new Dictionary<string, IReadOnlyList<EditorAssetEntry>>(StringComparer.OrdinalIgnoreCase),
            MapNameByAssetPath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            AssetDependencySummariesByAssetPath = new Dictionary<string, EditorAssetDependencySummary>(
                StringComparer.OrdinalIgnoreCase
            ),
            MapSectorsByName = new Dictionary<string, IReadOnlyList<EditorSectorSummary>>(
                StringComparer.OrdinalIgnoreCase
            ),
            SectorSummariesByAssetPath = new Dictionary<string, EditorSectorSummary>(StringComparer.OrdinalIgnoreCase),
            LightSchemeSectorsByIndex = new Dictionary<int, IReadOnlyList<EditorSectorSummary>>(),
            MusicSchemeSectorsByIndex = new Dictionary<int, IReadOnlyList<EditorSectorSummary>>(),
            AmbientSchemeSectorsByIndex = new Dictionary<int, IReadOnlyList<EditorSectorSummary>>(),
            MapProjectionsByName = new Dictionary<string, EditorMapProjection>(StringComparer.OrdinalIgnoreCase),
            MessageAssetsByIndex = new Dictionary<int, IReadOnlyList<EditorAssetEntry>>(),
            ProtoDefinitionsByNumber = new Dictionary<int, EditorAssetEntry>(),
            ScriptDefinitionsById = new Dictionary<int, IReadOnlyList<EditorAssetEntry>>(),
            DialogDefinitionsById = new Dictionary<int, IReadOnlyList<EditorAssetEntry>>(),
            ScriptDetailsById = new Dictionary<int, IReadOnlyList<EditorScriptDefinition>>(),
            DialogDetailsById = new Dictionary<int, IReadOnlyList<EditorDialogDefinition>>(),
            ArtDetailsByAssetPath = new Dictionary<string, EditorArtDefinition>(StringComparer.OrdinalIgnoreCase),
            JumpDetailsByAssetPath = new Dictionary<string, EditorJumpDefinition>(StringComparer.OrdinalIgnoreCase),
            MapPropertiesDetailsByAssetPath = new Dictionary<string, EditorMapPropertiesDefinition>(
                StringComparer.OrdinalIgnoreCase
            ),
            TerrainDetailsByAssetPath = new Dictionary<string, EditorTerrainDefinition>(
                StringComparer.OrdinalIgnoreCase
            ),
            FacadeWalkDetailsByAssetPath = new Dictionary<string, EditorFacadeWalkDefinition>(
                StringComparer.OrdinalIgnoreCase
            ),
            ProtoReferencesByNumber = new Dictionary<int, IReadOnlyList<EditorProtoReference>>(),
            ScriptReferencesById = new Dictionary<int, IReadOnlyList<EditorScriptReference>>(),
            ArtReferencesById = new Dictionary<uint, IReadOnlyList<EditorArtReference>>(),
        };

    public required IReadOnlyList<string> MapNames { get; init; }
    public required IReadOnlyDictionary<string, IReadOnlyList<EditorAssetEntry>> MapAssetsByName { get; init; }
    public required IReadOnlyDictionary<string, string> MapNameByAssetPath { get; init; }
    public required IReadOnlyDictionary<
        string,
        EditorAssetDependencySummary
    > AssetDependencySummariesByAssetPath { get; init; }
    public required IReadOnlyDictionary<string, IReadOnlyList<EditorSectorSummary>> MapSectorsByName { get; init; }
    public required IReadOnlyDictionary<string, EditorSectorSummary> SectorSummariesByAssetPath { get; init; }
    public required IReadOnlyDictionary<
        int,
        IReadOnlyList<EditorSectorSummary>
    > LightSchemeSectorsByIndex { get; init; }
    public required IReadOnlyDictionary<
        int,
        IReadOnlyList<EditorSectorSummary>
    > MusicSchemeSectorsByIndex { get; init; }
    public required IReadOnlyDictionary<
        int,
        IReadOnlyList<EditorSectorSummary>
    > AmbientSchemeSectorsByIndex { get; init; }
    public required IReadOnlyDictionary<string, EditorMapProjection> MapProjectionsByName { get; init; }
    public required IReadOnlyDictionary<int, IReadOnlyList<EditorAssetEntry>> MessageAssetsByIndex { get; init; }
    public required IReadOnlyDictionary<int, EditorAssetEntry> ProtoDefinitionsByNumber { get; init; }
    public required IReadOnlyDictionary<int, IReadOnlyList<EditorAssetEntry>> ScriptDefinitionsById { get; init; }
    public required IReadOnlyDictionary<int, IReadOnlyList<EditorAssetEntry>> DialogDefinitionsById { get; init; }
    public required IReadOnlyDictionary<int, IReadOnlyList<EditorScriptDefinition>> ScriptDetailsById { get; init; }
    public required IReadOnlyDictionary<int, IReadOnlyList<EditorDialogDefinition>> DialogDetailsById { get; init; }
    public required IReadOnlyDictionary<string, EditorArtDefinition> ArtDetailsByAssetPath { get; init; }
    public required IReadOnlyDictionary<string, EditorJumpDefinition> JumpDetailsByAssetPath { get; init; }
    public required IReadOnlyDictionary<
        string,
        EditorMapPropertiesDefinition
    > MapPropertiesDetailsByAssetPath { get; init; }
    public required IReadOnlyDictionary<string, EditorTerrainDefinition> TerrainDetailsByAssetPath { get; init; }
    public required IReadOnlyDictionary<string, EditorFacadeWalkDefinition> FacadeWalkDetailsByAssetPath { get; init; }
    public required IReadOnlyDictionary<int, IReadOnlyList<EditorProtoReference>> ProtoReferencesByNumber { get; init; }
    public required IReadOnlyDictionary<int, IReadOnlyList<EditorScriptReference>> ScriptReferencesById { get; init; }
    public required IReadOnlyDictionary<uint, IReadOnlyList<EditorArtReference>> ArtReferencesById { get; init; }

    public bool IsEmpty =>
        MapNames.Count == 0
        && AssetDependencySummariesByAssetPath.Count == 0
        && SectorSummariesByAssetPath.Count == 0
        && LightSchemeSectorsByIndex.Count == 0
        && MusicSchemeSectorsByIndex.Count == 0
        && AmbientSchemeSectorsByIndex.Count == 0
        && MapProjectionsByName.Count == 0
        && MessageAssetsByIndex.Count == 0
        && ProtoDefinitionsByNumber.Count == 0
        && ScriptDefinitionsById.Count == 0
        && DialogDefinitionsById.Count == 0
        && ScriptDetailsById.Count == 0
        && DialogDetailsById.Count == 0
        && ArtDetailsByAssetPath.Count == 0
        && JumpDetailsByAssetPath.Count == 0
        && MapPropertiesDetailsByAssetPath.Count == 0
        && TerrainDetailsByAssetPath.Count == 0
        && FacadeWalkDetailsByAssetPath.Count == 0
        && ProtoReferencesByNumber.Count == 0
        && ScriptReferencesById.Count == 0
        && ArtReferencesById.Count == 0;
}
