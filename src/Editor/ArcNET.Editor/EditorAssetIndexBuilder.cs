using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using ArcNET.Core;
using ArcNET.Formats;
using ArcNET.GameData;
using static ArcNET.Editor.EditorWorkspaceValidationIssue;

namespace ArcNET.Editor;

internal static partial class EditorAssetIndexBuilder
{
    private static readonly Regex s_protoAssetPathPattern = ProtoAssetPathPattern();
    private static readonly Regex s_scriptAssetPathPattern = ScriptAssetPathPattern();
    private static readonly Regex s_dialogAssetPathPattern = DialogAssetPathPattern();

    public static (EditorAssetIndex Index, EditorWorkspaceValidationReport Validation) Create(
        GameDataStore gameData,
        EditorAssetCatalog assets,
        ArcanumInstallationType? installationType = null,
        IProgress<EditorAssetIndexBuildProgress>? progress = null
    )
    {
        ArgumentNullException.ThrowIfNull(gameData);
        ArgumentNullException.ThrowIfNull(assets);

        const int TotalPhases = 13;
        var progressGate = new object();
        var completedPhases = 0;
        void Report(string activity)
        {
            lock (progressGate)
            {
                progress?.Report(
                    new EditorAssetIndexBuildProgress(
                        activity,
                        completedPhases / (float)TotalPhases,
                        completedPhases,
                        TotalPhases
                    )
                );
            }
        }

        void CompletePhase()
        {
            lock (progressGate)
                completedPhases++;
        }

        Report("Indexing asset paths");
        var assetsByPath = assets.Entries.ToDictionary(entry => entry.AssetPath, StringComparer.OrdinalIgnoreCase);
        CompletePhase();

        Report("Indexing map assets");
        var (mapNames, mapAssetsByName, mapNameByAssetPath) = BuildMapAssets(assets.Entries);
        CompletePhase();

        IReadOnlyDictionary<string, IReadOnlyList<EditorSectorSummary>>? mapSectorsByName = null;
        IReadOnlyDictionary<string, EditorSectorSummary>? sectorSummariesByAssetPath = null;
        IReadOnlyList<EditorSectorSummary>? sectorSummaries = null;
        IReadOnlyDictionary<int, IReadOnlyList<EditorAssetEntry>>? messageAssetsByIndex = null;
        IReadOnlySet<int>? protoDisplayNameMessageIndices = null;
        IReadOnlyDictionary<int, EditorAssetEntry>? protoDefinitionsByNumber = null;
        IReadOnlyDictionary<int, IReadOnlyList<EditorAssetEntry>>? scriptDefinitionsById = null;
        IReadOnlyDictionary<int, IReadOnlyList<EditorAssetEntry>>? dialogDefinitionsById = null;
        IReadOnlyDictionary<int, IReadOnlyList<EditorScriptDefinition>>? scriptDetailsById = null;
        IReadOnlyDictionary<int, IReadOnlyList<EditorDialogDefinition>>? dialogDetailsById = null;
        IReadOnlyDictionary<string, EditorArtDefinition>? artDetailsByAssetPath = null;
        IReadOnlyDictionary<string, EditorJumpDefinition>? jumpDetailsByAssetPath = null;
        IReadOnlyDictionary<string, EditorMapPropertiesDefinition>? mapPropertiesDetailsByAssetPath = null;
        IReadOnlyDictionary<string, EditorTerrainDefinition>? terrainDetailsByAssetPath = null;
        IReadOnlyDictionary<string, EditorFacadeWalkDefinition>? facadeWalkDetailsByAssetPath = null;
        IReadOnlyDictionary<int, IReadOnlyList<EditorProtoReference>>? protoReferencesByNumber = null;
        IReadOnlyDictionary<string, IReadOnlyList<EditorProtoReference>>? protoReferencesByAssetPath = null;
        IReadOnlyDictionary<int, IReadOnlyList<EditorScriptReference>>? scriptReferencesById = null;
        IReadOnlyDictionary<string, IReadOnlyList<EditorScriptReference>>? scriptReferencesByAssetPath = null;

        RunIndexBuildPhases(
            new IndexBuildPhase(
                "Summarizing sectors",
                () =>
                {
                    Report("Summarizing sectors");
                    (mapSectorsByName, sectorSummariesByAssetPath) = BuildSectorSummaries(
                        gameData,
                        assetsByPath,
                        mapNameByAssetPath
                    );
                    sectorSummaries = sectorSummariesByAssetPath.Values.ToArray();
                    CompletePhase();
                }
            ),
            new IndexBuildPhase(
                "Indexing message assets",
                () =>
                {
                    Report("Indexing message assets");
                    messageAssetsByIndex = BuildMessageAssetsByIndex(gameData, assetsByPath);
                    protoDisplayNameMessageIndices = BuildProtoDisplayNameMessageIndices(gameData);
                    CompletePhase();
                }
            ),
            new IndexBuildPhase(
                "Indexing asset definitions",
                () =>
                {
                    Report("Indexing asset definitions");
                    protoDefinitionsByNumber = BuildProtoDefinitionsByNumber(assets.Entries);
                    scriptDefinitionsById = BuildScriptDefinitionsById(assets.Entries);
                    dialogDefinitionsById = BuildDialogDefinitionsById(assets.Entries);
                    CompletePhase();
                }
            ),
            new IndexBuildPhase(
                "Indexing script details",
                () =>
                {
                    Report("Indexing script details");
                    scriptDetailsById = BuildScriptDetailsById(gameData, assetsByPath);
                    CompletePhase();
                }
            ),
            new IndexBuildPhase(
                "Indexing dialog details",
                () =>
                {
                    Report("Indexing dialog details");
                    dialogDetailsById = BuildDialogDetailsById(gameData, assetsByPath);
                    CompletePhase();
                }
            ),
            new IndexBuildPhase(
                "Indexing art and map details",
                () =>
                {
                    Report("Indexing art and map details");
                    artDetailsByAssetPath = BuildArtDetailsByAssetPath(gameData, assetsByPath);
                    jumpDetailsByAssetPath = BuildJumpDetailsByAssetPath(gameData, assetsByPath);
                    mapPropertiesDetailsByAssetPath = BuildMapPropertiesDetailsByAssetPath(gameData, assetsByPath);
                    terrainDetailsByAssetPath = BuildTerrainDetailsByAssetPath(gameData, assetsByPath);
                    facadeWalkDetailsByAssetPath = BuildFacadeWalkDetailsByAssetPath(gameData, assetsByPath);
                    CompletePhase();
                }
            ),
            new IndexBuildPhase(
                "Counting asset references",
                () =>
                {
                    Report("Counting asset references");
                    EditorAssetReferenceCounter.CountReferences(
                        gameData,
                        assetsByPath,
                        out var protoRefsByNumber,
                        out var protoRefsByAssetPath,
                        out var scriptRefsById,
                        out var scriptRefsByAssetPath,
                        out _,
                        out _,
                        includeArtReferences: false
                    );
                    protoReferencesByNumber = protoRefsByNumber;
                    protoReferencesByAssetPath = protoRefsByAssetPath;
                    scriptReferencesById = scriptRefsById;
                    scriptReferencesByAssetPath = scriptRefsByAssetPath;
                    CompletePhase();
                }
            )
        );

        var artReferenceIndexes = CreateLazyArtReferenceIndexes(gameData, assetsByPath);
        var artReferencesById = new Lazy<IReadOnlyDictionary<uint, IReadOnlyList<EditorArtReference>>>(
            () => artReferenceIndexes.Value.ById,
            LazyThreadSafetyMode.ExecutionAndPublication
        );

        IReadOnlyDictionary<int, IReadOnlyList<EditorSectorSummary>>? lightSchemeSectorsByIndex = null;
        IReadOnlyDictionary<int, IReadOnlyList<EditorSectorSummary>>? musicSchemeSectorsByIndex = null;
        IReadOnlyDictionary<int, IReadOnlyList<EditorSectorSummary>>? ambientSchemeSectorsByIndex = null;
        IReadOnlyDictionary<string, EditorMapProjection>? mapProjectionsByName = null;

        RunIndexBuildPhases(
            new IndexBuildPhase(
                "Building scheme lookups",
                () =>
                {
                    Report("Building scheme lookups");
                    (lightSchemeSectorsByIndex, musicSchemeSectorsByIndex, ambientSchemeSectorsByIndex) =
                        BuildSectorSchemeLookups(sectorSummaries!);
                    CompletePhase();
                }
            ),
            new IndexBuildPhase(
                "Projecting map sectors",
                () =>
                {
                    Report("Projecting map sectors");
                    mapProjectionsByName = EditorSectorProjectionBuilder.Build(mapSectorsByName!);
                    CompletePhase();
                }
            )
        );

        Lazy<IReadOnlyDictionary<string, EditorAssetDependencySummary>>? assetDependencySummariesByAssetPath = null;
        EditorWorkspaceValidationReport? validation = null;

        RunIndexBuildPhases(
            new IndexBuildPhase(
                "Building dependency summaries",
                () =>
                {
                    Report("Building dependency summaries");
                    assetDependencySummariesByAssetPath = new Lazy<
                        IReadOnlyDictionary<string, EditorAssetDependencySummary>
                    >(
                        () =>
                        {
                            var artReferences = artReferenceIndexes.Value;
                            return BuildAssetDependencySummaries(
                                assets.Entries,
                                mapNameByAssetPath,
                                protoReferencesByNumber!,
                                protoReferencesByAssetPath!,
                                scriptReferencesById!,
                                scriptReferencesByAssetPath!,
                                artReferences.ById,
                                artReferences.ByAssetPath
                            );
                        },
                        LazyThreadSafetyMode.ExecutionAndPublication
                    );
                    CompletePhase();
                }
            ),
            new IndexBuildPhase(
                "Validating workspace",
                () =>
                {
                    Report("Validating workspace");
                    validation = new EditorWorkspaceValidator().Build(
                        protoDefinitionsByNumber!,
                        scriptDefinitionsById!,
                        scriptDetailsById!,
                        dialogDetailsById!,
                        protoReferencesByNumber!,
                        scriptReferencesById!,
                        protoDisplayNameMessageIndices!,
                        installationType
                    );
                    CompletePhase();
                }
            )
        );

        var index = EditorAssetIndex.Create(
            new EditorAssetIndexData
            {
                MapNames = mapNames,
                MapAssetsByName = mapAssetsByName,
                MapNameByAssetPath = mapNameByAssetPath,
                AssetDependencySummariesByAssetPath = assetDependencySummariesByAssetPath!,
                MapSectorsByName = mapSectorsByName!,
                SectorSummariesByAssetPath = sectorSummariesByAssetPath!,
                LightSchemeSectorsByIndex = lightSchemeSectorsByIndex!,
                MusicSchemeSectorsByIndex = musicSchemeSectorsByIndex!,
                AmbientSchemeSectorsByIndex = ambientSchemeSectorsByIndex!,
                MapProjectionsByName = mapProjectionsByName!,
                MessageAssetsByIndex = messageAssetsByIndex!,
                ProtoDefinitionsByNumber = protoDefinitionsByNumber!,
                ScriptDefinitionsById = scriptDefinitionsById!,
                DialogDefinitionsById = dialogDefinitionsById!,
                ScriptDetailsById = scriptDetailsById!,
                DialogDetailsById = dialogDetailsById!,
                ArtDetailsByAssetPath = artDetailsByAssetPath!,
                JumpDetailsByAssetPath = jumpDetailsByAssetPath!,
                MapPropertiesDetailsByAssetPath = mapPropertiesDetailsByAssetPath!,
                TerrainDetailsByAssetPath = terrainDetailsByAssetPath!,
                FacadeWalkDetailsByAssetPath = facadeWalkDetailsByAssetPath!,
                ProtoReferencesByNumber = protoReferencesByNumber!,
                ScriptReferencesById = scriptReferencesById!,
                ArtReferencesById = artReferencesById,
            }
        );

        progress?.Report(new EditorAssetIndexBuildProgress("Asset index complete", 1f, TotalPhases, TotalPhases));

        return (index, validation!);
    }

