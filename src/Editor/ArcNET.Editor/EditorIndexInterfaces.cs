namespace ArcNET.Editor;

/// <summary>
/// Read-only map and sector queries exposed by the editor asset index.
/// </summary>
public interface IMapIndex
{
    IReadOnlyList<string> MapNames { get; }

    IReadOnlyList<string> SearchMapNames(string text);

    IReadOnlyList<EditorAssetEntry> FindMapAssets(string mapName);

    string? FindAssetMap(string assetPath);

    IReadOnlyList<EditorSectorSummary> FindMapSectors(string mapName);

    IReadOnlyList<EditorSectorSummary> SearchSectors(string text);

    EditorSectorSummary? FindSectorSummary(string assetPath);

    EditorMapProjection? FindMapProjection(string mapName);
}

/// <summary>
/// Read-only asset-path-based dependency summaries exposed by the editor asset index.
/// </summary>
public interface IAssetDependencyIndex
{
    EditorAssetDependencySummary? FindAssetDependencySummary(string assetPath);
}

/// <summary>
/// Read-only sector environment-scheme queries exposed by the editor asset index.
/// </summary>
public interface ISchemeIndex
{
    IReadOnlyList<EditorSectorSummary> FindLightSchemeSectors(int lightSchemeIndex);

    IReadOnlyList<EditorSectorSummary> FindMusicSchemeSectors(int musicSchemeIndex);

    IReadOnlyList<EditorSectorSummary> FindAmbientSchemeSectors(int ambientSchemeIndex);
}

/// <summary>
/// Read-only message lookup queries exposed by the editor asset index.
/// </summary>
public interface IMessageIndex
{
    IReadOnlyList<EditorAssetEntry> FindMessageAssets(int messageIndex);
}

/// <summary>
/// Read-only proto definition and reverse-reference queries exposed by the editor asset index.
/// </summary>
public interface IProtoIndex
{
    EditorAssetEntry? FindProtoDefinition(int protoNumber);

    IReadOnlyList<EditorProtoReference> FindProtoReferences(int protoNumber);
}

/// <summary>
/// Read-only script definition and reverse-reference queries exposed by the editor asset index.
/// </summary>
public interface IScriptIndex
{
    EditorAssetEntry? FindScriptDefinition(int scriptId);

    IReadOnlyList<EditorAssetEntry> FindScriptDefinitions(int scriptId);

    IReadOnlyList<EditorScriptDefinition> FindScriptDetails(int scriptId);

    IReadOnlyList<EditorScriptDefinition> SearchScriptDetails(string text);

    IReadOnlyList<EditorScriptReference> FindScriptReferences(int scriptId);
}

/// <summary>
/// Read-only dialog definition queries exposed by the editor asset index.
/// </summary>
public interface IDialogIndex
{
    EditorAssetEntry? FindDialogDefinition(int dialogId);

    IReadOnlyList<EditorAssetEntry> FindDialogDefinitions(int dialogId);

    IReadOnlyList<EditorDialogDefinition> FindDialogDetails(int dialogId);

    IReadOnlyList<EditorDialogDefinition> SearchDialogDetails(string text);
}

/// <summary>
/// Read-only art reverse-reference queries exposed by the editor asset index.
/// </summary>
public interface IArtIndex
{
    EditorArtDefinition? FindArtDetail(string assetPath);

    IReadOnlyList<EditorArtDefinition> SearchArtDetails(string text);

    IReadOnlyList<EditorArtReference> FindArtReferences(uint artId);
}

/// <summary>
/// Read-only jump-file detail queries exposed by the editor asset index.
/// </summary>
public interface IJumpIndex
{
    EditorJumpDefinition? FindJumpDetail(string assetPath);

    IReadOnlyList<EditorJumpDefinition> SearchJumpDetails(string text);
}

/// <summary>
/// Read-only map-properties detail queries exposed by the editor asset index.
/// </summary>
public interface IMapPropertiesIndex
{
    EditorMapPropertiesDefinition? FindMapPropertiesDetail(string assetPath);

    IReadOnlyList<EditorMapPropertiesDefinition> SearchMapPropertiesDetails(string text);
}

/// <summary>
/// Read-only terrain detail queries exposed by the editor asset index.
/// </summary>
public interface ITerrainIndex
{
    EditorTerrainDefinition? FindTerrainDetail(string assetPath);

    IReadOnlyList<EditorTerrainDefinition> SearchTerrainDetails(string text);
}

/// <summary>
/// Read-only facade-walk detail queries exposed by the editor asset index.
/// </summary>
public interface IFacadeWalkIndex
{
    EditorFacadeWalkDefinition? FindFacadeWalkDetail(string assetPath);

    IReadOnlyList<EditorFacadeWalkDefinition> SearchFacadeWalkDetails(string text);
}
