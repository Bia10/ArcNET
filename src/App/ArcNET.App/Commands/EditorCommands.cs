using System.Globalization;
using ArcNET.App.Output;
using ArcNET.App.Rendering;
using ArcNET.Editor;
using ArcNET.Formats;
using ConsoleAppFramework;

namespace ArcNET.App;

/// <summary>
/// <c>editor</c> command group - inspect install-backed editor workspace state against live data.
/// Usage: <c>arcnet editor &lt;subcommand&gt; &lt;gameDir&gt; [args...]</c>
/// </summary>
public sealed class EditorCommands
{
    private readonly IEditorOutput _output;

    public EditorCommands()
        : this(new ConsoleEditorOutput()) { }

    internal EditorCommands(IEditorOutput output) => _output = output;

    /// <summary>Load a live install-backed workspace and print high-level counts plus skip totals.</summary>
    public async Task Summary([Argument] string gameDir, string? saveFolder = null, string? saveSlotName = null)
    {
        var workspace = await LoadWorkspace(gameDir, saveFolder, saveSlotName);
        var validationIssues = workspace.Validation.Issues;

        _output.WriteLine($"GameDirectory: {workspace.GameDirectory}");
        _output.WriteLine($"InstallType: {workspace.InstallationType}");
        _output.WriteLine($"Messages: {workspace.GameData.Messages.Count}");
        _output.WriteLine($"Sectors: {workspace.GameData.Sectors.Count}");
        _output.WriteLine($"Protos: {workspace.GameData.Protos.Count}");
        _output.WriteLine($"Mobs: {workspace.GameData.Mobs.Count}");
        _output.WriteLine($"Scripts: {workspace.GameData.Scripts.Count}");
        _output.WriteLine($"Dialogs: {workspace.GameData.Dialogs.Count}");
        _output.WriteLine($"Assets: {workspace.Assets.Count}");
        _output.WriteLine($"Maps: {workspace.Index.MapNames.Count}");
        _output.WriteLine($"SkippedArchiveCandidates: {workspace.LoadReport.SkippedArchiveCandidates.Count}");
        _output.WriteLine($"SkippedAssets: {workspace.LoadReport.SkippedAssets.Count}");
        _output.WriteLine($"ValidationIssues: {validationIssues.Count}");
        _output.WriteLine(
            $"ValidationErrors: {validationIssues.Count(static issue => issue.Severity == EditorWorkspaceValidationSeverity.Error)}"
        );
        _output.WriteLine(
            $"ValidationWarnings: {validationIssues.Count(static issue => issue.Severity == EditorWorkspaceValidationSeverity.Warning)}"
        );
        _output.WriteLine(
            $"ValidationInfos: {validationIssues.Count(static issue => issue.Severity == EditorWorkspaceValidationSeverity.Info)}"
        );
        _output.WriteLine($"HasSave: {workspace.HasSaveLoaded}");
    }

