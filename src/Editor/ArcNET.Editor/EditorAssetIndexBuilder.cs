using System.Text.RegularExpressions;
using ArcNET.Core;
using ArcNET.Formats;
using ArcNET.GameData;
using ArcNET.GameObjects;
using static ArcNET.Editor.EditorWorkspaceValidationIssue;

namespace ArcNET.Editor;

internal static partial class EditorAssetIndexBuilder
{
    private const string DescriptionMesAssetPath = "mes/description.mes";
    private const string ProtoNameOverrideAssetPath = "oemes/oname.mes";
    private static readonly Regex s_protoAssetPathPattern = ProtoAssetPathPattern();
    private static readonly Regex s_scriptAssetPathPattern = ScriptAssetPathPattern();
    private static readonly Regex s_dialogAssetPathPattern = DialogAssetPathPattern();

    public static (EditorAssetIndex Index, EditorWorkspaceValidationReport Validation) Create(
        GameDataStore gameData,
        EditorAssetCatalog assets,
        ArcanumInstallationType? installationType = null
    )
    {
        ArgumentNullException.ThrowIfNull(gameData);
        ArgumentNullException.ThrowIfNull(assets);

        var assetsByPath = assets.Entries.ToDictionary(entry => entry.AssetPath, StringComparer.OrdinalIgnoreCase);
        var (mapNames, mapAssetsByName, mapNameByAssetPath) = BuildMapAssets(assets.Entries);
        var messageAssetsByIndex = BuildMessageAssetsByIndex(gameData, assetsByPath);
        var protoDisplayNameMessageIndices = BuildProtoDisplayNameMessageIndices(gameData);
        var protoDefinitionsByNumber = BuildProtoDefinitionsByNumber(assets.Entries);
        var scriptDefinitionsById = BuildScriptDefinitionsById(assets.Entries);
        var dialogDefinitionsById = BuildDialogDefinitionsById(assets.Entries);
        var scriptDetailsById = BuildScriptDetailsById(gameData, assetsByPath);
        var dialogDetailsById = BuildDialogDetailsById(gameData, assetsByPath);
        var protoReferencesByNumber = BuildProtoReferencesByNumber(gameData, assetsByPath);
        var scriptReferencesById = BuildScriptReferencesById(gameData, assetsByPath);
        var artReferencesById = BuildArtReferencesById(gameData, assetsByPath);

        var index = EditorAssetIndex.Create(
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
        var validation = BuildValidationReport(
            protoDefinitionsByNumber,
            scriptDefinitionsById,
            scriptDetailsById,
            dialogDetailsById,
            protoReferencesByNumber,
            scriptReferencesById,
            protoDisplayNameMessageIndices,
            installationType
        );

        return (index, validation);
    }

    private static (
        IReadOnlyList<string> MapNames,
        IReadOnlyDictionary<string, IReadOnlyList<EditorAssetEntry>> AssetsByMap,
        IReadOnlyDictionary<string, string> MapByAssetPath
    ) BuildMapAssets(IReadOnlyList<EditorAssetEntry> assets)
    {
        var assetsByMap = new Dictionary<string, List<EditorAssetEntry>>(StringComparer.OrdinalIgnoreCase);
        var mapByAssetPath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var asset in assets)
        {
            if (!TryGetMapNameFromAssetPath(asset.AssetPath, out var mapName))
                continue;

            mapByAssetPath[asset.AssetPath] = mapName;

            if (!assetsByMap.TryGetValue(mapName, out var mapAssets))
            {
                mapAssets = [];
                assetsByMap[mapName] = mapAssets;
            }

            mapAssets.Add(asset);
        }

        var orderedMapNames = assetsByMap.Keys.OrderBy(static name => name, StringComparer.OrdinalIgnoreCase).ToArray();
        var orderedAssetsByMap = assetsByMap.ToDictionary(
            pair => pair.Key,
            pair =>
                (IReadOnlyList<EditorAssetEntry>)
                    pair.Value.OrderBy(static asset => asset.AssetPath, StringComparer.OrdinalIgnoreCase).ToArray(),
            StringComparer.OrdinalIgnoreCase
        );

        return (orderedMapNames, orderedAssetsByMap, mapByAssetPath);
    }

