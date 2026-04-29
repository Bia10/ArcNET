using System.Globalization;
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
    /// <summary>Load a live install-backed workspace and print high-level counts plus skip totals.</summary>
    public async Task Summary([Argument] string gameDir, string? saveFolder = null, string? saveSlotName = null)
    {
        var workspace = await LoadWorkspace(gameDir, saveFolder, saveSlotName);
        var validationIssues = workspace.Validation.Issues;

        Console.WriteLine($"GameDirectory: {workspace.GameDirectory}");
        Console.WriteLine($"InstallType: {workspace.InstallationType}");
        Console.WriteLine($"Messages: {workspace.GameData.Messages.Count}");
        Console.WriteLine($"Sectors: {workspace.GameData.Sectors.Count}");
        Console.WriteLine($"Protos: {workspace.GameData.Protos.Count}");
        Console.WriteLine($"Mobs: {workspace.GameData.Mobs.Count}");
        Console.WriteLine($"Scripts: {workspace.GameData.Scripts.Count}");
        Console.WriteLine($"Dialogs: {workspace.GameData.Dialogs.Count}");
        Console.WriteLine($"Assets: {workspace.Assets.Count}");
        Console.WriteLine($"Maps: {workspace.Index.MapNames.Count}");
        Console.WriteLine($"SkippedArchiveCandidates: {workspace.LoadReport.SkippedArchiveCandidates.Count}");
        Console.WriteLine($"SkippedAssets: {workspace.LoadReport.SkippedAssets.Count}");
        Console.WriteLine($"ValidationIssues: {validationIssues.Count}");
        Console.WriteLine(
            $"ValidationErrors: {validationIssues.Count(static issue => issue.Severity == EditorWorkspaceValidationSeverity.Error)}"
        );
        Console.WriteLine(
            $"ValidationWarnings: {validationIssues.Count(static issue => issue.Severity == EditorWorkspaceValidationSeverity.Warning)}"
        );
        Console.WriteLine(
            $"ValidationInfos: {validationIssues.Count(static issue => issue.Severity == EditorWorkspaceValidationSeverity.Info)}"
        );
        Console.WriteLine($"HasSave: {workspace.HasSaveLoaded}");
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
        if (!TryParseFormatFilter(format, out var parsedFormat))
        {
            Console.Error.WriteLine($"Invalid format filter: {format}");
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

        Console.WriteLine(
            $"Filters: format={format ?? "-"}|source={source ?? "-"}|asset={asset ?? "-"}|reason={reason ?? "-"}|top={limit}"
        );

        Console.WriteLine(
            $"SkippedArchiveCandidates: {skippedArchives.Length}/{workspace.LoadReport.SkippedArchiveCandidates.Count}"
        );
        foreach (var skippedArchive in skippedArchives)
            Console.WriteLine($"ARCHIVE|{skippedArchive.Path}|{skippedArchive.Reason}");

        Console.WriteLine($"SkippedAssets: {skippedAssets.Length}/{workspace.LoadReport.SkippedAssets.Count}");
        foreach (var skippedAsset in skippedAssets)
        {
            Console.WriteLine(
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
        if (!TryParseValidationSeverity(severity, out var parsedSeverity))
        {
            Console.Error.WriteLine($"Invalid severity filter: {severity}");
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

        Console.WriteLine($"Filters: severity={severity ?? "-"}|asset={asset ?? "-"}|text={text ?? "-"}|top={limit}");
        Console.WriteLine($"ValidationIssues: {filteredIssues.Length}/{validationIssues.Count}");
        Console.WriteLine(
            $"ValidationErrors: {validationIssues.Count(static issue => issue.Severity == EditorWorkspaceValidationSeverity.Error)}"
        );
        Console.WriteLine(
            $"ValidationWarnings: {validationIssues.Count(static issue => issue.Severity == EditorWorkspaceValidationSeverity.Warning)}"
        );
        Console.WriteLine(
            $"ValidationInfos: {validationIssues.Count(static issue => issue.Severity == EditorWorkspaceValidationSeverity.Info)}"
        );

        foreach (var issue in filteredIssues)
            Console.WriteLine($"{issue.Severity.ToString().ToUpperInvariant()}|{issue.AssetPath}|{issue.Message}");

        return workspace.Validation.HasErrors ? 1 : 0;
    }

    /// <summary>List all map names discovered in the loaded workspace.</summary>
    public async Task Maps([Argument] string gameDir, int top = 50)
    {
        var workspace = await LoadWorkspace(gameDir);
        var limit = ClampTop(top);

        Console.WriteLine($"MapCount: {workspace.Index.MapNames.Count}");
        foreach (var mapName in workspace.Index.MapNames.Take(limit))
            Console.WriteLine(mapName);
    }

    /// <summary>Print all indexed assets that belong to one map directory.</summary>
    public async Task Map([Argument] string gameDir, [Argument] string mapName)
    {
        var workspace = await LoadWorkspace(gameDir);
        var assets = workspace.Index.FindMapAssets(mapName);

        if (assets.Count == 0)
        {
            Console.WriteLine($"Map not found: {mapName}");
            return;
        }

        Console.WriteLine($"Map: {mapName}");
        Console.WriteLine($"AssetCount: {assets.Count}");

        foreach (var group in assets.GroupBy(static asset => asset.Format).OrderBy(static group => group.Key))
            Console.WriteLine($"{group.Key}: {group.Count()}");

        foreach (var asset in assets)
            Console.WriteLine(asset.AssetPath);
    }

    /// <summary>Print all assets that define one message index.</summary>
    public async Task Message([Argument] string gameDir, [Argument] int messageIndex)
    {
        var workspace = await LoadWorkspace(gameDir);
        var assets = workspace.Index.FindMessageAssets(messageIndex);

        Console.WriteLine($"MessageIndex: {messageIndex}");
        Console.WriteLine($"AssetCount: {assets.Count}");
        foreach (var asset in assets)
            Console.WriteLine(asset.AssetPath);
    }

    /// <summary>Print the defining asset and reverse references for one proto number.</summary>
    public async Task Proto([Argument] string gameDir, [Argument] int protoNumber, int top = 20)
    {
        var workspace = await LoadWorkspace(gameDir);
        var definition = workspace.Index.FindProtoDefinition(protoNumber);
        var references = workspace.Index.FindProtoReferences(protoNumber);

        Console.WriteLine($"ProtoNumber: {protoNumber}");
        Console.WriteLine($"Definition: {definition?.AssetPath ?? "<missing>"}");
        Console.WriteLine($"ReferenceAssets: {references.Count}");
        Console.WriteLine($"ReferenceCount: {references.Sum(static reference => reference.Count)}");

        foreach (
            var reference in references
                .OrderByDescending(static reference => reference.Count)
                .ThenBy(static reference => reference.Asset.AssetPath, StringComparer.OrdinalIgnoreCase)
                .Take(ClampTop(top))
        )
            Console.WriteLine($"{reference.Count}|{reference.Asset.AssetPath}|{reference.Format}");
    }

    /// <summary>Print reverse references for one script identifier.</summary>
    public async Task Script([Argument] string gameDir, [Argument] int scriptId, int top = 20)
    {
        var workspace = await LoadWorkspace(gameDir);
        var details = workspace.Index.FindScriptDetails(scriptId);
        var references = workspace.Index.FindScriptReferences(scriptId);
        var limit = ClampTop(top);

        Console.WriteLine($"ScriptId: {scriptId}");
        Console.WriteLine($"DefinitionAssets: {details.Count}");
        foreach (var detail in details.Take(limit))
        {
            Console.WriteLine(
                $"DEF|{detail.Asset.AssetPath}|entries={detail.EntryCount}|active={detail.ActiveAttachmentCount}|slots={FormatValues(detail.ActiveAttachmentSlots)}|points={FormatValues(detail.ActiveAttachmentPoints)}|flags={detail.Flags}|description={detail.Description}"
            );
        }
        Console.WriteLine($"ReferenceAssets: {references.Count}");
        Console.WriteLine($"ReferenceCount: {references.Sum(static reference => reference.Count)}");

        foreach (
            var reference in references
                .OrderByDescending(static reference => reference.Count)
                .ThenBy(static reference => reference.Asset.AssetPath, StringComparer.OrdinalIgnoreCase)
                .Take(limit)
        )
            Console.WriteLine($"{reference.Count}|{reference.Asset.AssetPath}|{reference.Format}");
    }

    /// <summary>Print all dialog assets whose file name starts with one dialog identifier.</summary>
    public async Task Dialog([Argument] string gameDir, [Argument] int dialogId, int top = 20)
    {
        var workspace = await LoadWorkspace(gameDir);
        var details = workspace.Index.FindDialogDetails(dialogId);
        var limit = ClampTop(top);

        Console.WriteLine($"DialogId: {dialogId}");
        Console.WriteLine($"DefinitionAssets: {details.Count}");
        foreach (var detail in details.Take(limit))
        {
            Console.WriteLine(
                $"DEF|{detail.Asset.AssetPath}|entries={detail.EntryCount}|npc={detail.NpcEntryCount}|pc={detail.PcOptionCount}|control={detail.ControlEntryCount}|transitions={detail.TransitionCount}|terminals={detail.TerminalEntryCount}|rootEntries={detail.RootEntryNumbers.Count}|missingTargets={detail.MissingResponseTargetNumbers.Count}"
            );
        }
    }

    /// <summary>Print reverse references for one art resource identifier (decimal or 0x-prefixed hex).</summary>
    public async Task Art([Argument] string gameDir, [Argument] string artId, int top = 20)
    {
        if (!TryParseUInt32(artId, out var parsedArtId))
        {
            Console.Error.WriteLine($"Invalid art ID: {artId}");
            return;
        }

        var workspace = await LoadWorkspace(gameDir);
        var references = workspace.Index.FindArtReferences(parsedArtId);

        Console.WriteLine($"ArtId: 0x{parsedArtId:X8}");
        Console.WriteLine($"ReferenceAssets: {references.Count}");
        Console.WriteLine($"ReferenceCount: {references.Sum(static reference => reference.Count)}");

        foreach (
            var reference in references
                .OrderByDescending(static reference => reference.Count)
                .ThenBy(static reference => reference.Asset.AssetPath, StringComparer.OrdinalIgnoreCase)
                .Take(ClampTop(top))
        )
            Console.WriteLine($"{reference.Count}|{reference.Asset.AssetPath}|{reference.Format}");
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

    private static string FormatValues<T>(IReadOnlyList<T> values) =>
        values.Count == 0 ? "-" : string.Join(',', values);

    private static bool TryParseFormatFilter(string? text, out FileFormat? format)
    {
        format = null;
        if (string.IsNullOrWhiteSpace(text))
            return true;

        var normalized = text.Trim().TrimStart('.');
        if (Enum.TryParse<FileFormat>(normalized, ignoreCase: true, out var parsedFormat))
        {
            format = parsedFormat;
            return true;
        }

        format = normalized.ToLowerInvariant() switch
        {
            "mes" => FileFormat.Message,
            "sec" => FileFormat.Sector,
            "pro" => FileFormat.Proto,
            "mob" => FileFormat.Mob,
            "scr" => FileFormat.Script,
            "dlg" => FileFormat.Dialog,
            _ => null,
        };

        return format.HasValue;
    }

    private static bool TryParseValidationSeverity(string? text, out EditorWorkspaceValidationSeverity? severity)
    {
        severity = null;
        if (string.IsNullOrWhiteSpace(text))
            return true;

        if (Enum.TryParse<EditorWorkspaceValidationSeverity>(text.Trim(), ignoreCase: true, out var parsedSeverity))
        {
            severity = parsedSeverity;
            return true;
        }

        return false;
    }

    private static bool TryParseUInt32(string text, out uint value)
    {
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return uint.TryParse(text[2..], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value);

        return uint.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }
}