    /// <summary>Print all skipped archive candidates and skipped winning assets from a live install load.</summary>
    public async Task Skipped(
        [Argument] string gameDir,
        string? format = null,
        string? source = null,
        string? asset = null,
        string? reason = null,
        int top = 200
    )
    {
        if (!EditorCommandParsers.TryParseFormatFilter(format, out var parsedFormat))
        {
            _output.WriteError($"Invalid format filter: {format}");
            return;
        }

        var workspace = await LoadWorkspace(gameDir);
        var limit = ClampTop(top);
        var skippedArchives = workspace
            .LoadReport.SkippedArchiveCandidates.Where(skippedArchive =>
                MatchesContains(skippedArchive.Path, source) && MatchesContains(skippedArchive.Reason, reason)
            )
            .Take(limit)
            .ToArray();
        var skippedAssets = workspace
            .LoadReport.SkippedAssets.Where(skippedAsset =>
                MatchesFormat(skippedAsset.Format, parsedFormat)
                && MatchesContains(skippedAsset.AssetPath, asset)
                && MatchesContains(skippedAsset.Reason, reason)
                && MatchesContains(source, skippedAsset.SourcePath, skippedAsset.SourceEntryPath)
            )
            .Take(limit)
            .ToArray();

        _output.WriteLine(
            $"Filters: format={format ?? "-"}|source={source ?? "-"}|asset={asset ?? "-"}|reason={reason ?? "-"}|top={limit}"
        );

        _output.WriteLine(
            $"SkippedArchiveCandidates: {skippedArchives.Length}/{workspace.LoadReport.SkippedArchiveCandidates.Count}"
        );
        foreach (var skippedArchive in skippedArchives)
            _output.WriteLine($"ARCHIVE|{skippedArchive.Path}|{skippedArchive.Reason}");

        _output.WriteLine($"SkippedAssets: {skippedAssets.Length}/{workspace.LoadReport.SkippedAssets.Count}");
        foreach (var skippedAsset in skippedAssets)
        {
            _output.WriteLine(
                $"ASSET|{skippedAsset.AssetPath}|{skippedAsset.Format}|{skippedAsset.SourceKind}|{skippedAsset.SourcePath}|{skippedAsset.SourceEntryPath ?? "-"}|{skippedAsset.Reason}"
            );
        }
    }

    /// <summary>Print workspace validation findings from a live install, with optional filters.</summary>
    public async Task<int> Validate(
        [Argument] string gameDir,
        string? severity = null,
        string? asset = null,
        string? text = null,
        int top = 200
    )
    {
        if (!EditorCommandParsers.TryParseValidationSeverity(severity, out var parsedSeverity))
        {
            _output.WriteError($"Invalid severity filter: {severity}");
            return 2;
        }

        var workspace = await LoadWorkspace(gameDir);
        var validationIssues = workspace.Validation.Issues;
        var limit = ClampTop(top);
        var filteredIssues = validationIssues
            .Where(issue =>
                MatchesValidationSeverity(issue.Severity, parsedSeverity)
                && MatchesContains(issue.AssetPath, asset)
                && MatchesContains(issue.Message, text)
            )
            .Take(limit)
            .ToArray();

        _output.WriteLine($"Filters: severity={severity ?? "-"}|asset={asset ?? "-"}|text={text ?? "-"}|top={limit}");
        _output.WriteLine($"ValidationIssues: {filteredIssues.Length}/{validationIssues.Count}");
        _output.WriteLine(
            $"ValidationErrors: {validationIssues.Count(static issue => issue.Severity == EditorWorkspaceValidationSeverity.Error)}"
        );
        _output.WriteLine(
            $"ValidationWarnings: {validationIssues.Count(static issue => issue.Severity == EditorWorkspaceValidationSeverity.Warning)}"
        );
        _output.WriteLine(
            $"ValidationInfos: {validationIssues.Count(static issue => issue.Severity == EditorWorkspaceValidationSeverity.Info)}"
        );

        foreach (var issue in filteredIssues)
            _output.WriteLine($"{issue.Severity.ToString().ToUpperInvariant()}|{issue.AssetPath}|{issue.Message}");

        return workspace.Validation.HasErrors ? 1 : 0;
    }

    /// <summary>List all map names discovered in the loaded workspace.</summary>
    public async Task Maps([Argument] string gameDir, int top = 50)
    {
        var workspace = await LoadWorkspace(gameDir);
        IMapIndex index = workspace.Index;
        var limit = ClampTop(top);

        _output.WriteLine($"MapCount: {index.MapNames.Count}");
        foreach (var mapName in index.MapNames.Take(limit))
            _output.WriteLine(mapName);
    }

