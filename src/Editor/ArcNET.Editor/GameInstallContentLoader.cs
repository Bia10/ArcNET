using ArcNET.Archive;
using ArcNET.Formats;
using ArcNET.GameData;

namespace ArcNET.Editor;

internal static class GameInstallContentLoader
{
    private const float ArchiveReadProgressWeight = 0.35f;
    private static readonly FileFormat[] s_supportedFormats =
    [
        FileFormat.Message,
        FileFormat.Sector,
        FileFormat.Proto,
        FileFormat.Mob,
        FileFormat.Art,
        FileFormat.Jmp,
        FileFormat.MapProperties,
        FileFormat.Script,
        FileFormat.Dialog,
        FileFormat.Terrain,
        FileFormat.FacadeWalk,
    ];

    public static async Task<(
        GameDataStore GameData,
        EditorAssetCatalog AssetCatalog,
        EditorWorkspaceLoadReport LoadReport
    )> LoadAsync(
        string gameDir,
        IProgress<float>? progress = null,
        CancellationToken cancellationToken = default,
        IProgress<EditorAssetLoadProgress>? assetProgress = null,
        IProgress<GameDataLoadProgress>? gameDataProgress = null,
        Func<string, DatArchive>? archiveOpener = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameDir);
        if (!Directory.Exists(gameDir))
            throw new DirectoryNotFoundException($"Game directory not found: {gameDir}");

        archiveOpener ??= DatArchive.Open;

        using var installFiles = await ReadInstallFilesAsync(
                gameDir,
                progress,
                cancellationToken,
                assetProgress,
                archiveOpener
            )
            .ConfigureAwait(false);

        var loadResult = await GameDataLoader
            .LoadFromEntriesAsync(
                [.. installFiles.LoadEntries.Values],
                CreateWeightedProgress(progress, ArchiveReadProgressWeight, 1f - ArchiveReadProgressWeight),
                cancellationToken,
                gameDataProgress
            )
            .ConfigureAwait(false);
        AppendSkippedAssets(installFiles, loadResult.Failures);
        var gameData = loadResult.Store;

