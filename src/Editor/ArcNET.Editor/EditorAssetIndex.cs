using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// Workspace-level asset index for higher-level lookups such as map ownership,
/// message ownership, numbered asset definition lookup, and reverse references.
/// </summary>
public sealed class EditorAssetIndex
{
    private readonly IReadOnlyList<string> _mapNames;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<EditorAssetEntry>> _mapAssetsByName;
    private readonly IReadOnlyDictionary<string, string> _mapNameByAssetPath;
    private readonly IReadOnlyDictionary<int, IReadOnlyList<EditorAssetEntry>> _messageAssetsByIndex;
    private readonly IReadOnlyDictionary<int, EditorAssetEntry> _protoDefinitionsByNumber;
    private readonly IReadOnlyDictionary<int, IReadOnlyList<EditorAssetEntry>> _scriptDefinitionsById;
    private readonly IReadOnlyDictionary<int, IReadOnlyList<EditorAssetEntry>> _dialogDefinitionsById;
    private readonly IReadOnlyDictionary<int, IReadOnlyList<EditorScriptDefinition>> _scriptDetailsById;
    private readonly IReadOnlyDictionary<int, IReadOnlyList<EditorDialogDefinition>> _dialogDetailsById;
    private readonly IReadOnlyDictionary<int, IReadOnlyList<EditorProtoReference>> _protoReferencesByNumber;
    private readonly IReadOnlyDictionary<int, IReadOnlyList<EditorScriptReference>> _scriptReferencesById;
    private readonly IReadOnlyDictionary<uint, IReadOnlyList<EditorArtReference>> _artReferencesById;

    private EditorAssetIndex(
        IReadOnlyList<string> mapNames,
        IReadOnlyDictionary<string, IReadOnlyList<EditorAssetEntry>> mapAssetsByName,
        IReadOnlyDictionary<string, string> mapNameByAssetPath,
        IReadOnlyDictionary<int, IReadOnlyList<EditorAssetEntry>> messageAssetsByIndex,
        IReadOnlyDictionary<int, EditorAssetEntry> protoDefinitionsByNumber,
        IReadOnlyDictionary<int, IReadOnlyList<EditorAssetEntry>> scriptDefinitionsById,
        IReadOnlyDictionary<int, IReadOnlyList<EditorAssetEntry>> dialogDefinitionsById,
        IReadOnlyDictionary<int, IReadOnlyList<EditorScriptDefinition>> scriptDetailsById,
        IReadOnlyDictionary<int, IReadOnlyList<EditorDialogDefinition>> dialogDetailsById,
        IReadOnlyDictionary<int, IReadOnlyList<EditorProtoReference>> protoReferencesByNumber,
        IReadOnlyDictionary<int, IReadOnlyList<EditorScriptReference>> scriptReferencesById,
        IReadOnlyDictionary<uint, IReadOnlyList<EditorArtReference>> artReferencesById
    )
    {
        _mapNames = mapNames;
        _mapAssetsByName = mapAssetsByName;
        _mapNameByAssetPath = mapNameByAssetPath;
        _messageAssetsByIndex = messageAssetsByIndex;
        _protoDefinitionsByNumber = protoDefinitionsByNumber;
        _scriptDefinitionsById = scriptDefinitionsById;
        _dialogDefinitionsById = dialogDefinitionsById;
        _scriptDetailsById = scriptDetailsById;
        _dialogDetailsById = dialogDetailsById;
        _protoReferencesByNumber = protoReferencesByNumber;
        _scriptReferencesById = scriptReferencesById;
        _artReferencesById = artReferencesById;
    }

    /// <summary>
    /// Empty asset index used when no richer asset relationships were discovered.
    /// </summary>
    public static EditorAssetIndex Empty { get; } =
        new(
            [],
            new Dictionary<string, IReadOnlyList<EditorAssetEntry>>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<int, IReadOnlyList<EditorAssetEntry>>(),
            new Dictionary<int, EditorAssetEntry>(),
            new Dictionary<int, IReadOnlyList<EditorAssetEntry>>(),
            new Dictionary<int, IReadOnlyList<EditorAssetEntry>>(),
            new Dictionary<int, IReadOnlyList<EditorScriptDefinition>>(),
            new Dictionary<int, IReadOnlyList<EditorDialogDefinition>>(),
            new Dictionary<int, IReadOnlyList<EditorProtoReference>>(),
            new Dictionary<int, IReadOnlyList<EditorScriptReference>>(),
            new Dictionary<uint, IReadOnlyList<EditorArtReference>>()
        );

    /// <summary>
    /// All distinct map directory names discovered under <c>maps/</c> in the asset catalog.
    /// </summary>
    public IReadOnlyList<string> MapNames => _mapNames;

    /// <summary>
    /// Returns all indexed assets that belong to the supplied map directory name.
    /// </summary>
    public IReadOnlyList<EditorAssetEntry> FindMapAssets(string mapName) =>
        _mapAssetsByName.TryGetValue(mapName, out var assets) ? assets : [];