    /// <summary>Print all indexed assets that belong to one map directory.</summary>
    public async Task Map([Argument] string gameDir, [Argument] string mapName)
    {
        var workspace = await LoadWorkspace(gameDir);
        IMapIndex index = workspace.Index;
        var assets = index.FindMapAssets(mapName);
        var sectors = index.FindMapSectors(mapName);

        if (assets.Count == 0)
        {
            _output.WriteLine($"Map not found: {mapName}");
            return;
        }

        _output.WriteLine($"Map: {mapName}");
        _output.WriteLine($"AssetCount: {assets.Count}");
        _output.WriteLine($"SectorCount: {sectors.Count}");

        foreach (var group in assets.GroupBy(static asset => asset.Format).OrderBy(static group => group.Key))
            _output.WriteLine($"{group.Key}: {group.Count()}");

        foreach (var sector in sectors)
            _output.WriteLine(FormatSectorSummary(sector));

        foreach (var asset in assets)
            _output.WriteLine(asset.AssetPath);
    }

    /// <summary>Print one parsed sector summary by workspace asset path.</summary>
    public async Task Sector([Argument] string gameDir, [Argument] string assetPath)
    {
        var workspace = await LoadWorkspace(gameDir);
        IMapIndex index = workspace.Index;
        var sector = index.FindSectorSummary(assetPath);

        if (sector is null)
        {
            _output.WriteLine($"Sector not found: {assetPath}");
            return;
        }

        _output.WriteLine($"AssetPath: {sector.Asset.AssetPath}");
        _output.WriteLine($"Map: {sector.MapName}");
        _output.WriteLine($"ObjectCount: {sector.ObjectCount}");
        _output.WriteLine($"LightCount: {sector.LightCount}");
        _output.WriteLine($"TileScriptCount: {sector.TileScriptCount}");
        _output.WriteLine($"SectorScriptId: {sector.SectorScriptId?.ToString(CultureInfo.InvariantCulture) ?? "-"}");
        _output.WriteLine($"HasRoofs: {sector.HasRoofs}");
        _output.WriteLine($"DistinctTileArtCount: {sector.DistinctTileArtCount}");
        _output.WriteLine($"BlockedTileCount: {sector.BlockedTileCount}");
        _output.WriteLine($"LightSchemeIdx: {sector.LightSchemeIndex}");
        _output.WriteLine($"MusicSchemeIdx: {sector.MusicSchemeIndex}");
        _output.WriteLine($"AmbientSchemeIdx: {sector.AmbientSchemeIndex}");
    }

    /// <summary>Print all sectors that use one light, music, or ambient scheme index.</summary>
    public async Task Scheme(
        [Argument] string gameDir,
        [Argument] string kind,
        [Argument] int schemeIndex,
        int top = 20
    )
    {
        var workspace = await LoadWorkspace(gameDir);
        ISchemeIndex index = workspace.Index;
        if (!TryGetSchemeSectors(index, kind, schemeIndex, out var normalizedKind, out var sectors))
        {
            _output.WriteError($"Invalid scheme kind: {kind}");
            return;
        }

        var limit = ClampTop(top);
        _output.WriteLine($"SchemeKind: {normalizedKind}");
        _output.WriteLine($"SchemeIndex: {schemeIndex}");
        _output.WriteLine($"SectorCount: {sectors.Count}");

        foreach (var sector in sectors.Take(limit))
            _output.WriteLine(FormatSectorSummary(sector));
    }

    /// <summary>Print a map's projected sector footprint as an ASCII occupancy or feature overlay grid.</summary>
    public async Task Outline([Argument] string gameDir, [Argument] string mapName, string mode = "occupancy")
    {
        if (!EditorCommandParsers.TryParseOutlineMode(mode, out var outlineMode, out var normalizedMode))
        {
            _output.WriteError($"Invalid outline mode: {mode}. Expected: {EditorCommandParsers.OutlineModesHelpText}");
            return;
        }

        var workspace = await LoadWorkspace(gameDir);
        IMapIndex index = workspace.Index;
        var assets = index.FindMapAssets(mapName);

        if (assets.Count == 0)
        {
            _output.WriteLine($"Map not found: {mapName}");
            return;
        }

        var sectors = index.FindMapSectors(mapName);
        var projection = index.FindMapProjection(mapName);

        _output.WriteLine($"Map: {mapName}");
        _output.WriteLine($"Mode: {normalizedMode}");
        _output.WriteLine($"SectorCount: {sectors.Count}");

        if (projection is null)
        {
            _output.WriteLine("PositionedSectors: 0");
            _output.WriteLine("UnpositionedSectors: 0");
            _output.WriteLine("Outline: <none>");
            return;
        }

        _output.WriteLine($"PositionedSectors: {projection.Sectors.Count}");
        _output.WriteLine($"UnpositionedSectors: {projection.UnpositionedSectorCount}");

        if (projection.Sectors.Count == 0)
        {
            _output.WriteLine("Outline: <no positioned sectors>");
            return;
        }

        _output.WriteLine(
            $"Bounds: x={projection.MinSectorX}..{projection.MaxSectorX}|y={projection.MinSectorY}..{projection.MaxSectorY}|width={projection.Width}|height={projection.Height}"
        );

        EditorOutlineRenderer.Render(projection, outlineMode, _output);
    }