    private static IReadOnlyDictionary<int, IReadOnlyList<EditorAssetEntry>> BuildMessageAssetsByIndex(
        GameDataStore gameData,
        IReadOnlyDictionary<string, EditorAssetEntry> assetsByPath
    )
    {
        var messageAssetsByIndex = new Dictionary<int, List<EditorAssetEntry>>();

        foreach (var (assetPath, entries) in gameData.MessagesBySource)
        {
            if (!assetsByPath.TryGetValue(assetPath, out var asset))
                continue;

            foreach (var entry in entries)
            {
                if (!messageAssetsByIndex.TryGetValue(entry.Index, out var assets))
                {
                    assets = [];
                    messageAssetsByIndex[entry.Index] = assets;
                }

                if (!assets.Contains(asset))
                    assets.Add(asset);
            }
        }

        return messageAssetsByIndex.ToDictionary(
            pair => pair.Key,
            pair =>
                (IReadOnlyList<EditorAssetEntry>)
                    pair.Value.OrderBy(static asset => asset.AssetPath, StringComparer.OrdinalIgnoreCase).ToArray()
        );
    }

    private static IReadOnlySet<int> BuildProtoDisplayNameMessageIndices(GameDataStore gameData)
    {
        var indices = new HashSet<int>();

        foreach (var (assetPath, entries) in gameData.MessagesBySource)
        {
            if (!IsProtoDisplayNameAssetPath(assetPath))
                continue;

            foreach (var entry in entries)
                indices.Add(entry.Index);
        }

        return indices;
    }

    private static IReadOnlyDictionary<int, EditorAssetEntry> BuildProtoDefinitionsByNumber(
        IReadOnlyList<EditorAssetEntry> assets
    )
    {
        var protoDefinitionsByNumber = new Dictionary<int, EditorAssetEntry>();

        foreach (var asset in assets)
        {
            if (!TryGetProtoNumberFromAssetPath(asset.AssetPath, out var protoNumber))
                continue;

            protoDefinitionsByNumber[protoNumber] = asset;
        }

        return protoDefinitionsByNumber;
    }

    private static IReadOnlyDictionary<int, IReadOnlyList<EditorAssetEntry>> BuildScriptDefinitionsById(
        IReadOnlyList<EditorAssetEntry> assets
    ) => BuildDefinitionsById(assets, TryGetScriptIdFromAssetPath);

    private static IReadOnlyDictionary<int, IReadOnlyList<EditorAssetEntry>> BuildDialogDefinitionsById(
        IReadOnlyList<EditorAssetEntry> assets
    ) => BuildDefinitionsById(assets, TryGetDialogIdFromAssetPath);

    private static IReadOnlyDictionary<int, IReadOnlyList<EditorAssetEntry>> BuildDefinitionsById(
        IReadOnlyList<EditorAssetEntry> assets,
        TryGetAssetId tryGetAssetId
    )
    {
        var definitionsById = new Dictionary<int, List<EditorAssetEntry>>();

        foreach (var asset in assets)
        {
            if (!tryGetAssetId(asset.AssetPath, out var assetId))
                continue;

            if (!definitionsById.TryGetValue(assetId, out var definitions))
            {
                definitions = [];
                definitionsById[assetId] = definitions;
            }

            definitions.Add(asset);
        }

        return definitionsById.ToDictionary(
            pair => pair.Key,
            pair =>
                (IReadOnlyList<EditorAssetEntry>)
                    pair.Value.OrderBy(static asset => asset.AssetPath, StringComparer.OrdinalIgnoreCase).ToArray()
        );
    }

