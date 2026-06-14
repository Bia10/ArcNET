using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.Versioning;
using System.Text;
using ArcNET.Archive;
using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;
using ArcNET.Formats;
using ArcNET.GameData.Workspace;

namespace ArcNET.Diagnostics.Windows;

[SupportedOSPlatform("windows")]
internal static class RuntimeWorkspacePathHintResolver
{
    public static string? TryResolveForRunningProcess(int processId)
    {
        if (!ProcessCommandLineReader.TryRead(processId, out var commandLine))
            return null;

        return TryResolveFromCommandLine(commandLine);
    }

    public static string? TryResolveForAttachedProcess(
        int processId,
        string modulePath,
        RuntimeProfileSnapshot runtimeProfile
    )
    {
        var commandLineWorkspacePathHint = TryResolveForRunningProcess(processId);
        if (TryResolveModuleDirectory(commandLineWorkspacePathHint, out var moduleDirectory))
            return moduleDirectory;

        var gameDirectory = ResolveGameDirectory(commandLineWorkspacePathHint, modulePath);
        if (string.IsNullOrWhiteSpace(gameDirectory))
            return commandLineWorkspacePathHint;

        if (WorkspaceInstallPathResolver.TryResolveSingleModuleDirectory(gameDirectory, out var singleModuleDirectory))
            return Path.GetFullPath(singleModuleDirectory);

        if (TryResolveModuleDirectoryByCurrentMapId(processId, runtimeProfile, gameDirectory, out moduleDirectory))
            return moduleDirectory;

        return commandLineWorkspacePathHint;
    }

    private static bool TryResolveModuleDirectoryByCurrentMapId(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        string gameDirectory,
        out string moduleDirectory
    )
    {
        moduleDirectory = string.Empty;
        if (
            !DiagnosticsCapabilityPolicy
                .Create(runtimeProfile, hasModuleSymbols: false)
                .Capabilities.HasFlag(DiagnosticsCapability.InvokeFunctions)
        )
        {
            return false;
        }

        try
        {
            using var memory = ProcessMemory.Attach(processId);
            if (
                !RuntimeActionInvoker.TryReadCurrentMapId(
                    memory,
                    runtimeProfile,
                    CurrentMapTimeout,
                    out var currentMapId
                )
            )
                return false;

            return TryResolveModuleDirectoryByMapId(gameDirectory, currentMapId, out moduleDirectory);
        }
        catch
        {
            return false;
        }
    }

    internal static bool TryResolveModuleDirectoryByMapId(string gameDirectory, int mapId, out string moduleDirectory)
    {
        var matches = EnumerateCandidateModuleDirectories(gameDirectory)
            .Where(modulePath => ModuleMapCatalogContainsMapId(modulePath, mapId))
            .Take(2)
            .ToArray();
        if (matches.Length != 1)
        {
            moduleDirectory = string.Empty;
            return false;
        }

        moduleDirectory = matches[0];
        return true;
    }

    private static bool ModuleMapCatalogContainsMapId(string moduleDirectory, int mapId)
    {
        var normalizedModuleDirectory = Path.GetFullPath(moduleDirectory);
        var mapIds = s_moduleMapIds.GetOrAdd(normalizedModuleDirectory, LoadEffectiveMapIds);
        return mapIds.Contains(mapId);
    }

    private static HashSet<int> LoadEffectiveMapIds(string moduleDirectory)
    {
        var mapList = TryLoadEffectiveMapList(moduleDirectory);
        if (mapList is null)
            return [];

        HashSet<int> mapIds = [];
        foreach (var entry in mapList.Entries)
        {
            if (LooksLikeMapListEntry(entry))
                mapIds.Add(entry.Index);
        }

        return mapIds;
    }

