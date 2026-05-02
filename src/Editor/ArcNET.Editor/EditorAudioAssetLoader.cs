using ArcNET.Archive;

namespace ArcNET.Editor;

internal static class EditorAudioAssetLoader
{
    public static async Task<EditorAudioAssetLoadResult> LoadFromContentDirectoryAsync(
        string contentDirectory,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentDirectory);
        if (!Directory.Exists(contentDirectory))
            throw new DirectoryNotFoundException($"Content directory not found: {contentDirectory}");

        var entries = new List<EditorAudioAssetEntry>();
        var dataByPath = new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.OrdinalIgnoreCase);

        foreach (var filePath in Directory.EnumerateFiles(contentDirectory, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsSupportedAudioAsset(filePath))
                continue;

            var assetPath = NormalizeVirtualPath(Path.GetRelativePath(contentDirectory, filePath));
            var bytes = await File.ReadAllBytesAsync(filePath, cancellationToken).ConfigureAwait(false);
            dataByPath[assetPath] = bytes;
            entries.Add(
                new EditorAudioAssetEntry
                {
                    AssetPath = assetPath,
                    SourceKind = EditorAssetSourceKind.LooseFile,
                    SourcePath = filePath,
                    SourceEntryPath = null,
                    ByteLength = bytes.Length,
                }
            );
        }

        return new EditorAudioAssetLoadResult(EditorAudioAssetCatalog.Create(entries), dataByPath);
    }

    public static Task<EditorAudioAssetLoadResult> LoadFromGameInstallAsync(
        string gameDir,
        CancellationToken cancellationToken = default
    ) => Task.Run(() => LoadFromGameInstall(gameDir, cancellationToken), cancellationToken);

    public static Task<EditorAudioAssetLoadResult> LoadFromModuleDirectoryAsync(
        string moduleDirectory,
        CancellationToken cancellationToken = default
    ) => Task.Run(() => LoadFromModuleDirectory(moduleDirectory, cancellationToken), cancellationToken);

    private static EditorAudioAssetLoadResult LoadFromGameInstall(string gameDir, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameDir);
        if (!Directory.Exists(gameDir))
            throw new DirectoryNotFoundException($"Game directory not found: {gameDir}");

        var dataByPath = new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.OrdinalIgnoreCase);
        var sourceByPath = new Dictionary<
            string,
            (EditorAssetSourceKind SourceKind, string SourcePath, string? SourceEntryPath)
        >(StringComparer.OrdinalIgnoreCase);

        foreach (var archivePath in DiscoverArchivePaths(gameDir))
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var archive = DatArchive.Open(archivePath);
            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!IsSupportedAudioAsset(entry.Path))
                    continue;

                var assetPath = NormalizeVirtualPath(entry.Path);
                dataByPath[assetPath] = archive.GetEntryData(entry.Path);
                sourceByPath[assetPath] = (EditorAssetSourceKind.DatArchive, archivePath, assetPath);
            }
        }

        var looseDataDirectory = Path.Combine(gameDir, "data");
        if (Directory.Exists(looseDataDirectory))
        {
            foreach (var filePath in Directory.EnumerateFiles(looseDataDirectory, "*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!IsSupportedAudioAsset(filePath))
                    continue;

                var assetPath = NormalizeVirtualPath(Path.GetRelativePath(looseDataDirectory, filePath));
                var bytes = File.ReadAllBytes(filePath);
                dataByPath[assetPath] = bytes;
                sourceByPath[assetPath] = (EditorAssetSourceKind.LooseFile, filePath, null);
            }
        }

        var entries = dataByPath
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair =>
            {
                var source = sourceByPath[pair.Key];
                return new EditorAudioAssetEntry
                {
                    AssetPath = pair.Key,
                    SourceKind = source.SourceKind,
                    SourcePath = source.SourcePath,
                    SourceEntryPath = source.SourceEntryPath,
                    ByteLength = pair.Value.Length,
                };
            });

        return new EditorAudioAssetLoadResult(EditorAudioAssetCatalog.Create(entries), dataByPath);
    }

    private static EditorAudioAssetLoadResult LoadFromModuleDirectory(
        string moduleDirectory,
        CancellationToken cancellationToken
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleDirectory);
        if (!Directory.Exists(moduleDirectory))
            throw new DirectoryNotFoundException($"Module directory not found: {moduleDirectory}");

        var dataByPath = new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.OrdinalIgnoreCase);
        var sourceByPath = new Dictionary<
            string,
            (EditorAssetSourceKind SourceKind, string SourcePath, string? SourceEntryPath)
        >(StringComparer.OrdinalIgnoreCase);

        foreach (var archivePath in DiscoverModuleArchivePaths(moduleDirectory))
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var archive = DatArchive.Open(archivePath);
            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!IsSupportedAudioAsset(entry.Path))
                    continue;

                var assetPath = NormalizeVirtualPath(entry.Path);
                dataByPath[assetPath] = archive.GetEntryData(entry.Path);
                sourceByPath[assetPath] = (EditorAssetSourceKind.DatArchive, archivePath, assetPath);
            }
        }

        foreach (var filePath in Directory.EnumerateFiles(moduleDirectory, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (
                filePath.Contains(
                    $"{Path.DirectorySeparatorChar}Save{Path.DirectorySeparatorChar}",
                    StringComparison.OrdinalIgnoreCase
                )
            )
                continue;

            if (!IsSupportedAudioAsset(filePath))
                continue;

            var assetPath = NormalizeVirtualPath(Path.GetRelativePath(moduleDirectory, filePath));
            var bytes = File.ReadAllBytes(filePath);
            dataByPath[assetPath] = bytes;
            sourceByPath[assetPath] = (EditorAssetSourceKind.LooseFile, filePath, null);
        }

        var entries = dataByPath
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair =>
            {
                var source = sourceByPath[pair.Key];
                return new EditorAudioAssetEntry
                {
                    AssetPath = pair.Key,
                    SourceKind = source.SourceKind,
                    SourcePath = source.SourcePath,
                    SourceEntryPath = source.SourceEntryPath,
                    ByteLength = pair.Value.Length,
                };
            });

        return new EditorAudioAssetLoadResult(EditorAudioAssetCatalog.Create(entries), dataByPath);
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

    private static bool IsUnsupportedArchiveFormat(InvalidDataException exception) =>
        exception.Message.StartsWith("Unsupported DAT magic ", StringComparison.Ordinal);

    private static string NormalizeVirtualPath(string path) =>
        path.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');

    internal sealed class EditorAudioAssetLoadResult(
        EditorAudioAssetCatalog catalog,
        IReadOnlyDictionary<string, ReadOnlyMemory<byte>> dataByPath
    )
    {
        public EditorAudioAssetCatalog Catalog { get; } = catalog;

        public IReadOnlyDictionary<string, ReadOnlyMemory<byte>> DataByPath { get; } = dataByPath;
    }
}