    /// <summary>Print all assets that define one message index.</summary>
    public async Task Message([Argument] string gameDir, [Argument] int messageIndex)
    {
        var workspace = await LoadWorkspace(gameDir);
        IMessageIndex index = workspace.Index;
        var assets = index.FindMessageAssets(messageIndex);

        _output.WriteLine($"MessageIndex: {messageIndex}");
        _output.WriteLine($"AssetCount: {assets.Count}");
        foreach (var asset in assets)
            _output.WriteLine(asset.AssetPath);
    }

    /// <summary>Print the defining asset and reverse references for one proto number.</summary>
    public async Task Proto([Argument] string gameDir, [Argument] int protoNumber, int top = 20)
    {
        var workspace = await LoadWorkspace(gameDir);
        IProtoIndex index = workspace.Index;
        var definition = index.FindProtoDefinition(protoNumber);
        var references = index.FindProtoReferences(protoNumber);

        _output.WriteLine($"ProtoNumber: {protoNumber}");
        _output.WriteLine($"Definition: {definition?.AssetPath ?? "<missing>"}");
        _output.WriteLine($"ReferenceAssets: {references.Count}");
        _output.WriteLine($"ReferenceCount: {references.Sum(static reference => reference.Count)}");

        foreach (
            var reference in references
                .OrderByDescending(static reference => reference.Count)
                .ThenBy(static reference => reference.Asset.AssetPath, StringComparer.OrdinalIgnoreCase)
                .Take(ClampTop(top))
        )
            _output.WriteLine($"{reference.Count}|{reference.Asset.AssetPath}|{reference.Format}");
    }

    /// <summary>Print reverse references for one script identifier.</summary>
    public async Task Script([Argument] string gameDir, [Argument] int scriptId, int top = 20)
    {
        var workspace = await LoadWorkspace(gameDir);
        IScriptIndex index = workspace.Index;
        var details = index.FindScriptDetails(scriptId);
        var references = index.FindScriptReferences(scriptId);
        var limit = ClampTop(top);

        _output.WriteLine($"ScriptId: {scriptId}");
        _output.WriteLine($"DefinitionAssets: {details.Count}");
        foreach (var detail in details.Take(limit))
        {
            _output.WriteLine(
                $"DEF|{detail.Asset.AssetPath}|entries={detail.EntryCount}|active={detail.ActiveAttachmentCount}|slots={FormatValues(detail.ActiveAttachmentSlots)}|points={FormatValues(detail.ActiveAttachmentPoints)}|flags={detail.Flags}|description={detail.Description}"
            );
        }
        _output.WriteLine($"ReferenceAssets: {references.Count}");
        _output.WriteLine($"ReferenceCount: {references.Sum(static reference => reference.Count)}");

        foreach (
            var reference in references
                .OrderByDescending(static reference => reference.Count)
                .ThenBy(static reference => reference.Asset.AssetPath, StringComparer.OrdinalIgnoreCase)
                .Take(limit)
        )
            _output.WriteLine($"{reference.Count}|{reference.Asset.AssetPath}|{reference.Format}");
    }