    private static MesFile? TryLoadEffectiveMapList(string moduleDirectory)
    {
        var moduleLooseMapListPath = Path.Combine(moduleDirectory, "Rules", "MapList.mes");
        if (File.Exists(moduleLooseMapListPath))
            return MessageFormat.ParseFile(moduleLooseMapListPath);

        foreach (
            var archivePath in WorkspaceArchiveDiscovery.DiscoverModuleArchives(moduleDirectory).ArchivePaths.Reverse()
        )
        {
            if (TryLoadMessageFileFromArchive(archivePath, MapListAssetPath, out var mapList))
                return mapList;
        }

        var gameDirectory = WorkspaceInstallPathResolver.ResolveOwningGameDirectoryFromModuleDirectory(moduleDirectory);
        var baseLooseMapListPath = Path.Combine(gameDirectory, "data", "Rules", "MapList.mes");
        if (File.Exists(baseLooseMapListPath))
            return MessageFormat.ParseFile(baseLooseMapListPath);

        foreach (
            var archivePath in WorkspaceArchiveDiscovery
                .DiscoverBaseInstallArchives(gameDirectory)
                .ArchivePaths.Reverse()
        )
        {
            if (TryLoadMessageFileFromArchive(archivePath, MapListAssetPath, out var mapList))
                return mapList;
        }

        return null;
    }

    private static bool TryLoadMessageFileFromArchive(string archivePath, string assetPath, out MesFile? file)
    {
        using var archive = DatArchive.Open(archivePath);
        if (archive.FindEntry(assetPath) is null)
        {
            file = null;
            return false;
        }

        file = MessageFormat.ParseMemory(archive.GetEntryData(assetPath));
        return true;
    }

    private static bool LooksLikeMapListEntry(MessageEntry entry)
    {
        var segments = entry.Text.Split(',', StringSplitOptions.TrimEntries);
        return segments.Length >= 3
            && int.TryParse(segments[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out _)
            && int.TryParse(segments[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
    }

    private static IReadOnlyList<string> EnumerateCandidateModuleDirectories(string gameDirectory)
    {
        var normalizedGameDirectory = WorkspaceInstallPathResolver.ResolveGameInstallDirectory(gameDirectory);
        var modulesDirectory = Path.Combine(normalizedGameDirectory, "modules");
        if (!Directory.Exists(modulesDirectory))
            return [];

        HashSet<string> candidateDirectories = new(StringComparer.OrdinalIgnoreCase);
        foreach (
            var candidateDirectory in Directory.EnumerateDirectories(
                modulesDirectory,
                "*",
                SearchOption.TopDirectoryOnly
            )
        )
            candidateDirectories.Add(Path.GetFullPath(candidateDirectory));

        foreach (var archivePath in Directory.EnumerateFiles(modulesDirectory, "*", SearchOption.TopDirectoryOnly))
        {
            if (TryParseModuleArchiveName(Path.GetFileName(archivePath), out var moduleName))
                candidateDirectories.Add(Path.Combine(modulesDirectory, moduleName));
        }

        return
        [
            .. candidateDirectories
                .Where(WorkspaceContentLoader.HasModuleContent)
                .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase),
        ];
    }

    private static bool TryParseModuleArchiveName(string fileName, out string moduleName)
    {
        if (fileName.EndsWith(".dat", StringComparison.OrdinalIgnoreCase))
        {
            moduleName = Path.GetFileNameWithoutExtension(fileName);
            return !string.IsNullOrWhiteSpace(moduleName);
        }

        var patchIndex = fileName.IndexOf(".PATCH", StringComparison.OrdinalIgnoreCase);
        if (patchIndex > 0)
        {
            moduleName = fileName[..patchIndex];
            return true;
        }

        moduleName = string.Empty;
        return false;
    }

    private static string? TryResolveFromCommandLine(string? commandLine)
    {
        foreach (var candidatePath in EnumerateCommandLinePathCandidates(commandLine))
        {
            var workspacePath = NormalizeWorkspacePathHint(candidatePath);
            if (!string.IsNullOrWhiteSpace(workspacePath))
                return workspacePath;
        }

        return null;
    }

    private static IEnumerable<string> EnumerateCommandLinePathCandidates(string? commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine))
            yield break;

        var skipExecutableToken = true;
        foreach (var token in TokenizeCommandLine(commandLine))
        {
            if (skipExecutableToken)
            {
                skipExecutableToken = false;
                continue;
            }

            if (TryGetPathCandidate(token, out var candidatePath))
                yield return candidatePath;
        }
    }