    private static IReadOnlyDictionary<int, IReadOnlyList<EditorScriptDefinition>> BuildScriptDetailsById(
        GameDataStore gameData,
        IReadOnlyDictionary<string, EditorAssetEntry> assetsByPath
    )
    {
        var definitionsById = new Dictionary<int, List<EditorScriptDefinition>>();

        foreach (var (assetPath, scripts) in gameData.ScriptsBySource)
        {
            if (
                !assetsByPath.TryGetValue(assetPath, out var asset)
                || !TryGetScriptIdFromAssetPath(assetPath, out var scriptId)
            )
                continue;

            foreach (var script in scripts)
            {
                var activeAttachmentSlots = GetActiveAttachmentSlots(script).ToArray();
                var activeAttachmentPoints = activeAttachmentSlots
                    .Where(static slot => Enum.IsDefined((ScriptAttachmentPoint)slot))
                    .Select(static slot => (ScriptAttachmentPoint)slot)
                    .ToArray();

                AddDefinition(
                    definitionsById,
                    scriptId,
                    new EditorScriptDefinition
                    {
                        Asset = asset,
                        ScriptId = scriptId,
                        Description = script.Description,
                        Flags = script.Flags,
                        EntryCount = script.Entries.Count,
                        ActiveAttachmentCount = activeAttachmentSlots.Length,
                        ActiveAttachmentSlots = activeAttachmentSlots,
                        ActiveAttachmentPoints = activeAttachmentPoints,
                    }
                );
            }
        }

        return OrderDefinitions(definitionsById);
    }

    private static IReadOnlyDictionary<int, IReadOnlyList<EditorDialogDefinition>> BuildDialogDetailsById(
        GameDataStore gameData,
        IReadOnlyDictionary<string, EditorAssetEntry> assetsByPath
    )
    {
        var definitionsById = new Dictionary<int, List<EditorDialogDefinition>>();

        foreach (var (assetPath, dialogs) in gameData.DialogsBySource)
        {
            if (
                !assetsByPath.TryGetValue(assetPath, out var asset)
                || !TryGetDialogIdFromAssetPath(assetPath, out var dialogId)
            )
                continue;

            foreach (var dialog in dialogs)
            {
                var entryNumbers = dialog.Entries.Select(static entry => entry.Num).ToHashSet();
                var inboundTargets = dialog
                    .Entries.Where(static entry => entry.ResponseVal > 0)
                    .Select(static entry => entry.ResponseVal)
                    .ToHashSet();
                var roots = dialog
                    .Entries.Where(entry => !inboundTargets.Contains(entry.Num))
                    .Select(static entry => entry.Num)
                    .OrderBy(static value => value)
                    .ToArray();
                var missingTargets = dialog
                    .Entries.Where(static entry => entry.ResponseVal > 0)
                    .Select(static entry => entry.ResponseVal)
                    .Where(target => !entryNumbers.Contains(target))
                    .Distinct()
                    .OrderBy(static value => value)
                    .ToArray();

                AddDefinition(
                    definitionsById,
                    dialogId,
                    new EditorDialogDefinition
                    {
                        Asset = asset,
                        DialogId = dialogId,
                        EntryCount = dialog.Entries.Count,
                        NpcEntryCount = dialog.Entries.Count(static entry => entry.Iq == 0),
                        PcOptionCount = dialog.Entries.Count(static entry => entry.Iq != 0),
                        ControlEntryCount = dialog.Entries.Count(IsControlEntry),
                        TransitionCount = dialog.Entries.Count(static entry => entry.ResponseVal > 0),
                        TerminalEntryCount = dialog.Entries.Count(static entry => entry.ResponseVal == 0),
                        RootEntryNumbers = roots,
                        MissingResponseTargetNumbers = missingTargets,
                    }
                );
            }
        }

        return OrderDefinitions(definitionsById);
    }

    private static void AddDefinition<T>(Dictionary<int, List<T>> definitionsById, int id, T definition)
    {
        if (!definitionsById.TryGetValue(id, out var definitions))
        {
            definitions = [];
            definitionsById[id] = definitions;
        }

        definitions.Add(definition);
    }