    /// <summary>
    /// Returns the map directory name for the supplied asset path, or <see langword="null"/>
    /// when the asset is not under <c>maps/&lt;name&gt;/</c>.
    /// </summary>
    public string? FindAssetMap(string assetPath) =>
        _mapNameByAssetPath.TryGetValue(assetPath, out var mapName) ? mapName : null;

    /// <summary>
    /// Returns all message assets that define the supplied message index.
    /// </summary>
    public IReadOnlyList<EditorAssetEntry> FindMessageAssets(int messageIndex) =>
        _messageAssetsByIndex.TryGetValue(messageIndex, out var assets) ? assets : [];

    /// <summary>
    /// Returns the asset that defines the supplied proto number, or <see langword="null"/>
    /// when no matching prototype asset was indexed.
    /// </summary>
    public EditorAssetEntry? FindProtoDefinition(int protoNumber) =>
        _protoDefinitionsByNumber.TryGetValue(protoNumber, out var asset) ? asset : null;

    /// <summary>
    /// Returns all assets whose file name starts with the supplied script identifier.
    /// </summary>
    public IReadOnlyList<EditorAssetEntry> FindScriptDefinitions(int scriptId) =>
        _scriptDefinitionsById.TryGetValue(scriptId, out var assets) ? assets : [];

    /// <summary>
    /// Returns all assets whose file name starts with the supplied dialog identifier.
    /// </summary>
    public IReadOnlyList<EditorAssetEntry> FindDialogDefinitions(int dialogId) =>
        _dialogDefinitionsById.TryGetValue(dialogId, out var assets) ? assets : [];

    /// <summary>
    /// Returns higher-level attachment summaries for all script assets whose file name
    /// starts with the supplied script identifier.
    /// </summary>
    public IReadOnlyList<EditorScriptDefinition> FindScriptDetails(int scriptId) =>
        _scriptDetailsById.TryGetValue(scriptId, out var details) ? details : [];

    /// <summary>
    /// Returns higher-level graph summaries for all dialog assets whose file name
    /// starts with the supplied dialog identifier.
    /// </summary>
    public IReadOnlyList<EditorDialogDefinition> FindDialogDetails(int dialogId) =>
        _dialogDetailsById.TryGetValue(dialogId, out var details) ? details : [];

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
        _protoReferencesByNumber.TryGetValue(protoNumber, out var references) ? references : [];

    /// <summary>
    /// Returns all assets that reference the supplied script identifier.
    /// </summary>
    public IReadOnlyList<EditorScriptReference> FindScriptReferences(int scriptId) =>
        _scriptReferencesById.TryGetValue(scriptId, out var references) ? references : [];

    /// <summary>
    /// Returns all assets that reference the supplied art identifier.
    /// </summary>
    public IReadOnlyList<EditorArtReference> FindArtReferences(uint artId) =>
        _artReferencesById.TryGetValue(artId, out var references) ? references : [];

    internal static EditorAssetIndex Create(
        IReadOnlyList<string> mapNames,
        IReadOnlyDictionary<string, IReadOnlyList<EditorAssetEntry>> mapAssetsByName,
        IReadOnlyDictionary<string, string> mapNameByAssetPath,
        IReadOnlyDictionary<int, IReadOnlyList<EditorAssetEntry>> messageAssetsByIndex,
        IReadOnlyDictionary<int, EditorAssetEntry> protoDefinitionsByNumber,
        IReadOnlyDictionary<int, IReadOnlyList<EditorAssetEntry>> scriptDefinitionsById,
        IReadOnlyDictionary<int, IReadOnlyList<EditorAssetEntry>> dialogDefinitionsById,
        IReadOnlyDictionary<int, IReadOnlyList<EditorScriptDefinition>> scriptDetailsById,
        IReadOnlyDictionary<int, IReadOnlyList<EditorDialogDefinition>> dialogDetailsById,
        IReadOnlyDictionary<int, IReadOnlyList<EditorProtoReference>> protoReferencesByNumber,
        IReadOnlyDictionary<int, IReadOnlyList<EditorScriptReference>> scriptReferencesById,
        IReadOnlyDictionary<uint, IReadOnlyList<EditorArtReference>> artReferencesById
    ) =>
        mapNames.Count == 0
        && messageAssetsByIndex.Count == 0
        && protoDefinitionsByNumber.Count == 0
        && scriptDefinitionsById.Count == 0
        && dialogDefinitionsById.Count == 0
        && scriptDetailsById.Count == 0
        && dialogDetailsById.Count == 0
        && protoReferencesByNumber.Count == 0
        && scriptReferencesById.Count == 0
        && artReferencesById.Count == 0
            ? Empty
            : new EditorAssetIndex(
                mapNames,
                mapAssetsByName,
                mapNameByAssetPath,
                messageAssetsByIndex,
                protoDefinitionsByNumber,
                scriptDefinitionsById,
                dialogDefinitionsById,
                scriptDetailsById,
                dialogDetailsById,
                protoReferencesByNumber,
                scriptReferencesById,
                artReferencesById
            );
}