    private static bool TryGetPathCandidate(string token, out string candidatePath)
    {
        candidatePath = string.Empty;
        var trimmedToken = token.Trim().Trim('"').Trim(',', ';');
        if (TryGetRootedPathCandidate(trimmedToken, out candidatePath))
            return true;

        var optionValue = ExtractOptionValue(trimmedToken);
        return TryGetRootedPathCandidate(optionValue, out candidatePath);
    }

    private static bool TryGetRootedPathCandidate(string? token, out string candidatePath)
    {
        candidatePath = string.Empty;
        if (string.IsNullOrWhiteSpace(token))
            return false;

        var trimmedToken = token.Trim().Trim('"');
        if (!Path.IsPathRooted(trimmedToken))
            return false;

        candidatePath = trimmedToken;
        return true;
    }

    private static string? ExtractOptionValue(string token)
    {
        if (token.Length == 0)
            return null;

        var equalsIndex = token.IndexOf('=');
        if (equalsIndex > 0 && equalsIndex < token.Length - 1)
            return token[(equalsIndex + 1)..];

        if (token[0] is not ('-' or '/'))
            return null;

        var colonIndex = token.IndexOf(':', 1);
        return colonIndex > 1 && colonIndex < token.Length - 1 ? token[(colonIndex + 1)..] : null;
    }

    private static IEnumerable<string> TokenizeCommandLine(string commandLine)
    {
        StringBuilder buffer = new();
        var insideQuotes = false;
        foreach (var ch in commandLine)
        {
            if (ch == '"')
            {
                insideQuotes = !insideQuotes;
                continue;
            }

            if (char.IsWhiteSpace(ch) && !insideQuotes)
            {
                if (buffer.Length == 0)
                    continue;

                yield return buffer.ToString();
                buffer.Clear();
                continue;
            }

            buffer.Append(ch);
        }

        if (buffer.Length > 0)
            yield return buffer.ToString();
    }

    private static string? ResolveGameDirectory(string? workspacePathHint, string modulePath)
    {
        var candidatePath = workspacePathHint;
        if (string.IsNullOrWhiteSpace(candidatePath))
            candidatePath = NormalizeWorkspacePathHint(modulePath);

        return string.IsNullOrWhiteSpace(candidatePath)
            ? null
            : Path.GetFullPath(WorkspaceInstallPathResolver.ResolveGameInstallDirectory(candidatePath));
    }

    private static bool TryResolveModuleDirectory(string? workspacePath, out string moduleDirectory)
    {
        moduleDirectory = string.Empty;
        if (string.IsNullOrWhiteSpace(workspacePath))
            return false;

        var normalizedWorkspacePath = Path.GetFullPath(workspacePath);
        if (
            !WorkspaceInstallPathResolver.TryResolveOwningGameDirectoryFromModuleDirectory(
                normalizedWorkspacePath,
                out _
            )
        )
        {
            return false;
        }

        moduleDirectory = normalizedWorkspacePath;
        return true;
    }

    private static string? NormalizeWorkspacePathHint(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        var resolvedWorkspacePath = Path.GetFullPath(
            WorkspaceInstallPathResolver.ResolveWorkspaceDirectory(path.Trim())
        );
        if (WorkspaceInstallPathResolver.TryResolveOwningGameDirectoryFromModuleDirectory(resolvedWorkspacePath, out _))
            return resolvedWorkspacePath;

        var gameDirectory = Path.GetFullPath(
            WorkspaceInstallPathResolver.ResolveGameInstallDirectory(resolvedWorkspacePath)
        );
        if (WorkspaceInstallPathResolver.TryResolveSingleModuleDirectory(gameDirectory, out var moduleDirectory))
            return Path.GetFullPath(moduleDirectory);

        return resolvedWorkspacePath.Length == 0 ? null : resolvedWorkspacePath;
    }

    private static readonly ConcurrentDictionary<string, HashSet<int>> s_moduleMapIds = new(
        StringComparer.OrdinalIgnoreCase
    );
    private static readonly TimeSpan CurrentMapTimeout = TimeSpan.FromMilliseconds(250);
    private const string MapListAssetPath = "Rules/MapList.mes";
}