    private static IReadOnlyDictionary<int, IReadOnlyList<T>> OrderDefinitions<T>(
        Dictionary<int, List<T>> definitionsById
    )
        where T : class
    {
        return definitionsById.ToDictionary(
            pair => pair.Key,
            pair =>
                (IReadOnlyList<T>)
                    pair
                        .Value.OrderBy(
                            static definition =>
                                definition switch
                                {
                                    EditorScriptDefinition scriptDefinition => scriptDefinition.Asset.AssetPath,
                                    EditorDialogDefinition dialogDefinition => dialogDefinition.Asset.AssetPath,
                                    _ => string.Empty,
                                },
                            StringComparer.OrdinalIgnoreCase
                        )
                        .ToArray()
        );
    }

    private static IReadOnlyDictionary<int, IReadOnlyList<EditorProtoReference>> BuildProtoReferencesByNumber(
        GameDataStore gameData,
        IReadOnlyDictionary<string, EditorAssetEntry> assetsByPath
    )
    {
        var protoReferencesByNumber = new Dictionary<int, List<EditorProtoReference>>();

        foreach (var (assetPath, mobs) in gameData.MobsBySource)
            AddProtoReferences(protoReferencesByNumber, assetsByPath, assetPath, CountMobProtoReferences(mobs));

        foreach (var (assetPath, sectors) in gameData.SectorsBySource)
            AddProtoReferences(protoReferencesByNumber, assetsByPath, assetPath, CountSectorProtoReferences(sectors));

        return protoReferencesByNumber.ToDictionary(
            pair => pair.Key,
            pair =>
                (IReadOnlyList<EditorProtoReference>)
                    pair
                        .Value.OrderBy(static reference => reference.Asset.AssetPath, StringComparer.OrdinalIgnoreCase)
                        .ToArray()
        );
    }

    private static IReadOnlyDictionary<int, IReadOnlyList<EditorScriptReference>> BuildScriptReferencesById(
        GameDataStore gameData,
        IReadOnlyDictionary<string, EditorAssetEntry> assetsByPath
    )
    {
        var scriptReferencesById = new Dictionary<int, List<EditorScriptReference>>();

        foreach (var (assetPath, mobs) in gameData.MobsBySource)
            AddScriptReferences(scriptReferencesById, assetsByPath, assetPath, CountMobScriptReferences(mobs));

        foreach (var (assetPath, protos) in gameData.ProtosBySource)
            AddScriptReferences(scriptReferencesById, assetsByPath, assetPath, CountProtoScriptReferences(protos));

        foreach (var (assetPath, sectors) in gameData.SectorsBySource)
            AddScriptReferences(scriptReferencesById, assetsByPath, assetPath, CountSectorScriptReferences(sectors));

        return scriptReferencesById.ToDictionary(
            pair => pair.Key,
            pair =>
                (IReadOnlyList<EditorScriptReference>)
                    pair
                        .Value.OrderBy(static reference => reference.Asset.AssetPath, StringComparer.OrdinalIgnoreCase)
                        .ToArray()
        );
    }

    private static IReadOnlyDictionary<uint, IReadOnlyList<EditorArtReference>> BuildArtReferencesById(
        GameDataStore gameData,
        IReadOnlyDictionary<string, EditorAssetEntry> assetsByPath
    )
    {
        var artReferencesById = new Dictionary<uint, List<EditorArtReference>>();

        foreach (var (assetPath, mobs) in gameData.MobsBySource)
            AddArtReferences(artReferencesById, assetsByPath, assetPath, CountMobArtReferences(mobs));

        foreach (var (assetPath, protos) in gameData.ProtosBySource)
            AddArtReferences(artReferencesById, assetsByPath, assetPath, CountProtoArtReferences(protos));

        foreach (var (assetPath, sectors) in gameData.SectorsBySource)
            AddArtReferences(artReferencesById, assetsByPath, assetPath, CountSectorArtReferences(sectors));

        return artReferencesById.ToDictionary(
            pair => pair.Key,
            pair =>
                (IReadOnlyList<EditorArtReference>)
                    pair
                        .Value.OrderBy(static reference => reference.Asset.AssetPath, StringComparer.OrdinalIgnoreCase)
                        .ToArray()
        );
    }