    /// <summary>Print all dialog assets whose file name starts with one dialog identifier.</summary>
    public async Task Dialog([Argument] string gameDir, [Argument] int dialogId, int top = 20)
    {
        var workspace = await LoadWorkspace(gameDir);
        IDialogIndex index = workspace.Index;
        var details = index.FindDialogDetails(dialogId);
        var limit = ClampTop(top);

        _output.WriteLine($"DialogId: {dialogId}");
        _output.WriteLine($"DefinitionAssets: {details.Count}");
        foreach (var detail in details.Take(limit))
        {
            _output.WriteLine(
                $"DEF|{detail.Asset.AssetPath}|entries={detail.EntryCount}|npc={detail.NpcEntryCount}|pc={detail.PcOptionCount}|control={detail.ControlEntryCount}|transitions={detail.TransitionCount}|terminals={detail.TerminalEntryCount}|rootEntries={detail.RootEntryNumbers.Count}|missingTargets={detail.MissingResponseTargetNumbers.Count}"
            );
        }
    }

    /// <summary>Print reverse references for one art resource identifier (decimal or 0x-prefixed hex).</summary>
    public async Task Art([Argument] string gameDir, [Argument] string artId, int top = 20)
    {
        if (!EditorCommandParsers.TryParseUInt32(artId, out var parsedArtId))
        {
            _output.WriteError($"Invalid art ID: {artId}");
            return;
        }

        var workspace = await LoadWorkspace(gameDir);
        IArtIndex index = workspace.Index;
        var references = index.FindArtReferences(parsedArtId);

        _output.WriteLine($"ArtId: 0x{parsedArtId:X8}");
        _output.WriteLine($"ReferenceAssets: {references.Count}");
        _output.WriteLine($"ReferenceCount: {references.Sum(static reference => reference.Count)}");

        foreach (
            var reference in references
                .OrderByDescending(static reference => reference.Count)
                .ThenBy(static reference => reference.Asset.AssetPath, StringComparer.OrdinalIgnoreCase)
                .Take(ClampTop(top))
        )
            _output.WriteLine($"{reference.Count}|{reference.Asset.AssetPath}|{reference.Format}");
    }

    /// <summary>Print one indexed jump-file detail by asset path, or search jump assets by path/map id text.</summary>
    public async Task Jump([Argument] string gameDir, [Argument] string assetPathOrSearch, int top = 20)
    {
        var workspace = await LoadWorkspace(gameDir);
        IJumpIndex index = workspace.Index;
        var normalizedInput = NormalizeAssetPathArgument(assetPathOrSearch);

        if (index.FindJumpDetail(normalizedInput) is { } detail)
        {
            WriteAssetHeader(detail.Asset);
            _output.WriteLine($"JumpCount: {detail.JumpCount}");
            _output.WriteLine($"DestinationMapIds: {FormatValues(detail.DestinationMapIds)}");
            return;
        }

        var limit = ClampTop(top);
        var matches = index.SearchJumpDetails(normalizedInput);
        _output.WriteLine($"JumpMatches: {Math.Min(matches.Count, limit)}/{matches.Count}");

        if (matches.Count == 0)
        {
            _output.WriteLine($"Jump asset not found: {normalizedInput}");
            return;
        }

        foreach (var match in matches.Take(limit))
        {
            _output.WriteLine(
                $"JUMP|{match.Asset.AssetPath}|jumps={match.JumpCount}|destinations={FormatValues(match.DestinationMapIds)}"
            );
        }
    }

