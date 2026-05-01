using System.Text.RegularExpressions;
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
        ArcanumInstallationType? installationType = null
    )
    {
        ArgumentNullException.ThrowIfNull(gameData);
        ArgumentNullException.ThrowIfNull(assets);

        var assetsByPath = assets.Entries.ToDictionary(entry => entry.AssetPath, StringComparer.OrdinalIgnoreCase);
        var (mapNames, mapAssetsByName, mapNameByAssetPath) = BuildMapAssets(assets.Entries);
        var (mapSectorsByName, sectorSummariesByAssetPath) = BuildSectorSummaries(
            gameData,
            assetsByPath,
            mapNameByAssetPath
        );
        var sectorSummaries = sectorSummariesByAssetPath.Values.ToArray();
        var (lightSchemeSectorsByIndex, musicSchemeSectorsByIndex, ambientSchemeSectorsByIndex) =
            BuildSectorSchemeLookups(sectorSummaries);
        var mapProjectionsByName = EditorSectorProjectionBuilder.Build(mapSectorsByName);
        var messageAssetsByIndex = BuildMessageAssetsByIndex(gameData, assetsByPath);
        var protoDisplayNameMessageIndices = BuildProtoDisplayNameMessageIndices(gameData);
        var protoDefinitionsByNumber = BuildProtoDefinitionsByNumber(assets.Entries);
        var scriptDefinitionsById = BuildScriptDefinitionsById(assets.Entries);
        var dialogDefinitionsById = BuildDialogDefinitionsById(assets.Entries);
        var scriptDetailsById = BuildScriptDetailsById(gameData, assetsByPath);
        var dialogDetailsById = BuildDialogDetailsById(gameData, assetsByPath);
        var artDetailsByAssetPath = BuildArtDetailsByAssetPath(gameData, assetsByPath);
        var jumpDetailsByAssetPath = BuildJumpDetailsByAssetPath(gameData, assetsByPath);
        var mapPropertiesDetailsByAssetPath = BuildMapPropertiesDetailsByAssetPath(gameData, assetsByPath);
        var terrainDetailsByAssetPath = BuildTerrainDetailsByAssetPath(gameData, assetsByPath);
        var facadeWalkDetailsByAssetPath = BuildFacadeWalkDetailsByAssetPath(gameData, assetsByPath);
        var protoReferencesByNumber = EditorAssetReferenceCounter.CountProtoReferences(gameData, assetsByPath);
        var scriptReferencesById = EditorAssetReferenceCounter.CountScriptReferences(gameData, assetsByPath);
        var artReferencesById = EditorAssetReferenceCounter.CountArtReferences(gameData, assetsByPath);
        var assetDependencySummariesByAssetPath = BuildAssetDependencySummaries(
            assets.Entries,
            mapNameByAssetPath,
            protoReferencesByNumber,
            scriptReferencesById,
            artReferencesById
        );

        var index = EditorAssetIndex.Create(
            new EditorAssetIndexData
            {
                MapNames = mapNames,
                MapAssetsByName = mapAssetsByName,
                MapNameByAssetPath = mapNameByAssetPath,
                AssetDependencySummariesByAssetPath = assetDependencySummariesByAssetPath,
                MapSectorsByName = mapSectorsByName,
                SectorSummariesByAssetPath = sectorSummariesByAssetPath,
                LightSchemeSectorsByIndex = lightSchemeSectorsByIndex,
                MusicSchemeSectorsByIndex = musicSchemeSectorsByIndex,
                AmbientSchemeSectorsByIndex = ambientSchemeSectorsByIndex,
                MapProjectionsByName = mapProjectionsByName,
                MessageAssetsByIndex = messageAssetsByIndex,
                ProtoDefinitionsByNumber = protoDefinitionsByNumber,
                ScriptDefinitionsById = scriptDefinitionsById,
                DialogDefinitionsById = dialogDefinitionsById,
                ScriptDetailsById = scriptDetailsById,
                DialogDetailsById = dialogDetailsById,
                ArtDetailsByAssetPath = artDetailsByAssetPath,
                JumpDetailsByAssetPath = jumpDetailsByAssetPath,
                MapPropertiesDetailsByAssetPath = mapPropertiesDetailsByAssetPath,
                TerrainDetailsByAssetPath = terrainDetailsByAssetPath,
                FacadeWalkDetailsByAssetPath = facadeWalkDetailsByAssetPath,
                ProtoReferencesByNumber = protoReferencesByNumber,
                ScriptReferencesById = scriptReferencesById,
                ArtReferencesById = artReferencesById,
            }
        );
        var validation = new EditorWorkspaceValidator().Build(
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
                    DistinctTileArtCount = sector.Tiles.Distinct().Count(),
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
        IReadOnlyDictionary<int, IReadOnlyList<EditorScriptReference>> scriptReferencesById,
        IReadOnlyDictionary<uint, IReadOnlyList<EditorArtReference>> artReferencesById
    )
    {
        var protoReferencesByAssetPath = protoReferencesByNumber
            .Values.SelectMany(static references => references)
            .GroupBy(static reference => reference.Asset.AssetPath, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group =>
                    (IReadOnlyList<EditorProtoReference>)
                        group.OrderBy(static reference => reference.ProtoNumber).ToArray(),
                StringComparer.OrdinalIgnoreCase
            );
        var scriptReferencesByAssetPath = scriptReferencesById
            .Values.SelectMany(static references => references)
            .GroupBy(static reference => reference.Asset.AssetPath, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group =>
                    (IReadOnlyList<EditorScriptReference>)
                        group.OrderBy(static reference => reference.ScriptId).ToArray(),
                StringComparer.OrdinalIgnoreCase
            );
        var artReferencesByAssetPath = artReferencesById
            .Values.SelectMany(static references => references)
            .GroupBy(static reference => reference.Asset.AssetPath, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group =>
                    (IReadOnlyList<EditorArtReference>)group.OrderBy(static reference => reference.ArtId).ToArray(),
                StringComparer.OrdinalIgnoreCase
            );

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