    private static EditorWorkspaceValidationReport BuildValidationReport(
        IReadOnlyDictionary<int, EditorAssetEntry> protoDefinitionsByNumber,
        IReadOnlyDictionary<int, IReadOnlyList<EditorAssetEntry>> scriptDefinitionsById,
        IReadOnlyDictionary<int, IReadOnlyList<EditorScriptDefinition>> scriptDetailsById,
        IReadOnlyDictionary<int, IReadOnlyList<EditorDialogDefinition>> dialogDetailsById,
        IReadOnlyDictionary<int, IReadOnlyList<EditorProtoReference>> protoReferencesByNumber,
        IReadOnlyDictionary<int, IReadOnlyList<EditorScriptReference>> scriptReferencesById,
        IReadOnlySet<int> protoDisplayNameMessageIndices,
        ArcanumInstallationType? installationType
    )
    {
        var issues = new List<EditorWorkspaceValidationIssue>();

        foreach (var (protoNumber, references) in protoReferencesByNumber.OrderBy(static pair => pair.Key))
        {
            if (protoDefinitionsByNumber.ContainsKey(protoNumber))
                continue;

            foreach (var reference in references)
            {
                issues.Add(
                    Error(
                        reference.Asset.AssetPath,
                        $"References proto {protoNumber} {reference.Count} time(s), but no matching proto asset was indexed."
                    )
                );
            }
        }

        if (installationType.HasValue && protoDisplayNameMessageIndices.Count > 0)
        {
            foreach (var pair in protoDefinitionsByNumber.OrderBy(static pair => pair.Key))
            {
                if (HasProtoDisplayName(pair.Key, protoDisplayNameMessageIndices, installationType.Value))
                    continue;

                issues.Add(
                    Warning(
                        pair.Value.AssetPath,
                        $"Proto {pair.Key} has no display-name entry in {DescriptionMesAssetPath} or {ProtoNameOverrideAssetPath} for {FormatProtoDisplayNameLookup(pair.Key, installationType.Value)}."
                    )
                );
            }
        }

        foreach (var (scriptId, references) in scriptReferencesById.OrderBy(static pair => pair.Key))
        {
            if (scriptDefinitionsById.ContainsKey(scriptId))
                continue;

            foreach (var reference in references)
            {
                issues.Add(
                    Warning(
                        reference.Asset.AssetPath,
                        $"References script {scriptId} {reference.Count} time(s), but no matching script asset was indexed."
                    )
                );
            }
        }

        foreach (var (_, definitions) in scriptDetailsById.OrderBy(static pair => pair.Key))
        {
            foreach (var definition in definitions)
            {
                if (!definition.HasUnknownAttachmentSlots)
                    continue;

                var unknownSlots = definition
                    .ActiveAttachmentSlots.Where(static slot => !Enum.IsDefined((ScriptAttachmentPoint)slot))
                    .OrderBy(static slot => slot)
                    .ToArray();

                issues.Add(
                    Info(
                        definition.Asset.AssetPath,
                        $"Script {definition.ScriptId} uses non-empty attachment slot(s) that ArcNET does not name yet: {string.Join(", ", unknownSlots)}."
                    )
                );
            }
        }

        foreach (var (_, definitions) in dialogDetailsById.OrderBy(static pair => pair.Key))
        {
            foreach (var definition in definitions)
            {
                if (!definition.HasMissingResponseTargets)
                    continue;

                issues.Add(
                    Warning(
                        definition.Asset.AssetPath,
                        $"Dialog {definition.DialogId} references missing response target(s): {string.Join(", ", definition.MissingResponseTargetNumbers)}."
                    )
                );
            }
        }

        return issues.Count == 0 ? EditorWorkspaceValidationReport.Empty : new() { Issues = [.. issues] };
    }

    private static Dictionary<int, int> CountMobProtoReferences(IReadOnlyList<MobData> mobs)
    {
        var counts = new Dictionary<int, int>();

        foreach (var mob in mobs)
        {
            var protoNumber = mob.Header.ProtoId.GetProtoNumber();
            if (!protoNumber.HasValue)
                continue;

            counts[protoNumber.Value] = counts.GetValueOrDefault(protoNumber.Value) + 1;
        }

        return counts;
    }