    /// <summary>Print one indexed map-properties detail by asset path, or search map-properties assets by path/art text.</summary>
    public async Task MapProperties([Argument] string gameDir, [Argument] string assetPathOrSearch, int top = 20)
    {
        var workspace = await LoadWorkspace(gameDir);
        IMapPropertiesIndex index = workspace.Index;
        var normalizedInput = NormalizeAssetPathArgument(assetPathOrSearch);

        if (index.FindMapPropertiesDetail(normalizedInput) is { } detail)
        {
            WriteAssetHeader(detail.Asset);
            _output.WriteLine($"ArtId: {detail.ArtId}");
            _output.WriteLine($"LimitX: {detail.LimitX}");
            _output.WriteLine($"LimitY: {detail.LimitY}");
            return;
        }

        var limit = ClampTop(top);
        var matches = index.SearchMapPropertiesDetails(normalizedInput);
        _output.WriteLine($"MapPropertiesMatches: {Math.Min(matches.Count, limit)}/{matches.Count}");

        if (matches.Count == 0)
        {
            _output.WriteLine($"Map-properties asset not found: {normalizedInput}");
            return;
        }

        foreach (var match in matches.Take(limit))
        {
            _output.WriteLine(
                $"MAP-PROPERTIES|{match.Asset.AssetPath}|artId={match.ArtId}|limitX={match.LimitX}|limitY={match.LimitY}"
            );
        }
    }

    /// <summary>Print one indexed terrain detail by asset path, or search terrain assets by path/base-terrain text.</summary>
    public async Task Terrain([Argument] string gameDir, [Argument] string assetPathOrSearch, int top = 20)
    {
        var workspace = await LoadWorkspace(gameDir);
        ITerrainIndex index = workspace.Index;
        var normalizedInput = NormalizeAssetPathArgument(assetPathOrSearch);

        if (index.FindTerrainDetail(normalizedInput) is { } detail)
        {
            WriteAssetHeader(detail.Asset);
            _output.WriteLine($"Version: {detail.Version.ToString(CultureInfo.InvariantCulture)}");
            _output.WriteLine($"BaseTerrainType: {detail.BaseTerrainType}");
            _output.WriteLine($"Width: {detail.Width}");
            _output.WriteLine($"Height: {detail.Height}");
            _output.WriteLine($"Compressed: {detail.Compressed}");
            _output.WriteLine($"DistinctTileCount: {detail.DistinctTileCount}");
            return;
        }

        var limit = ClampTop(top);
        var matches = index.SearchTerrainDetails(normalizedInput);
        _output.WriteLine($"TerrainMatches: {Math.Min(matches.Count, limit)}/{matches.Count}");

        if (matches.Count == 0)
        {
            _output.WriteLine($"Terrain asset not found: {normalizedInput}");
            return;
        }

        foreach (var match in matches.Take(limit))
        {
            _output.WriteLine(
                $"TERRAIN|{match.Asset.AssetPath}|base={match.BaseTerrainType}|size={match.Width}x{match.Height}|compressed={match.Compressed}|distinctTiles={match.DistinctTileCount}"
            );
        }
    }

    /// <summary>Print one indexed facade-walk detail by asset path, or search facade-walk assets by path/terrain text.</summary>
    public async Task FacadeWalk([Argument] string gameDir, [Argument] string assetPathOrSearch, int top = 20)
    {
        var workspace = await LoadWorkspace(gameDir);
        IFacadeWalkIndex index = workspace.Index;
        var normalizedInput = NormalizeAssetPathArgument(assetPathOrSearch);

        if (index.FindFacadeWalkDetail(normalizedInput) is { } detail)
        {
            WriteAssetHeader(detail.Asset);
            _output.WriteLine($"Terrain: {detail.Terrain}");
            _output.WriteLine($"Outdoor: {detail.Outdoor}");
            _output.WriteLine($"Flippable: {detail.Flippable}");
            _output.WriteLine($"Width: {detail.Width}");
            _output.WriteLine($"Height: {detail.Height}");
            _output.WriteLine($"EntryCount: {detail.EntryCount}");
            _output.WriteLine($"WalkableEntryCount: {detail.WalkableEntryCount}");
            return;
        }

        var limit = ClampTop(top);
        var matches = index.SearchFacadeWalkDetails(normalizedInput);
        _output.WriteLine($"FacadeWalkMatches: {Math.Min(matches.Count, limit)}/{matches.Count}");

        if (matches.Count == 0)
        {
            _output.WriteLine($"Facade-walk asset not found: {normalizedInput}");
            return;
        }

        foreach (var match in matches.Take(limit))
        {
            _output.WriteLine(
                $"FACADE-WALK|{match.Asset.AssetPath}|terrain={match.Terrain}|size={match.Width}x{match.Height}|entries={match.EntryCount}|walkable={match.WalkableEntryCount}|outdoor={match.Outdoor}|flippable={match.Flippable}"
            );
        }
    }

