using ArcNET.Archive;

namespace ArcNET.Editor;

internal static class EditorAudioAssetLoader
{
    public static async Task<EditorAudioAssetLoadResult> LoadFromContentDirectoryAsync(
        string contentDirectory,
        CancellationToken cancellationToken = default,
        IProgress<EditorAssetLoadProgress>? progress = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentDirectory);
        if (!Directory.Exists(contentDirectory))
            throw new DirectoryNotFoundException($"Content directory not found: {contentDirectory}");

        var aggregator = new ProgressAggregator(progress);
        var overlaySource = await LoadLooseFilesAsync(
                contentDirectory,
                skipSaveDirectory: false,
                cancellationToken,
                aggregator.CreateSubProgress("looseFiles"),
                "Loading audio assets"
            )
            .ConfigureAwait(false);

        return CreateLoadResult([overlaySource]);
    }

    public static async Task<EditorAudioAssetLoadResult> LoadFromGameInstallAsync(
        string gameDir,
        CancellationToken cancellationToken = default,
        IProgress<EditorAssetLoadProgress>? progress = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameDir);
        if (!Directory.Exists(gameDir))
            throw new DirectoryNotFoundException($"Game directory not found: {gameDir}");

        var aggregator = new ProgressAggregator(progress);
        var sourceTasks = DiscoverArchivePaths(gameDir)
            .Select(archivePath =>
                LoadArchiveAsync(archivePath, cancellationToken, aggregator.CreateSubProgress(archivePath))
            )
            .ToList();

        var looseDataDirectory = Path.Combine(gameDir, "data");
        if (Directory.Exists(looseDataDirectory))
            sourceTasks.Add(
                LoadLooseFilesAsync(
                    looseDataDirectory,
                    skipSaveDirectory: false,
                    cancellationToken,
                    aggregator.CreateSubProgress("looseFiles")
                )
            );

        var overlaySources = await Task.WhenAll(sourceTasks).ConfigureAwait(false);
        return CreateLoadResult(overlaySources);
    }

    public static async Task<EditorAudioAssetLoadResult> LoadFromModuleDirectoryAsync(
        string moduleDirectory,
        CancellationToken cancellationToken = default,
        IProgress<EditorAssetLoadProgress>? progress = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleDirectory);
        if (!Directory.Exists(moduleDirectory))
            throw new DirectoryNotFoundException($"Module directory not found: {moduleDirectory}");

        var aggregator = new ProgressAggregator(progress);
        var sourceTasks = new List<Task<AudioOverlaySource>>();

        var gameDirectory = ResolveOwningGameDirectory(moduleDirectory);
        if (gameDirectory is not null)
        {
            sourceTasks.AddRange(
                DiscoverArchivePaths(gameDirectory)
                    .Select(archivePath =>
                        LoadArchiveAsync(archivePath, cancellationToken, aggregator.CreateSubProgress(archivePath))
                    )
            );

            var looseDataDirectory = Path.Combine(gameDirectory, "data");
            if (Directory.Exists(looseDataDirectory))
                sourceTasks.Add(
                    LoadLooseFilesAsync(
                        looseDataDirectory,
                        skipSaveDirectory: false,
                        cancellationToken,
                        aggregator.CreateSubProgress("looseFiles")
                    )
                );
        }

        sourceTasks.AddRange(
            DiscoverModuleArchivePaths(moduleDirectory)
                .Select(archivePath =>
                    LoadArchiveAsync(archivePath, cancellationToken, aggregator.CreateSubProgress(archivePath))
                )
        );
        sourceTasks.Add(
            LoadLooseFilesAsync(
                moduleDirectory,
                skipSaveDirectory: true,
                cancellationToken,
                aggregator.CreateSubProgress("looseFiles")
            )
        );

        var overlaySources = await Task.WhenAll(sourceTasks).ConfigureAwait(false);
        return CreateLoadResult(overlaySources);
    }

    private static Task<AudioOverlaySource> LoadArchiveAsync(
        string archivePath,
        CancellationToken cancellationToken,
        IProgress<EditorAssetLoadProgress>? progress = null
    ) => Task.Run(() => ReadArchive(archivePath, cancellationToken, progress), cancellationToken);

    private static AudioOverlaySource ReadArchive(
        string archivePath,
        CancellationToken cancellationToken,
        IProgress<EditorAssetLoadProgress>? progress = null
    )
    {
        var overlaySource = new AudioOverlaySource();
        using var archive = DatArchive.Open(archivePath);

        var audioEntries = archive.Entries.Where(e => IsSupportedAudioAsset(e.Path)).ToArray();

        if (audioEntries.Length == 0)
            return overlaySource;

        var completedCount = 0;
        var archiveName = Path.GetFileName(archivePath);

        progress?.Report(
            new EditorAssetLoadProgress($"Reading {archiveName}", 0f, 0, audioEntries.Length, "audio files")
        );

        Parallel.ForEach(
            audioEntries,
            new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = Environment.ProcessorCount,
            },
            entry =>
            {
                var assetPath = NormalizeVirtualPath(entry.Path);

                lock (overlaySource)
                {
                    overlaySource.EntriesByPath[assetPath] = new EditorAudioAssetEntry
                    {
                        AssetPath = assetPath,
                        SourceKind = EditorAssetSourceKind.DatArchive,
                        SourcePath = archivePath,
                        SourceEntryPath = assetPath,
                        ByteLength = entry.UncompressedSize,
                    };
                }

                var completed = Interlocked.Increment(ref completedCount);
                progress?.Report(
                    new EditorAssetLoadProgress(
                        $"Reading {archiveName}",
                        completed / (float)audioEntries.Length,
                        completed,
                        audioEntries.Length,
                        "audio files"
                    )
                );
            }
        );

        progress?.Report(
            new EditorAssetLoadProgress(
                $"Reading {archiveName}",
                1f,
                audioEntries.Length,
                audioEntries.Length,
                "audio files"
            )
        );

        return overlaySource;
    }

    private static Task<AudioOverlaySource> LoadLooseFilesAsync(
        string rootDirectory,
        bool skipSaveDirectory,
        CancellationToken cancellationToken,
        IProgress<EditorAssetLoadProgress>? progress = null,
        string activity = "Loading audio assets"
    )
    {
        var filePaths = Directory
            .EnumerateFiles(rootDirectory, "*", SearchOption.AllDirectories)
            .Where(filePath => !skipSaveDirectory || !IsSaveFilePath(filePath))
            .Where(IsSupportedAudioAsset)
            .ToArray();
        var completedCount = 0;

        progress?.Report(new EditorAssetLoadProgress(activity, 0f, 0, filePaths.Length, "audio files"));

        var overlaySource = new AudioOverlaySource();
        for (var index = 0; index < filePaths.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var filePath = filePaths[index];
            var assetPath = NormalizeVirtualPath(Path.GetRelativePath(rootDirectory, filePath));
            overlaySource.EntriesByPath[assetPath] = new EditorAudioAssetEntry
            {
                AssetPath = assetPath,
                SourceKind = EditorAssetSourceKind.LooseFile,
                SourcePath = filePath,
                SourceEntryPath = null,
                ByteLength = checked((int)new FileInfo(filePath).Length),
            };

            var loadedCount = Interlocked.Increment(ref completedCount);
            progress?.Report(
                new EditorAssetLoadProgress(
                    activity,
                    loadedCount / (float)Math.Max(filePaths.Length, 1),
                    loadedCount,
                    filePaths.Length,
                    "audio files"
                )
            );
        }

        progress?.Report(new EditorAssetLoadProgress(activity, 1f, filePaths.Length, filePaths.Length, "audio files"));

        return Task.FromResult(overlaySource);
    }

    private static EditorAudioAssetLoadResult CreateLoadResult(IReadOnlyList<AudioOverlaySource> overlaySources)
    {
        var entriesByPath = new Dictionary<string, EditorAudioAssetEntry>(StringComparer.OrdinalIgnoreCase);

        foreach (var overlaySource in overlaySources)
        {
            foreach (var (assetPath, entry) in overlaySource.EntriesByPath)
                entriesByPath[assetPath] = entry;
        }

        return new EditorAudioAssetLoadResult(EditorAudioAssetCatalog.Create(entriesByPath.Values));
    }

    private static string? ResolveOwningGameDirectory(string moduleDirectory)
    {
        var modulesRoot = Directory.GetParent(moduleDirectory)?.FullName;
        return modulesRoot is null ? null : Directory.GetParent(modulesRoot)?.FullName;
    }

    private static IReadOnlyList<string> DiscoverArchivePaths(string gameDir)
    {
        var paths = new List<string>();

        paths.AddRange(EnumerateSortedFiles(gameDir, "*.dat", SearchOption.TopDirectoryOnly));

        var modulesDir = Path.Combine(gameDir, "modules");
        if (Directory.Exists(modulesDir))
        {
            paths.AddRange(EnumerateSortedFiles(modulesDir, "*.dat", SearchOption.AllDirectories));
            paths.AddRange(EnumerateSortedFiles(modulesDir, "*.PATCH*", SearchOption.TopDirectoryOnly));
        }

        var archivePaths = new List<string>();
        foreach (var path in paths)
        {
            if (TryAcceptArchiveCandidate(path))
                archivePaths.Add(path);
        }

        return archivePaths;
    }

    private static IReadOnlyList<string> DiscoverModuleArchivePaths(string moduleDirectory)
    {
        var moduleName = Path.GetFileName(
            moduleDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
        );
        var modulesRoot = Directory.GetParent(moduleDirectory)?.FullName;
        if (modulesRoot is null || !Directory.Exists(modulesRoot))
            return [];

        var archivePaths = new List<string>();
        foreach (
            var path in Directory
                .EnumerateFiles(modulesRoot, "*", SearchOption.TopDirectoryOnly)
                .Where(path => IsModuleArchiveCandidate(path, moduleName))
                .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
        )
        {
            if (TryAcceptArchiveCandidate(path))
                archivePaths.Add(path);
        }

        return archivePaths;
    }

    private static bool IsModuleArchiveCandidate(string path, string moduleName)
    {
        var fileName = Path.GetFileName(path);
        return fileName.Equals($"{moduleName}.dat", StringComparison.OrdinalIgnoreCase)
            || fileName.StartsWith($"{moduleName}.PATCH", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<string> EnumerateSortedFiles(string dir, string pattern, SearchOption searchOption) =>
        Directory
            .EnumerateFiles(dir, pattern, searchOption)
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static bool TryAcceptArchiveCandidate(string archivePath)
    {
        try
        {
            using var archive = DatArchive.Open(archivePath);
            return true;
        }
        catch (InvalidDataException ex) when (IsUnsupportedArchiveFormat(ex))
        {
            return false;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private static bool IsSupportedAudioAsset(string path) => path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase);

    private static bool IsSaveFilePath(string filePath) =>
        filePath.Contains(
            $"{Path.DirectorySeparatorChar}Save{Path.DirectorySeparatorChar}",
            StringComparison.OrdinalIgnoreCase
        );

    private static bool IsUnsupportedArchiveFormat(InvalidDataException exception) =>
        exception.Message.StartsWith("Unsupported DAT magic ", StringComparison.Ordinal);

    private static string NormalizeVirtualPath(string path) => ArcNET.Core.VirtualPath.Normalize(path);

    private sealed class AudioOverlaySource
    {
        public Dictionary<string, EditorAudioAssetEntry> EntriesByPath { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    internal sealed class EditorAudioAssetLoadResult(EditorAudioAssetCatalog catalog)
    {
        public EditorAudioAssetCatalog Catalog { get; } = catalog;
    }

    private sealed class ProgressAggregator(
        IProgress<EditorAssetLoadProgress>? destination,
        string activity = "Loading audio assets"
    )
    {
        private readonly object _gate = new();
        private readonly Dictionary<string, (int Completed, int Total)> _progressMap = [];

        public IProgress<EditorAssetLoadProgress> CreateSubProgress(string key)
        {
            lock (_gate)
            {
                _progressMap[key] = (0, 0);
            }
            return new DirectProgress(update =>
            {
                lock (_gate)
                {
                    _progressMap[key] = (update.CompletedUnits ?? 0, update.TotalUnits ?? 0);

                    var totalCompleted = 0;
                    var totalUnits = 0;
                    foreach (var pair in _progressMap.Values)
                    {
                        totalCompleted += pair.Completed;
                        totalUnits += pair.Total;
                    }

                    var overallProgress = totalUnits > 0 ? (float)totalCompleted / totalUnits : 1f;
                    destination?.Report(
                        new EditorAssetLoadProgress(
                            activity,
                            overallProgress,
                            totalCompleted,
                            totalUnits,
                            "audio files"
                        )
                    );
                }
            });
        }

        private sealed class DirectProgress(Action<EditorAssetLoadProgress> report) : IProgress<EditorAssetLoadProgress>
        {
            public void Report(EditorAssetLoadProgress value) => report(value);
        }
    }
}