    private static Dictionary<int, int> CountMobScriptReferences(IReadOnlyList<MobData> mobs)
    {
        var counts = new Dictionary<int, int>();

        foreach (var mob in mobs)
            AddObjectScriptReferences(counts, mob.Properties);

        return counts;
    }

    private static Dictionary<uint, int> CountMobArtReferences(IReadOnlyList<MobData> mobs)
    {
        var counts = new Dictionary<uint, int>();

        foreach (var mob in mobs)
            AddObjectArtReferences(counts, mob.Properties);

        return counts;
    }

    private static Dictionary<int, int> CountProtoScriptReferences(IReadOnlyList<ProtoData> protos)
    {
        var counts = new Dictionary<int, int>();

        foreach (var proto in protos)
            AddObjectScriptReferences(counts, proto.Properties);

        return counts;
    }

    private static Dictionary<uint, int> CountProtoArtReferences(IReadOnlyList<ProtoData> protos)
    {
        var counts = new Dictionary<uint, int>();

        foreach (var proto in protos)
            AddObjectArtReferences(counts, proto.Properties);

        return counts;
    }

    private static Dictionary<int, int> CountSectorProtoReferences(IReadOnlyList<Sector> sectors)
    {
        var counts = new Dictionary<int, int>();

        foreach (var sector in sectors)
        foreach (var mob in sector.Objects)
        {
            var protoNumber = mob.Header.ProtoId.GetProtoNumber();
            if (!protoNumber.HasValue)
                continue;

            counts[protoNumber.Value] = counts.GetValueOrDefault(protoNumber.Value) + 1;
        }

        return counts;
    }

    private static Dictionary<int, int> CountSectorScriptReferences(IReadOnlyList<Sector> sectors)
    {
        var counts = new Dictionary<int, int>();

        foreach (var sector in sectors)
        {
            if (sector.SectorScript is { } sectorScript && !sectorScript.IsEmpty)
                AddCount(counts, sectorScript.ScriptId);

            foreach (var tileScript in sector.TileScripts)
            {
                if (tileScript.ScriptNum == 0)
                    continue;

                AddCount(counts, tileScript.ScriptNum);
            }

            foreach (var mob in sector.Objects)
                AddObjectScriptReferences(counts, mob.Properties);
        }

        return counts;
    }

    private static Dictionary<uint, int> CountSectorArtReferences(IReadOnlyList<Sector> sectors)
    {
        var counts = new Dictionary<uint, int>();

        foreach (var sector in sectors)
        {
            foreach (var light in sector.Lights)
                AddNonZeroCount(counts, light.ArtId);

            foreach (var tileArtId in sector.Tiles)
                AddNonZeroCount(counts, tileArtId);

            if (sector.Roofs is not null)
            {
                foreach (var roofArtId in sector.Roofs)
                    AddNonZeroCount(counts, roofArtId);
            }

            foreach (var mob in sector.Objects)
                AddObjectArtReferences(counts, mob.Properties);
        }

        return counts;
    }

    private static void AddProtoReferences(
        Dictionary<int, List<EditorProtoReference>> protoReferencesByNumber,
        IReadOnlyDictionary<string, EditorAssetEntry> assetsByPath,
        string assetPath,
        IReadOnlyDictionary<int, int> countsByProtoNumber
    )
    {
        if (!assetsByPath.TryGetValue(assetPath, out var asset))
            return;

        foreach (var (protoNumber, count) in countsByProtoNumber)
        {
            if (!protoReferencesByNumber.TryGetValue(protoNumber, out var references))
            {
                references = [];
                protoReferencesByNumber[protoNumber] = references;
            }

            references.Add(
                new EditorProtoReference
                {
                    Asset = asset,
                    ProtoNumber = protoNumber,
                    Count = count,
                }
            );
        }
    }