    private static async Task<EditorWorkspace> LoadWorkspace(
        string gameDir,
        string? saveFolder = null,
        string? saveSlotName = null
    )
    {
        if (string.IsNullOrWhiteSpace(saveFolder) && string.IsNullOrWhiteSpace(saveSlotName))
            return await EditorWorkspaceLoader.LoadFromGameInstallAsync(gameDir);

        return await EditorWorkspaceLoader.LoadFromGameInstallAsync(
            gameDir,
            new EditorWorkspaceLoadOptions { SaveFolder = saveFolder, SaveSlotName = saveSlotName }
        );
    }

    private static int ClampTop(int top) => top < 0 ? 0 : top;

    private static bool MatchesFormat(FileFormat actualFormat, FileFormat? filterFormat) =>
        !filterFormat.HasValue || actualFormat == filterFormat.Value;

    private static bool MatchesValidationSeverity(
        EditorWorkspaceValidationSeverity actualSeverity,
        EditorWorkspaceValidationSeverity? filterSeverity
    ) => !filterSeverity.HasValue || actualSeverity == filterSeverity.Value;

    private static bool MatchesContains(string? value, string? filter) =>
        string.IsNullOrWhiteSpace(filter)
        || (!string.IsNullOrEmpty(value) && value.Contains(filter, StringComparison.OrdinalIgnoreCase));

    private static bool MatchesContains(string? filter, params string?[] values)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return true;

        foreach (var value in values)
        {
            if (!string.IsNullOrEmpty(value) && value.Contains(filter, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool TryGetSchemeSectors(
        ISchemeIndex index,
        string kind,
        int schemeIndex,
        out string normalizedKind,
        out IReadOnlyList<EditorSectorSummary> sectors
    )
    {
        switch (kind.Trim().ToLowerInvariant())
        {
            case "light":
                normalizedKind = "light";
                sectors = index.FindLightSchemeSectors(schemeIndex);
                return true;
            case "music":
                normalizedKind = "music";
                sectors = index.FindMusicSchemeSectors(schemeIndex);
                return true;
            case "ambient":
                normalizedKind = "ambient";
                sectors = index.FindAmbientSchemeSectors(schemeIndex);
                return true;
            default:
                normalizedKind = string.Empty;
                sectors = [];
                return false;
        }
    }

    private void WriteAssetHeader(EditorAssetEntry asset)
    {
        _output.WriteLine($"AssetPath: {asset.AssetPath}");
        _output.WriteLine($"Format: {asset.Format}");
        _output.WriteLine($"ItemCount: {asset.ItemCount}");
        _output.WriteLine($"SourceKind: {asset.SourceKind}");
        _output.WriteLine($"SourcePath: {asset.SourcePath}");
        _output.WriteLine($"SourceEntryPath: {asset.SourceEntryPath ?? "-"}");
    }

    private static string NormalizeAssetPathArgument(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        return text.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
    }

    private static string FormatSectorSummary(EditorSectorSummary sector) =>
        $"SECTOR|{sector.Asset.AssetPath}|map={sector.MapName}|objects={sector.ObjectCount}|lights={sector.LightCount}|tileScripts={sector.TileScriptCount}|sectorScript={sector.SectorScriptId?.ToString(CultureInfo.InvariantCulture) ?? "-"}|blockedTiles={sector.BlockedTileCount}|distinctTileArt={sector.DistinctTileArtCount}|lightScheme={sector.LightSchemeIndex}|musicScheme={sector.MusicSchemeIndex}|ambientScheme={sector.AmbientSchemeIndex}|hasRoofs={sector.HasRoofs}";

    private static string FormatValues<T>(IReadOnlyList<T> values) =>
        values.Count == 0 ? "-" : string.Join(',', values);
}
