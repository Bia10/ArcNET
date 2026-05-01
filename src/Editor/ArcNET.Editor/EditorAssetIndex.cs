using System.Globalization;

namespace ArcNET.Editor;

/// <summary>
/// Workspace-level asset index for higher-level lookups such as map ownership,
/// message ownership, numbered asset definition lookup, and reverse references.
/// </summary>
public sealed class EditorAssetIndex
    : IMapIndex,
        IAssetDependencyIndex,
        ISchemeIndex,
        IMessageIndex,
        IProtoIndex,
        IScriptIndex,
        IDialogIndex,
        IArtIndex,
        IJumpIndex,
        IMapPropertiesIndex,
        ITerrainIndex,
        IFacadeWalkIndex
{
    private readonly EditorAssetIndexData _data;

    private EditorAssetIndex(EditorAssetIndexData data) => _data = data;

    /// <summary>
    /// Empty asset index used when no richer asset relationships were discovered.
    /// </summary>
    public static EditorAssetIndex Empty { get; } = new(EditorAssetIndexData.Empty);

    /// <summary>
    /// All distinct map directory names discovered under <c>maps/</c> in the asset catalog.
    /// </summary>
    public IReadOnlyList<string> MapNames => _data.MapNames;

    /// <summary>
    /// Returns all indexed map directory names that contain the supplied text.
    /// </summary>
    public IReadOnlyList<string> SearchMapNames(string text)
    {
        var searchText = ValidateSearchText(text);
        return _data.MapNames.Where(name => ContainsSearchText(name, searchText)).ToArray();
    }

    /// <summary>
    /// Returns all indexed assets that belong to the supplied map directory name.
    /// </summary>
    public IReadOnlyList<EditorAssetEntry> FindMapAssets(string mapName) =>
        _data.MapAssetsByName.TryGetValue(mapName, out var assets) ? assets : [];

    /// <summary>
    /// Returns the map directory name for the supplied asset path, or <see langword="null"/>
    /// when the asset is not under <c>maps/&lt;name&gt;/</c>.
    /// </summary>
    public string? FindAssetMap(string assetPath) =>
        _data.MapNameByAssetPath.TryGetValue(assetPath, out var mapName) ? mapName : null;

    /// <summary>
    /// Returns the asset-centric dependency summary for one indexed asset path, or <see langword="null"/>
    /// when the asset path was not present in the workspace asset catalog.
    /// </summary>
    public EditorAssetDependencySummary? FindAssetDependencySummary(string assetPath) =>
        _data.AssetDependencySummariesByAssetPath.TryGetValue(assetPath, out var summary) ? summary : null;

    /// <summary>
    /// Returns parsed sector summaries for all indexed sector assets that belong to one map.
    /// </summary>
    public IReadOnlyList<EditorSectorSummary> FindMapSectors(string mapName) =>
        _data.MapSectorsByName.TryGetValue(mapName, out var sectors) ? sectors : [];

    /// <summary>
    /// Returns indexed sector summaries whose map name or asset path contains the supplied text.
    /// </summary>
    public IReadOnlyList<EditorSectorSummary> SearchSectors(string text)
    {
        var searchText = ValidateSearchText(text);
        return _data
            .SectorSummariesByAssetPath.Values.Where(summary =>
                ContainsSearchText(summary.MapName, searchText)
                || ContainsSearchText(summary.Asset.AssetPath, searchText)
            )
            .OrderBy(summary => summary.Asset.AssetPath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Returns the parsed summary for one sector asset path, or <see langword="null"/>
    /// when the asset was not indexed as a sector.
    /// </summary>
    public EditorSectorSummary? FindSectorSummary(string assetPath) =>
        _data.SectorSummariesByAssetPath.TryGetValue(assetPath, out var sector) ? sector : null;

    /// <summary>
    /// Returns all indexed sectors that use the supplied light-scheme index.
    /// </summary>
    public IReadOnlyList<EditorSectorSummary> FindLightSchemeSectors(int lightSchemeIndex) =>
        _data.LightSchemeSectorsByIndex.TryGetValue(lightSchemeIndex, out var sectors) ? sectors : [];

    /// <summary>
    /// Returns all indexed sectors that use the supplied music-scheme index.
    /// </summary>
    public IReadOnlyList<EditorSectorSummary> FindMusicSchemeSectors(int musicSchemeIndex) =>
        _data.MusicSchemeSectorsByIndex.TryGetValue(musicSchemeIndex, out var sectors) ? sectors : [];

    /// <summary>
    /// Returns all indexed sectors that use the supplied ambient-sound scheme index.
    /// </summary>
    public IReadOnlyList<EditorSectorSummary> FindAmbientSchemeSectors(int ambientSchemeIndex) =>
        _data.AmbientSchemeSectorsByIndex.TryGetValue(ambientSchemeIndex, out var sectors) ? sectors : [];

    /// <summary>
    /// Returns a projected 2D sector layout for the supplied map name, or <see langword="null"/>
    /// when no sector projection data was indexed for that map.
    /// </summary>
    public EditorMapProjection? FindMapProjection(string mapName) =>
        _data.MapProjectionsByName.TryGetValue(mapName, out var projection) ? projection : null;

    /// <summary>
    /// Returns all message assets that define the supplied message index.
    /// </summary>
    public IReadOnlyList<EditorAssetEntry> FindMessageAssets(int messageIndex) =>
        _data.MessageAssetsByIndex.TryGetValue(messageIndex, out var assets) ? assets : [];

    /// <summary>
    /// Returns the asset that defines the supplied proto number, or <see langword="null"/>
    /// when no matching prototype asset was indexed.
    /// </summary>
    public EditorAssetEntry? FindProtoDefinition(int protoNumber) =>
        _data.ProtoDefinitionsByNumber.TryGetValue(protoNumber, out var asset) ? asset : null;

    /// <summary>
    /// Returns all assets whose file name starts with the supplied script identifier.
    /// </summary>
    public IReadOnlyList<EditorAssetEntry> FindScriptDefinitions(int scriptId) =>
        _data.ScriptDefinitionsById.TryGetValue(scriptId, out var assets) ? assets : [];

    /// <summary>
    /// Returns all assets whose file name starts with the supplied dialog identifier.
    /// </summary>
    public IReadOnlyList<EditorAssetEntry> FindDialogDefinitions(int dialogId) =>
        _data.DialogDefinitionsById.TryGetValue(dialogId, out var assets) ? assets : [];

    /// <summary>
    /// Returns higher-level attachment summaries for all script assets whose file name
    /// starts with the supplied script identifier.
    /// </summary>
    public IReadOnlyList<EditorScriptDefinition> FindScriptDetails(int scriptId) =>
        _data.ScriptDetailsById.TryGetValue(scriptId, out var details) ? details : [];

    /// <summary>
    /// Returns script definitions whose identifier, asset path, or description contains the supplied text.
    /// </summary>
    public IReadOnlyList<EditorScriptDefinition> SearchScriptDetails(string text)
    {
        var searchText = ValidateSearchText(text);
        return _data
            .ScriptDetailsById.Values.SelectMany(static details => details)
            .Where(detail =>
                ContainsSearchText(detail.Asset.AssetPath, searchText)
                || ContainsSearchText(detail.Description, searchText)
                || ContainsSearchText(detail.ScriptId.ToString(CultureInfo.InvariantCulture), searchText)
            )
            .OrderBy(detail => detail.Asset.AssetPath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Returns higher-level graph summaries for all dialog assets whose file name
    /// starts with the supplied dialog identifier.
    /// </summary>
    public IReadOnlyList<EditorDialogDefinition> FindDialogDetails(int dialogId) =>
        _data.DialogDetailsById.TryGetValue(dialogId, out var details) ? details : [];

    /// <summary>
    /// Returns dialog definitions whose identifier, asset path, or node text contains the supplied text.
    /// </summary>
    public IReadOnlyList<EditorDialogDefinition> SearchDialogDetails(string text)
    {
        var searchText = ValidateSearchText(text);
        return _data
            .DialogDetailsById.Values.SelectMany(static details => details)
            .Where(detail =>
                ContainsSearchText(detail.Asset.AssetPath, searchText)
                || ContainsSearchText(detail.DialogId.ToString(CultureInfo.InvariantCulture), searchText)
                || detail.Nodes.Any(node => ContainsSearchText(node.Text, searchText))
            )
            .OrderBy(detail => detail.Asset.AssetPath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Returns the first asset whose file name starts with the supplied script identifier,
    /// or <see langword="null"/> when no matching compiled script asset was indexed.
    /// </summary>
    public EditorAssetEntry? FindScriptDefinition(int scriptId)
    {
        var definitions = FindScriptDefinitions(scriptId);
        return definitions.Count > 0 ? definitions[0] : null;
    }

    /// <summary>
    /// Returns the first asset whose file name starts with the supplied dialog identifier,
    /// or <see langword="null"/> when no matching dialog asset was indexed.
    /// </summary>
    public EditorAssetEntry? FindDialogDefinition(int dialogId)
    {
        var definitions = FindDialogDefinitions(dialogId);
        return definitions.Count > 0 ? definitions[0] : null;
    }

    /// <summary>
    /// Returns all assets that reference the supplied proto number through mobile objects
    /// or sector-embedded objects.
    /// </summary>
    public IReadOnlyList<EditorProtoReference> FindProtoReferences(int protoNumber) =>
        _data.ProtoReferencesByNumber.TryGetValue(protoNumber, out var references) ? references : [];

    /// <summary>
    /// Returns all assets that reference the supplied script identifier.
    /// </summary>
    public IReadOnlyList<EditorScriptReference> FindScriptReferences(int scriptId) =>
        _data.ScriptReferencesById.TryGetValue(scriptId, out var references) ? references : [];

    /// <summary>
    /// Returns all assets that reference the supplied art identifier.
    /// </summary>
    public EditorArtDefinition? FindArtDetail(string assetPath) =>
        _data.ArtDetailsByAssetPath.TryGetValue(assetPath, out var detail) ? detail : null;

    /// <summary>
    /// Returns indexed art definitions whose asset path contains the supplied text.
    /// </summary>
    public IReadOnlyList<EditorArtDefinition> SearchArtDetails(string text)
    {
        var searchText = ValidateSearchText(text);
        return _data
            .ArtDetailsByAssetPath.Values.Where(detail => ContainsSearchText(detail.Asset.AssetPath, searchText))
            .OrderBy(detail => detail.Asset.AssetPath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Returns all assets that reference the supplied art identifier.
    /// </summary>
    public IReadOnlyList<EditorArtReference> FindArtReferences(uint artId) =>
        _data.ArtReferencesById.TryGetValue(artId, out var references) ? references : [];

    /// <summary>
    /// Returns the indexed jump-file detail for one asset path, or <see langword="null"/>
    /// when the asset was not indexed as a jump file.
    /// </summary>
    public EditorJumpDefinition? FindJumpDetail(string assetPath) =>
        _data.JumpDetailsByAssetPath.TryGetValue(assetPath, out var detail) ? detail : null;

    /// <summary>
    /// Returns indexed jump-file details whose asset path or destination-map identifiers contain the supplied text.
    /// </summary>
    public IReadOnlyList<EditorJumpDefinition> SearchJumpDetails(string text)
    {
        var searchText = ValidateSearchText(text);
        return _data
            .JumpDetailsByAssetPath.Values.Where(detail =>
                ContainsSearchText(detail.Asset.AssetPath, searchText)
                || detail.DestinationMapIds.Any(mapId =>
                    ContainsSearchText(mapId.ToString(CultureInfo.InvariantCulture), searchText)
                )
            )
            .OrderBy(detail => detail.Asset.AssetPath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Returns the indexed map-properties detail for one asset path, or <see langword="null"/>
    /// when the asset was not indexed as a map-properties file.
    /// </summary>
    public EditorMapPropertiesDefinition? FindMapPropertiesDetail(string assetPath) =>
        _data.MapPropertiesDetailsByAssetPath.TryGetValue(assetPath, out var detail) ? detail : null;

    /// <summary>
    /// Returns indexed map-properties details whose asset path or terrain art identifier contains the supplied text.
    /// </summary>
    public IReadOnlyList<EditorMapPropertiesDefinition> SearchMapPropertiesDetails(string text)
    {
        var searchText = ValidateSearchText(text);
        return _data
            .MapPropertiesDetailsByAssetPath.Values.Where(detail =>
                ContainsSearchText(detail.Asset.AssetPath, searchText)
                || ContainsSearchText(detail.ArtId.ToString(CultureInfo.InvariantCulture), searchText)
            )
            .OrderBy(detail => detail.Asset.AssetPath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Returns the indexed terrain detail for one asset path, or <see langword="null"/>
    /// when the asset was not indexed as a terrain-definition file.
    /// </summary>
    public EditorTerrainDefinition? FindTerrainDetail(string assetPath) =>
        _data.TerrainDetailsByAssetPath.TryGetValue(assetPath, out var detail) ? detail : null;

    /// <summary>
    /// Returns indexed terrain details whose asset path or base terrain type contains the supplied text.
    /// </summary>
    public IReadOnlyList<EditorTerrainDefinition> SearchTerrainDetails(string text)
    {
        var searchText = ValidateSearchText(text);
        return _data
            .TerrainDetailsByAssetPath.Values.Where(detail =>
                ContainsSearchText(detail.Asset.AssetPath, searchText)
                || ContainsSearchText(detail.BaseTerrainType.ToString(), searchText)
            )
            .OrderBy(detail => detail.Asset.AssetPath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Returns the indexed facade-walk detail for one asset path, or <see langword="null"/>
    /// when the asset was not indexed as a facade-walk file.
    /// </summary>
    public EditorFacadeWalkDefinition? FindFacadeWalkDetail(string assetPath) =>
        _data.FacadeWalkDetailsByAssetPath.TryGetValue(assetPath, out var detail) ? detail : null;

    /// <summary>
    /// Returns indexed facade-walk details whose asset path or terrain index contains the supplied text.
    /// </summary>
    public IReadOnlyList<EditorFacadeWalkDefinition> SearchFacadeWalkDetails(string text)
    {
        var searchText = ValidateSearchText(text);
        return _data
            .FacadeWalkDetailsByAssetPath.Values.Where(detail =>
                ContainsSearchText(detail.Asset.AssetPath, searchText)
                || ContainsSearchText(detail.Terrain.ToString(CultureInfo.InvariantCulture), searchText)
            )
            .OrderBy(detail => detail.Asset.AssetPath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    internal static EditorAssetIndex Create(EditorAssetIndexData data) => data.IsEmpty ? Empty : new(data);

    private static string ValidateSearchText(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        return text.Trim();
    }

    private static bool ContainsSearchText(string value, string searchText) =>
        value.Contains(searchText, StringComparison.OrdinalIgnoreCase);
}