    private static void AddScriptReferences(
        Dictionary<int, List<EditorScriptReference>> scriptReferencesById,
        IReadOnlyDictionary<string, EditorAssetEntry> assetsByPath,
        string assetPath,
        IReadOnlyDictionary<int, int> countsByScriptId
    )
    {
        if (!assetsByPath.TryGetValue(assetPath, out var asset))
            return;

        foreach (var (scriptId, count) in countsByScriptId)
        {
            if (!scriptReferencesById.TryGetValue(scriptId, out var references))
            {
                references = [];
                scriptReferencesById[scriptId] = references;
            }

            references.Add(
                new EditorScriptReference
                {
                    Asset = asset,
                    ScriptId = scriptId,
                    Count = count,
                }
            );
        }
    }

    private static void AddArtReferences(
        Dictionary<uint, List<EditorArtReference>> artReferencesById,
        IReadOnlyDictionary<string, EditorAssetEntry> assetsByPath,
        string assetPath,
        IReadOnlyDictionary<uint, int> countsByArtId
    )
    {
        if (!assetsByPath.TryGetValue(assetPath, out var asset))
            return;

        foreach (var (artId, count) in countsByArtId)
        {
            if (!artReferencesById.TryGetValue(artId, out var references))
            {
                references = [];
                artReferencesById[artId] = references;
            }

            references.Add(
                new EditorArtReference
                {
                    Asset = asset,
                    ArtId = artId,
                    Count = count,
                }
            );
        }
    }

    private static void AddObjectScriptReferences(Dictionary<int, int> counts, IReadOnlyList<ObjectProperty> properties)
    {
        foreach (var property in properties)
        {
            if (property.Field != ObjectField.ObjFScriptsIdx)
                continue;

            if (!TryGetScriptArray(property, out var scripts))
                continue;

            foreach (var script in scripts)
            {
                if (script.ScriptId == 0)
                    continue;

                AddCount(counts, script.ScriptId);
            }
        }
    }

    private static void AddObjectArtReferences(Dictionary<uint, int> counts, IReadOnlyList<ObjectProperty> properties)
    {
        foreach (var property in properties)
        {
            switch (property.Field)
            {
                case ObjectField.ObjFCurrentAid:
                case ObjectField.ObjFShadow:
                case ObjectField.ObjFLightAid:
                case ObjectField.ObjFAid:
                case ObjectField.ObjFDestroyedAid:
                    if (TryGetArtId(property, out var artId))
                        AddNonZeroCount(counts, artId);
                    break;
            }
        }
    }

    private static bool TryGetScriptArray(ObjectProperty property, out ObjectPropertyScript[] scripts)
    {
        try
        {
            scripts = property.GetScriptArray();
            return true;
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException)
        {
            scripts = [];
            return false;
        }
    }

    private static bool TryGetArtId(ObjectProperty property, out uint artId)
    {
        try
        {
            artId = unchecked((uint)property.GetInt32());
            return true;
        }
        catch (InvalidOperationException)
        {
            artId = 0;
            return false;
        }
    }

    private static bool HasProtoDisplayName(
        int protoNumber,
        IReadOnlySet<int> protoDisplayNameMessageIndices,
        ArcanumInstallationType installationType
    )
    {
        var translatedKey = ArcanumInstallation.ToVanillaProtoId(protoNumber, installationType);
        if (translatedKey > 0 && protoDisplayNameMessageIndices.Contains(translatedKey))
            return true;

        return translatedKey != protoNumber && protoDisplayNameMessageIndices.Contains(protoNumber);
    }

    private static string FormatProtoDisplayNameLookup(int protoNumber, ArcanumInstallationType installationType)
    {
        var translatedKey = ArcanumInstallation.ToVanillaProtoId(protoNumber, installationType);
        return translatedKey > 0 && translatedKey != protoNumber
            ? $"lookup key {translatedKey} or raw fallback {protoNumber}"
            : $"lookup key {protoNumber}";
    }

    private static bool IsProtoDisplayNameAssetPath(string assetPath) =>
        assetPath.Equals(DescriptionMesAssetPath, StringComparison.OrdinalIgnoreCase)
        || assetPath.Equals(ProtoNameOverrideAssetPath, StringComparison.OrdinalIgnoreCase);