        var assetCatalog = EditorAssetCatalogBuilder.CreateForInstall(gameData, installFiles.AssetSources);
        return (
            gameData,
            assetCatalog,
            new EditorWorkspaceLoadReport
            {
                SkippedArchiveCandidates = [.. installFiles.SkippedArchiveCandidates],
                SkippedAssets = [.. installFiles.SkippedAssets],
            }
        );
    }

    private static async Task<InstallFileSet> ReadInstallFilesAsync(
        string gameDir,
        IProgress<float>? progress,
        CancellationToken cancellationToken,
        IProgress<EditorAssetLoadProgress>? assetProgress,
        Func<string, DatArchive> archiveOpener
    )
    {
        var files = new InstallFileSet();
        var archiveDiscovery = DiscoverArchivePaths(gameDir, archiveOpener);
        files.SkippedArchiveCandidates.AddRange(archiveDiscovery.SkippedArchiveCandidates);

        var archivePaths = archiveDiscovery.ArchivePaths;
        var looseDataDirectory = Path.Combine(gameDir, "data");
        var hasLooseDirectory = Directory.Exists(looseDataDirectory);
        var totalSources = archivePaths.Count + (hasLooseDirectory ? 1 : 0);

        if (totalSources == 0)
        {
            throw new FileNotFoundException(
                "No DAT archives or loose data directory were found under the supplied game directory.",
                gameDir
            );
        }

        assetProgress?.Report(
            new EditorAssetLoadProgress("Loading install asset sources", 0f, 0, totalSources, "sources")
        );

        var sourceTasks = new List<Task<InstallOverlaySource>>(totalSources);
        foreach (var archivePath in archivePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            sourceTasks.Add(LoadArchiveAsync(archivePath, archiveOpener, cancellationToken));
        }

        if (hasLooseDirectory)
        {
            cancellationToken.ThrowIfCancellationRequested();
            sourceTasks.Add(LoadLooseFilesAsync(looseDataDirectory, cancellationToken));
        }

        var overlaySources = await LoadSourcesAsync(sourceTasks, totalSources, progress, assetProgress)
            .ConfigureAwait(false);
        foreach (var overlaySource in overlaySources)
            ApplyOverlay(files, overlaySource);

        return files;
    }

    private static ArchiveDiscoveryResult DiscoverArchivePaths(string gameDir, Func<string, DatArchive> archiveOpener)
    {
        var paths = new List<string>();
        var archivePaths = new List<string>();
        var skippedArchiveCandidates = new List<EditorSkippedArchiveCandidate>();

        paths.AddRange(EnumerateSortedFiles(gameDir, "*.dat", SearchOption.TopDirectoryOnly));

        var modulesDir = Path.Combine(gameDir, "modules");
        if (Directory.Exists(modulesDir))
        {
            paths.AddRange(EnumerateSortedFiles(modulesDir, "*.dat", SearchOption.AllDirectories));
            paths.AddRange(EnumerateSortedFiles(modulesDir, "*.PATCH*", SearchOption.TopDirectoryOnly));
        }

        foreach (var path in paths)
        {
            if (TryAcceptArchiveCandidate(path, archiveOpener, out var skipReason))
            {
                archivePaths.Add(path);
                continue;
            }

            skippedArchiveCandidates.Add(new EditorSkippedArchiveCandidate { Path = path, Reason = skipReason! });
        }

        return new ArchiveDiscoveryResult(archivePaths, skippedArchiveCandidates);
    }

    private static IReadOnlyList<string> EnumerateSortedFiles(string dir, string pattern, SearchOption searchOption) =>
        Directory
            .EnumerateFiles(dir, pattern, searchOption)
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static bool TryAcceptArchiveCandidate(string archivePath, out string? skipReason) =>
        TryAcceptArchiveCandidate(archivePath, DatArchive.Open, out skipReason);

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

    private static Task<InstallOverlaySource> LoadArchiveAsync(
        string archivePath,
        Func<string, DatArchive> archiveOpener,
        CancellationToken cancellationToken
    ) => Task.Run(() => ReadArchive(archivePath, archiveOpener, cancellationToken), cancellationToken);

    private static InstallOverlaySource ReadArchive(
        string archivePath,
        Func<string, DatArchive> archiveOpener,
        CancellationToken cancellationToken
    )
    {
        var overlay = new InstallOverlaySource();
        var archive = archiveOpener(archivePath);
        try
        {
            overlay.Archive = archive;
            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var format = ResolveSupportedFormat(entry.Path);
                if (!IsSupportedFormat(format))
                    continue;

                var assetPath = NormalizeVirtualPath(entry.Path);
                var archiveEntryPath = entry.Path;
                overlay.LoadEntries[assetPath] = new GameDataLoadEntry(
                    format,
                    assetPath,
                    ct => Task.FromResult(LoadArchiveEntry(archive, archiveEntryPath, ct)),
                    entry.UncompressedSize
                );
                overlay.AssetSources[assetPath] = (EditorAssetSourceKind.DatArchive, archivePath, assetPath);
            }

            return overlay;
        }
        catch
        {
            archive.Dispose();
            throw;
        }
    }

    private static Task<InstallOverlaySource> LoadLooseFilesAsync(
        string looseDataDirectory,
        CancellationToken cancellationToken
    ) => Task.Run(() => ReadLooseFiles(looseDataDirectory, cancellationToken), cancellationToken);

    private static InstallOverlaySource ReadLooseFiles(string looseDataDirectory, CancellationToken cancellationToken)
    {
        var overlay = new InstallOverlaySource();
        foreach (var filePath in Directory.EnumerateFiles(looseDataDirectory, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var format = ResolveSupportedFormat(filePath);
            if (!IsSupportedFormat(format))
                continue;

            var relativePath = Path.GetRelativePath(looseDataDirectory, filePath);
            var assetPath = NormalizeVirtualPath(relativePath);
            overlay.LoadEntries[assetPath] = GameDataLoadEntry.FromFile(format, assetPath, filePath);
            overlay.AssetSources[assetPath] = (EditorAssetSourceKind.LooseFile, filePath, null);
        }

        return overlay;
    }

    private static bool IsSupportedFormat(string path) => IsSupportedFormat(ResolveSupportedFormat(path));

    private static bool IsSupportedFormat(FileFormat format)
    {
        foreach (var supportedFormat in s_supportedFormats)
        {
            if (format == supportedFormat)
                return true;
        }

        return false;
    }

    private static FileFormat ResolveSupportedFormat(string path)
    {
        var format = FileFormatExtensions.FromPath(path);
        if (format != FileFormat.Unknown)
            return format;

        var fileName = Path.GetFileName(ArcNET.Core.VirtualPath.Normalize(path));
        return fileName.StartsWith("facwalk.", StringComparison.OrdinalIgnoreCase)
            ? FileFormat.FacadeWalk
            : FileFormat.Unknown;
    }

    private static void AppendSkippedAssets(InstallFileSet installFiles, IReadOnlyList<GameDataLoadFailure> failures)
    {
        foreach (var failure in failures)
        {
            var assetPath = failure.SourcePath;
            if (installFiles.AssetSources.TryGetValue(assetPath, out var source))
            {
                installFiles.SkippedAssets.Add(
                    new EditorSkippedAsset
                    {
                        AssetPath = assetPath,
                        Format = failure.Format,
                        SourceKind = source.SourceKind,
                        SourcePath = source.SourcePath,
                        SourceEntryPath = source.SourceEntryPath,
                        Reason = failure.Reason,
                    }
                );
            }

            installFiles.LoadEntries.Remove(assetPath);
            installFiles.AssetSources.Remove(assetPath);
        }
    }

    private static async Task<InstallOverlaySource[]> LoadSourcesAsync(
        IReadOnlyList<Task<InstallOverlaySource>> sourceTasks,
        int totalSources,
        IProgress<float>? progress,
        IProgress<EditorAssetLoadProgress>? assetProgress
    )
    {
        if (sourceTasks.Count == 0)
        {
            assetProgress?.Report(new EditorAssetLoadProgress("Loading install asset sources", 1f, 0, 0, "sources"));
            return [];
        }

        var completedSources = 0;
        var trackedTasks = sourceTasks
            .Select(async sourceTask =>
            {
                var overlaySource = await sourceTask.ConfigureAwait(false);
                var completedCount = Interlocked.Increment(ref completedSources);
                var sourceProgress = completedCount / (float)totalSources;
                progress?.Report(sourceProgress * ArchiveReadProgressWeight);
                assetProgress?.Report(
                    new EditorAssetLoadProgress(
                        "Loading install asset sources",
                        sourceProgress,
                        completedCount,
                        totalSources,
                        "sources"
                    )
                );
                return overlaySource;
            })
            .ToArray();

        return await Task.WhenAll(trackedTasks).ConfigureAwait(false);
    }

    private static void ApplyOverlay(InstallFileSet files, InstallOverlaySource overlaySource)
    {
        if (overlaySource.Archive is not null)
            files.Archives.Add(overlaySource.Archive);

        foreach (var (assetPath, loadEntry) in overlaySource.LoadEntries)
            files.LoadEntries[assetPath] = loadEntry;

        foreach (var (assetPath, source) in overlaySource.AssetSources)
            files.AssetSources[assetPath] = source;
    }

    private static ReadOnlyMemory<byte> LoadArchiveEntry(
        DatArchive archive,
        string archiveEntryPath,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        return archive.GetEntryData(archiveEntryPath);
    }

    private static bool IsUnsupportedArchiveFormat(InvalidDataException exception) =>
        exception.Message.StartsWith("Unsupported DAT magic ", StringComparison.Ordinal);

    private static string NormalizeVirtualPath(string path) => ArcNET.Core.VirtualPath.Normalize(path);

    private static IProgress<float>? CreateWeightedProgress(IProgress<float>? progress, float offset, float span)
    {
        if (progress is null)
            return null;

        return new Progress<float>(value => progress.Report(offset + value * span));
    }

    private sealed class InstallFileSet : IDisposable
    {
        public Dictionary<string, GameDataLoadEntry> LoadEntries { get; } = new(StringComparer.OrdinalIgnoreCase);

        public List<DatArchive> Archives { get; } = [];

        public List<EditorSkippedArchiveCandidate> SkippedArchiveCandidates { get; } = [];

        public List<EditorSkippedAsset> SkippedAssets { get; } = [];

        public Dictionary<
            string,
            (EditorAssetSourceKind SourceKind, string SourcePath, string? SourceEntryPath)
        > AssetSources { get; } = new(StringComparer.OrdinalIgnoreCase);

        public void Dispose()
        {
            foreach (var archive in Archives)
                archive.Dispose();
        }
    }

    private sealed class InstallOverlaySource
    {
        public Dictionary<string, GameDataLoadEntry> LoadEntries { get; } = new(StringComparer.OrdinalIgnoreCase);

        public DatArchive? Archive { get; set; }

        public Dictionary<
            string,
            (EditorAssetSourceKind SourceKind, string SourcePath, string? SourceEntryPath)
        > AssetSources { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class ArchiveDiscoveryResult(
        IReadOnlyList<string> archivePaths,
        IReadOnlyList<EditorSkippedArchiveCandidate> skippedArchiveCandidates
    )
    {
        public IReadOnlyList<string> ArchivePaths { get; } = archivePaths;

        public IReadOnlyList<EditorSkippedArchiveCandidate> SkippedArchiveCandidates { get; } =
            skippedArchiveCandidates;
    }
}
