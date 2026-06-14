using ArcNET.Archive;

namespace ArcNET.GameData.Workspace;

public static class WorkspaceArchiveDiscovery
{
    public static WorkspaceArchiveDiscoveryResult DiscoverGameInstallArchives(
        string gameDirectory,
        Func<string, DatArchive>? archiveOpener = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameDirectory);

        archiveOpener ??= DatArchive.Open;
        List<string> candidates = [.. EnumerateSortedFiles(gameDirectory, "*.dat", SearchOption.TopDirectoryOnly)];

        var modulesDirectory = Path.Combine(gameDirectory, "modules");
        if (Directory.Exists(modulesDirectory))
        {
            candidates.AddRange(EnumerateSortedFiles(modulesDirectory, "*.dat", SearchOption.AllDirectories));
            candidates.AddRange(EnumerateSortedFiles(modulesDirectory, "*.PATCH*", SearchOption.TopDirectoryOnly));
        }

        return CreateResult(candidates, archiveOpener);
    }

    public static WorkspaceArchiveDiscoveryResult DiscoverBaseInstallArchives(
        string? gameDirectory,
        Func<string, DatArchive>? archiveOpener = null
    )
    {
        if (string.IsNullOrWhiteSpace(gameDirectory) || !Directory.Exists(gameDirectory))
            return WorkspaceArchiveDiscoveryResult.Empty;

        archiveOpener ??= DatArchive.Open;
        return CreateResult(EnumerateSortedFiles(gameDirectory, "*.dat", SearchOption.TopDirectoryOnly), archiveOpener);
    }

    public static WorkspaceArchiveDiscoveryResult DiscoverModuleArchives(
        string moduleDirectory,
        Func<string, DatArchive>? archiveOpener = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleDirectory);

        var moduleName = Path.GetFileName(
            moduleDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
        );
        var modulesRoot = Directory.GetParent(moduleDirectory)?.FullName;
        if (modulesRoot is null || !Directory.Exists(modulesRoot))
            return WorkspaceArchiveDiscoveryResult.Empty;

        archiveOpener ??= DatArchive.Open;
        return CreateResult(
            Directory
                .EnumerateFiles(modulesRoot, "*", SearchOption.TopDirectoryOnly)
                .Where(path => IsModuleArchiveCandidate(path, moduleName))
                .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase),
            archiveOpener
        );
    }

    private static WorkspaceArchiveDiscoveryResult CreateResult(
        IEnumerable<string> candidates,
        Func<string, DatArchive> archiveOpener
    )
    {
        List<string> archivePaths = [];
        List<WorkspaceSkippedArchiveCandidate> skippedArchiveCandidates = [];
        foreach (var path in candidates)
        {
            if (TryAcceptArchiveCandidate(path, archiveOpener, out var skipReason))
            {
                archivePaths.Add(path);
                continue;
            }

            skippedArchiveCandidates.Add(new WorkspaceSkippedArchiveCandidate { Path = path, Reason = skipReason! });
        }

        return archivePaths.Count == 0 && skippedArchiveCandidates.Count == 0
            ? WorkspaceArchiveDiscoveryResult.Empty
            : new WorkspaceArchiveDiscoveryResult(archivePaths, skippedArchiveCandidates);
    }

    private static IReadOnlyList<string> EnumerateSortedFiles(string dir, string pattern, SearchOption searchOption) =>
        Directory
            .EnumerateFiles(dir, pattern, searchOption)
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static bool IsModuleArchiveCandidate(string path, string moduleName)
    {
        var fileName = Path.GetFileName(path);
        return fileName.Equals($"{moduleName}.dat", StringComparison.OrdinalIgnoreCase)
            || fileName.StartsWith($"{moduleName}.PATCH", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryAcceptArchiveCandidate(
        string archivePath,
        Func<string, DatArchive> archiveOpener,
        out string? skipReason
    )
    {
        try
        {
            using var archive = archiveOpener(archivePath);
            skipReason = null;
            return true;
        }
        catch (InvalidDataException ex) when (IsUnsupportedArchiveFormat(ex))
        {
            skipReason = ex.Message;
            return false;
        }
        catch (ArgumentOutOfRangeException)
        {
            skipReason = "Archive was too short or malformed to contain a valid DAT directory.";
            return false;
        }
    }

    private static bool IsUnsupportedArchiveFormat(InvalidDataException exception) =>
        exception.Message.StartsWith("Unsupported DAT magic ", StringComparison.Ordinal);
}