    private static void AddCount(Dictionary<int, int> counts, int key) =>
        counts[key] = counts.GetValueOrDefault(key) + 1;

    private static void AddNonZeroCount(Dictionary<uint, int> counts, uint key)
    {
        if (key == 0)
            return;

        counts[key] = counts.GetValueOrDefault(key) + 1;
    }

    private static IEnumerable<int> GetActiveAttachmentSlots(ScrFile script)
    {
        for (var i = 0; i < script.Entries.Count; i++)
        {
            var entry = script.Entries[i];
            if (IsEmptyCondition(entry) && IsEmptyAction(entry.Action) && IsEmptyAction(entry.Else))
                continue;

            yield return i;
        }
    }

    private static bool IsEmptyCondition(ScriptConditionData condition)
    {
        if (condition.Type != (int)ScriptConditionType.True)
            return false;

        var opTypes = condition.OpTypes;
        var opValues = condition.OpValues;
        for (var i = 0; i < 8; i++)
        {
            if (opTypes[i] != 0 || opValues[i] != 0)
                return false;
        }

        return true;
    }

    private static bool IsEmptyAction(ScriptActionData action)
    {
        if (action.Type != (int)ScriptActionType.DoNothing)
            return false;

        var opTypes = action.OpTypes;
        var opValues = action.OpValues;
        for (var i = 0; i < 8; i++)
        {
            if (opTypes[i] != 0 || opValues[i] != 0)
                return false;
        }

        return true;
    }

    private static bool IsControlEntry(DialogEntry entry)
    {
        return entry.Text switch
        {
            "E:" => true,
            "F:" => true,
            _ when entry.Text.StartsWith("R:", StringComparison.Ordinal) => true,
            _ when entry.Text.StartsWith("C:", StringComparison.Ordinal) => true,
            _ when entry.Text.StartsWith("T:", StringComparison.Ordinal) => true,
            _ => false,
        };
    }

    private static bool TryGetProtoNumberFromAssetPath(string assetPath, out int protoNumber)
    {
        return TryGetAssetNumberFromPath(assetPath, s_protoAssetPathPattern, out protoNumber);
    }

    private static bool TryGetScriptIdFromAssetPath(string assetPath, out int scriptId)
    {
        return TryGetAssetNumberFromPath(assetPath, s_scriptAssetPathPattern, out scriptId);
    }

    private static bool TryGetDialogIdFromAssetPath(string assetPath, out int dialogId)
    {
        return TryGetAssetNumberFromPath(assetPath, s_dialogAssetPathPattern, out dialogId);
    }

    private static bool TryGetAssetNumberFromPath(string assetPath, Regex pattern, out int number)
    {
        var match = pattern.Match(assetPath);
        if (!match.Success || !int.TryParse(match.Groups["number"].Value, out number))
        {
            number = 0;
            return false;
        }

        return true;
    }

    private static bool TryGetMapNameFromAssetPath(string assetPath, out string mapName)
    {
        const string mapsPrefix = "maps/";
        if (!assetPath.StartsWith(mapsPrefix, StringComparison.OrdinalIgnoreCase))
        {
            mapName = string.Empty;
            return false;
        }

        var separatorIndex = assetPath.IndexOf('/', mapsPrefix.Length);
        if (separatorIndex < 0)
        {
            mapName = string.Empty;
            return false;
        }

        mapName = assetPath[mapsPrefix.Length..separatorIndex];
        return mapName.Length > 0;
    }

    [GeneratedRegex(@"(?:^|/)(?<number>\d+)(?:\s*-.*)?\.pro$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ProtoAssetPathPattern();

    [GeneratedRegex(@"(?:^|/)(?<number>\d+)[^/]*\.scr$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ScriptAssetPathPattern();

    [GeneratedRegex(@"(?:^|/)(?<number>\d+)[^/]*\.dlg$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DialogAssetPathPattern();

    private delegate bool TryGetAssetId(string assetPath, out int assetId);
}