    private static void RunIndexBuildPhases(params IndexBuildPhase[] phases)
    {
        var block = new ActionBlock<IndexBuildPhase>(
            phase => phase.Execute(),
            new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = phases.Length,
                EnsureOrdered = false,
                MaxDegreeOfParallelism = GetIndexBuildParallelism(),
            }
        );

        for (var index = 0; index < phases.Length; index++)
        {
            if (!block.Post(phases[index]))
                throw new InvalidOperationException("The workspace index pipeline declined a phase before completion.");
        }

        block.Complete();
        block.Completion.GetAwaiter().GetResult();
    }

    private static int GetIndexBuildParallelism() => Math.Clamp(Environment.ProcessorCount / 3, 2, 4);

    private static Lazy<ArtReferenceIndexes> CreateLazyArtReferenceIndexes(
        GameDataStore gameData,
        IReadOnlyDictionary<string, EditorAssetEntry> assetsByPath
    ) =>
        new(
            () =>
            {
                EditorAssetReferenceCounter.CountReferences(
                    gameData,
                    assetsByPath,
                    out _,
                    out _,
                    out _,
                    out _,
                    out var artReferencesById,
                    out var artReferencesByAssetPath
                );
                return new ArtReferenceIndexes(artReferencesById, artReferencesByAssetPath);
            },
            LazyThreadSafetyMode.ExecutionAndPublication
        );

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

    private static (
        IReadOnlyDictionary<string, IReadOnlyList<EditorSectorSummary>> MapSectorsByName,
        IReadOnlyDictionary<string, EditorSectorSummary> SectorSummariesByAssetPath
    ) BuildSectorSummaries(
        GameDataStore gameData,
        IReadOnlyDictionary<string, EditorAssetEntry> assetsByPath,
        IReadOnlyDictionary<string, string> mapNameByAssetPath
    )
    {
        var mapSectorsByName = new Dictionary<string, List<EditorSectorSummary>>(StringComparer.OrdinalIgnoreCase);
        var sectorSummariesByAssetPath = new Dictionary<string, EditorSectorSummary>(StringComparer.OrdinalIgnoreCase);
        var distinctTileArtScratch = new HashSet<uint>();

        foreach (var (assetPath, sectors) in gameData.SectorsBySource)
        {
            if (
                !assetsByPath.TryGetValue(assetPath, out var asset)
                || !mapNameByAssetPath.TryGetValue(assetPath, out var mapName)
            )
                continue;

            foreach (var sector in sectors)
            {
                int? sectorScriptId =
                    sector.SectorScript is { } sectorScript && !sectorScript.IsEmpty ? sectorScript.ScriptId : null;
                var summary = new EditorSectorSummary
                {
                    Asset = asset,
                    MapName = mapName,
                    ObjectCount = sector.Objects.Count,
                    LightCount = sector.Lights.Count,
                    TileScriptCount = sector.TileScripts.Count,
                    SectorScriptId = sectorScriptId,
                    HasRoofs = sector.HasRoofs,
                    DistinctTileArtCount = CountDistinctValues(sector.Tiles, distinctTileArtScratch),
                    BlockedTileCount = CountBlockedTiles(sector.BlockMask),
                    LightSchemeIndex = sector.LightSchemeIdx,
                    MusicSchemeIndex = sector.SoundList.MusicSchemeIdx,
                    AmbientSchemeIndex = sector.SoundList.AmbientSchemeIdx,
                };

                sectorSummariesByAssetPath[assetPath] = summary;

                if (!mapSectorsByName.TryGetValue(mapName, out var mapSectors))
                {
                    mapSectors = [];
                    mapSectorsByName[mapName] = mapSectors;
                }

                mapSectors.Add(summary);
            }
        }

        return (
            mapSectorsByName.ToDictionary(
                pair => pair.Key,
                pair =>
                    (IReadOnlyList<EditorSectorSummary>)
                        pair
                            .Value.OrderBy(static summary => summary.Asset.AssetPath, StringComparer.OrdinalIgnoreCase)
                            .ToArray(),
                StringComparer.OrdinalIgnoreCase
            ),
            sectorSummariesByAssetPath
        );
    }

    private static IReadOnlyDictionary<string, EditorAssetDependencySummary> BuildAssetDependencySummaries(
        IReadOnlyList<EditorAssetEntry> assets,
        IReadOnlyDictionary<string, string> mapNameByAssetPath,
        IReadOnlyDictionary<int, IReadOnlyList<EditorProtoReference>> protoReferencesByNumber,
        IReadOnlyDictionary<string, IReadOnlyList<EditorProtoReference>> protoReferencesByAssetPath,
        IReadOnlyDictionary<int, IReadOnlyList<EditorScriptReference>> scriptReferencesById,
        IReadOnlyDictionary<string, IReadOnlyList<EditorScriptReference>> scriptReferencesByAssetPath,
        IReadOnlyDictionary<uint, IReadOnlyList<EditorArtReference>> artReferencesById,
        IReadOnlyDictionary<string, IReadOnlyList<EditorArtReference>> artReferencesByAssetPath
    )
    {
        return assets.ToDictionary(
            asset => asset.AssetPath,
            asset =>
            {
                int? definedProtoNumber = TryGetProtoNumberFromAssetPath(asset.AssetPath, out var protoNumber)
                    ? protoNumber
                    : null;
                int? definedScriptId = TryGetScriptIdFromAssetPath(asset.AssetPath, out var scriptId) ? scriptId : null;
                int? definedDialogId = TryGetDialogIdFromAssetPath(asset.AssetPath, out var dialogId) ? dialogId : null;

                return new EditorAssetDependencySummary
                {
                    Asset = asset,
                    MapName = mapNameByAssetPath.TryGetValue(asset.AssetPath, out var mapName) ? mapName : null,
                    DefinedProtoNumber = definedProtoNumber,
                    DefinedScriptId = definedScriptId,
                    DefinedDialogId = definedDialogId,
                    ProtoReferences = protoReferencesByAssetPath.TryGetValue(asset.AssetPath, out var protoReferences)
                        ? protoReferences
                        : [],
                    ScriptReferences = scriptReferencesByAssetPath.TryGetValue(
                        asset.AssetPath,
                        out var scriptReferences
                    )
                        ? scriptReferences
                        : [],
                    ArtReferences = artReferencesByAssetPath.TryGetValue(asset.AssetPath, out var artReferences)
                        ? artReferences
                        : [],
                    IncomingProtoReferences =
                        definedProtoNumber is { } incomingProtoNumber
                        && protoReferencesByNumber.TryGetValue(incomingProtoNumber, out var incomingProtoReferences)
                            ? incomingProtoReferences
                            : [],
                    IncomingScriptReferences =
                        definedScriptId is { } incomingScriptKey
                        && scriptReferencesById.TryGetValue(incomingScriptKey, out var incomingScriptReferences)
                            ? incomingScriptReferences
                            : [],
                };
            },
            StringComparer.OrdinalIgnoreCase
        );
    }

    private static (
        IReadOnlyDictionary<int, IReadOnlyList<EditorSectorSummary>> LightSchemeSectorsByIndex,
        IReadOnlyDictionary<int, IReadOnlyList<EditorSectorSummary>> MusicSchemeSectorsByIndex,
        IReadOnlyDictionary<int, IReadOnlyList<EditorSectorSummary>> AmbientSchemeSectorsByIndex
    ) BuildSectorSchemeLookups(IReadOnlyList<EditorSectorSummary> sectorSummaries)
    {
        return (
            GroupSectorSummariesByIndex(sectorSummaries, static sector => sector.LightSchemeIndex),
            GroupSectorSummariesByIndex(sectorSummaries, static sector => sector.MusicSchemeIndex),
            GroupSectorSummariesByIndex(sectorSummaries, static sector => sector.AmbientSchemeIndex)
        );
    }

    private static IReadOnlyDictionary<int, IReadOnlyList<EditorSectorSummary>> GroupSectorSummariesByIndex(
        IReadOnlyList<EditorSectorSummary> sectorSummaries,
        Func<EditorSectorSummary, int> getIndex
    )
    {
        var sectorsByIndex = new Dictionary<int, List<EditorSectorSummary>>();

        foreach (var sector in sectorSummaries)
        {
            var index = getIndex(sector);
            if (!sectorsByIndex.TryGetValue(index, out var sectors))
            {
                sectors = [];
                sectorsByIndex[index] = sectors;
            }

            sectors.Add(sector);
        }

        return sectorsByIndex.ToDictionary(
            pair => pair.Key,
            pair =>
                (IReadOnlyList<EditorSectorSummary>)
                    pair
                        .Value.OrderBy(static sector => sector.Asset.AssetPath, StringComparer.OrdinalIgnoreCase)
                        .ToArray()
        );
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
            if (!EditorWorkspaceValidator.IsProtoDisplayNameAssetPath(assetPath))
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
                var activeAttachmentSlots = ScriptValidator.GetActiveAttachmentSlots(script).ToArray();
                var activeAttachmentPoints = activeAttachmentSlots
                    .Where(static slot => ScriptValidator.IsKnownAttachmentSlot(slot))
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
                var nodes = dialog
                    .Entries.Select(entry => new EditorDialogNode
                    {
                        EntryNumber = entry.Num,
                        Text = entry.Text,
                        GenderField = entry.GenderField,
                        IntelligenceRequirement = entry.Iq,
                        Conditions = entry.Conditions,
                        Actions = entry.Actions,
                        ResponseTargetNumber = entry.ResponseVal,
                        Kind = GetDialogNodeKind(entry),
                        IsRoot = !inboundTargets.Contains(entry.Num),
                        HasMissingResponseTarget = entry.ResponseVal > 0 && !entryNumbers.Contains(entry.ResponseVal),
                    })
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
                        Nodes = nodes,
                        RootEntryNumbers = roots,
                        MissingResponseTargetNumbers = missingTargets,
                    }
                );
            }
        }

        return OrderDefinitions(definitionsById);
    }

    private static IReadOnlyDictionary<string, EditorArtDefinition> BuildArtDetailsByAssetPath(
        GameDataStore gameData,
        IReadOnlyDictionary<string, EditorAssetEntry> assetsByPath
    )
    {
        var detailsByAssetPath = new Dictionary<string, EditorArtDefinition>(StringComparer.OrdinalIgnoreCase);

        foreach (var (assetPath, arts) in gameData.ArtsBySource)
        {
            if (!assetsByPath.TryGetValue(assetPath, out var asset) || arts.Count == 0)
                continue;

            var art = arts[0];
            var maxFrameWidth = 0;
            var maxFrameHeight = 0;

            for (var rotationIndex = 0; rotationIndex < art.Frames.Length; rotationIndex++)
            {
                var frames = art.Frames[rotationIndex];
                for (var frameIndex = 0; frameIndex < frames.Length; frameIndex++)
                {
                    var header = frames[frameIndex].Header;
                    maxFrameWidth = Math.Max(maxFrameWidth, checked((int)header.Width));
                    maxFrameHeight = Math.Max(maxFrameHeight, checked((int)header.Height));
                }
            }

            detailsByAssetPath[assetPath] = new EditorArtDefinition
            {
                Asset = asset,
                Flags = art.Flags,
                FrameRate = art.FrameRate,
                ActionFrame = art.ActionFrame,
                RotationCount = art.EffectiveRotationCount,
                FramesPerRotation = checked((int)art.FrameCount),
                PaletteCount = art.Palettes.Count(static palette => palette is not null),
                MaxFrameWidth = maxFrameWidth,
                MaxFrameHeight = maxFrameHeight,
            };
        }

        return detailsByAssetPath;
    }

    private static IReadOnlyDictionary<string, EditorJumpDefinition> BuildJumpDetailsByAssetPath(
        GameDataStore gameData,
        IReadOnlyDictionary<string, EditorAssetEntry> assetsByPath
    )
    {
        var detailsByAssetPath = new Dictionary<string, EditorJumpDefinition>(StringComparer.OrdinalIgnoreCase);

        foreach (var (assetPath, jumpFiles) in gameData.JumpFilesBySource)
        {
            if (!assetsByPath.TryGetValue(assetPath, out var asset) || jumpFiles.Count == 0)
                continue;

            var jumpFile = jumpFiles[0];
            detailsByAssetPath[assetPath] = new EditorJumpDefinition
            {
                Asset = asset,
                JumpCount = jumpFile.Jumps.Count,
                DestinationMapIds =
                [
                    .. jumpFile
                        .Jumps.Select(static jump => jump.DestinationMapId)
                        .Distinct()
                        .OrderBy(static mapId => mapId),
                ],
            };
        }

        return detailsByAssetPath;
    }

    private static IReadOnlyDictionary<string, EditorMapPropertiesDefinition> BuildMapPropertiesDetailsByAssetPath(
        GameDataStore gameData,
        IReadOnlyDictionary<string, EditorAssetEntry> assetsByPath
    )
    {
        var detailsByAssetPath = new Dictionary<string, EditorMapPropertiesDefinition>(
            StringComparer.OrdinalIgnoreCase
        );

        foreach (var (assetPath, propertiesList) in gameData.MapPropertiesBySource)
        {
            if (!assetsByPath.TryGetValue(assetPath, out var asset) || propertiesList.Count == 0)
                continue;

            var properties = propertiesList[0];
            detailsByAssetPath[assetPath] = new EditorMapPropertiesDefinition
            {
                Asset = asset,
                ArtId = properties.ArtId,
                LimitX = properties.LimitX,
                LimitY = properties.LimitY,
            };
        }

        return detailsByAssetPath;
    }

    private static IReadOnlyDictionary<string, EditorTerrainDefinition> BuildTerrainDetailsByAssetPath(
        GameDataStore gameData,
        IReadOnlyDictionary<string, EditorAssetEntry> assetsByPath
    )
    {
        var detailsByAssetPath = new Dictionary<string, EditorTerrainDefinition>(StringComparer.OrdinalIgnoreCase);

        foreach (var (assetPath, terrains) in gameData.TerrainsBySource)
        {
            if (!assetsByPath.TryGetValue(assetPath, out var asset) || terrains.Count == 0)
                continue;

            var terrain = terrains[0];
            detailsByAssetPath[assetPath] = new EditorTerrainDefinition
            {
                Asset = asset,
                Version = terrain.Version,
                BaseTerrainType = terrain.BaseTerrainType,
                Width = terrain.Width,
                Height = terrain.Height,
                Compressed = terrain.Compressed,
                DistinctTileCount = terrain.Tiles.Distinct().Count(),
            };
        }

        return detailsByAssetPath;
    }

    private static IReadOnlyDictionary<string, EditorFacadeWalkDefinition> BuildFacadeWalkDetailsByAssetPath(
        GameDataStore gameData,
        IReadOnlyDictionary<string, EditorAssetEntry> assetsByPath
    )
    {
        var detailsByAssetPath = new Dictionary<string, EditorFacadeWalkDefinition>(StringComparer.OrdinalIgnoreCase);

        foreach (var (assetPath, facadeWalks) in gameData.FacadeWalksBySource)
        {
            if (!assetsByPath.TryGetValue(assetPath, out var asset) || facadeWalks.Count == 0)
                continue;

            var facadeWalk = facadeWalks[0];
            detailsByAssetPath[assetPath] = new EditorFacadeWalkDefinition
            {
                Asset = asset,
                Terrain = facadeWalk.Header.Terrain,
                Outdoor = facadeWalk.Header.Outdoor != 0,
                Flippable = facadeWalk.Header.Flippable != 0,
                Width = facadeWalk.Header.Width,
                Height = facadeWalk.Header.Height,
                EntryCount = facadeWalk.Entries.Length,
                WalkableEntryCount = facadeWalk.Entries.Count(static entry => entry.Walkable),
            };
        }

        return detailsByAssetPath;
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

    private static int CountBlockedTiles(uint[] blockMask) => blockMask.Sum(mask => int.PopCount((int)mask));

    private static int CountDistinctValues(uint[] values, HashSet<uint> scratch)
    {
        scratch.Clear();
        for (var index = 0; index < values.Length; index++)
            scratch.Add(values[index]);
        return scratch.Count;
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

    private static EditorDialogNodeKind GetDialogNodeKind(DialogEntry entry)
    {
        if (IsControlEntry(entry))
            return EditorDialogNodeKind.Control;

        return entry.Iq == 0 ? EditorDialogNodeKind.NpcReply : EditorDialogNodeKind.PcOption;
    }

    private static bool TryGetProtoNumberFromAssetPath(string assetPath, out int protoNumber)
    {
        if (!assetPath.EndsWith(".pro", StringComparison.OrdinalIgnoreCase))
        {
            protoNumber = 0;
            return false;
        }
        return TryGetAssetNumberFromPath(assetPath, s_protoAssetPathPattern, out protoNumber);
    }

    private static bool TryGetScriptIdFromAssetPath(string assetPath, out int scriptId)
    {
        if (!assetPath.EndsWith(".scr", StringComparison.OrdinalIgnoreCase))
        {
            scriptId = 0;
            return false;
        }
        return TryGetAssetNumberFromPath(assetPath, s_scriptAssetPathPattern, out scriptId);
    }

    private static bool TryGetDialogIdFromAssetPath(string assetPath, out int dialogId)
    {
        if (!assetPath.EndsWith(".dlg", StringComparison.OrdinalIgnoreCase))
        {
            dialogId = 0;
            return false;
        }
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

    private readonly record struct IndexBuildPhase(string Activity, Action Execute);

    private readonly record struct ArtReferenceIndexes(
        IReadOnlyDictionary<uint, IReadOnlyList<EditorArtReference>> ById,
        IReadOnlyDictionary<string, IReadOnlyList<EditorArtReference>> ByAssetPath
    );

    private delegate bool TryGetAssetId(string assetPath, out int assetId);
}
