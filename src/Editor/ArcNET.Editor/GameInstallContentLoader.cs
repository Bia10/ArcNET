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
    )> LoadAsync(string gameDir, IProgress<float>? progress = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameDir);
        if (!Directory.Exists(gameDir))
            throw new DirectoryNotFoundException($"Game directory not found: {gameDir}");

        var installFiles = await Task.Run(
                () => ReadInstallFiles(gameDir, progress, cancellationToken),
                cancellationToken
            )
            .ConfigureAwait(false);

        RemoveUnparseableAssets(installFiles);

        var gameData = await GameDataLoader
            .LoadFromMemoryAsync(
                installFiles.Files,
                CreateWeightedProgress(progress, ArchiveReadProgressWeight, 1f - ArchiveReadProgressWeight),
                cancellationToken
            )
            .ConfigureAwait(false);

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

    private static InstallFileSet ReadInstallFiles(
        string gameDir,
        IProgress<float>? progress,
        CancellationToken cancellationToken
    )
    {
        var files = new InstallFileSet();
        var archiveDiscovery = DiscoverArchivePaths(gameDir);
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

        var completedSources = 0;
        foreach (var archivePath in archivePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            OverlayArchive(files, archivePath, cancellationToken);
            completedSources++;
            progress?.Report(completedSources / (float)totalSources * ArchiveReadProgressWeight);
        }

        if (hasLooseDirectory)
        {
            cancellationToken.ThrowIfCancellationRequested();
            OverlayLooseFiles(files, looseDataDirectory, cancellationToken);
            completedSources++;
            progress?.Report(completedSources / (float)totalSources * ArchiveReadProgressWeight);
        }

        return files;
    }

    private static ArchiveDiscoveryResult DiscoverArchivePaths(string gameDir)
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
            if (TryAcceptArchiveCandidate(path, out var skipReason))
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

    private static bool TryAcceptArchiveCandidate(string archivePath, out string? skipReason)
    {
        try
        {
            using var archive = DatArchive.Open(archivePath);
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

    private static void OverlayArchive(InstallFileSet files, string archivePath, CancellationToken cancellationToken)
    {
        using var archive = DatArchive.Open(archivePath);
        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!IsSupportedFormat(entry.Path))
                continue;

            var assetPath = NormalizeVirtualPath(entry.Path);
            files.Files[assetPath] = archive.GetEntryData(entry.Path);
            files.AssetSources[assetPath] = (EditorAssetSourceKind.DatArchive, archivePath, assetPath);
        }
    }

    private static void OverlayLooseFiles(
        InstallFileSet files,
        string looseDataDirectory,
        CancellationToken cancellationToken
    )
    {
        foreach (var filePath in Directory.EnumerateFiles(looseDataDirectory, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!IsSupportedFormat(filePath))
                continue;

            var relativePath = Path.GetRelativePath(looseDataDirectory, filePath);
            var assetPath = NormalizeVirtualPath(relativePath);
            files.Files[assetPath] = File.ReadAllBytes(filePath);
            files.AssetSources[assetPath] = (EditorAssetSourceKind.LooseFile, filePath, null);
        }
    }

    private static bool IsSupportedFormat(string path)
    {
        var format = ResolveSupportedFormat(path);
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

        var fileName = Path.GetFileName(path);
        return fileName.StartsWith("facwalk.", StringComparison.OrdinalIgnoreCase)
            ? FileFormat.FacadeWalk
            : FileFormat.Unknown;
    }

    private static void RemoveUnparseableAssets(InstallFileSet installFiles)
    {
        List<(string AssetPath, string Reason)>? invalidAssets = null;

        foreach (var (assetPath, data) in installFiles.Files)
        {
            if (TryParseAsset(assetPath, data, out var failureReason))
                continue;

            invalidAssets ??= [];
            invalidAssets.Add((assetPath, failureReason!));
        }

        if (invalidAssets is null)
            return;

        foreach (var (assetPath, reason) in invalidAssets)
        {
            if (installFiles.AssetSources.TryGetValue(assetPath, out var source))
            {
                installFiles.SkippedAssets.Add(
                    new EditorSkippedAsset
                    {
                        AssetPath = assetPath,
                        Format = ResolveSupportedFormat(assetPath),
                        SourceKind = source.SourceKind,
                        SourcePath = source.SourcePath,
                        SourceEntryPath = source.SourceEntryPath,
                        Reason = reason,
                    }
                );
            }

            installFiles.Files.Remove(assetPath);
            installFiles.AssetSources.Remove(assetPath);
        }
    }

    private static bool TryParseAsset(string assetPath, ReadOnlyMemory<byte> data, out string? failureReason)
    {
        try
        {
            switch (ResolveSupportedFormat(assetPath))
            {
                case FileFormat.Message:
                    MessageFormat.ParseMemory(data);
                    failureReason = null;
                    return true;
                case FileFormat.Sector:
                    SectorFormat.ParseMemory(data);
                    failureReason = null;
                    return true;
                case FileFormat.Proto:
                    ProtoFormat.ParseMemory(data);
                    failureReason = null;
                    return true;
                case FileFormat.Mob:
                    MobFormat.ParseMemory(data);
                    failureReason = null;
                    return true;
                case FileFormat.Art:
                    ArtFormat.ParseMemory(data);
                    failureReason = null;
                    return true;
                case FileFormat.Jmp:
                    JmpFormat.ParseMemory(data);
                    failureReason = null;
                    return true;
                case FileFormat.MapProperties:
                    MapPropertiesFormat.ParseMemory(data);
                    failureReason = null;
                    return true;
                case FileFormat.Script:
                    ScriptFormat.ParseMemory(data);
                    failureReason = null;
                    return true;
                case FileFormat.Dialog:
                    DialogFormat.ParseMemory(data);
                    failureReason = null;
                    return true;
                case FileFormat.Terrain:
                    TerrainFormat.ParseMemory(data);
                    failureReason = null;
                    return true;
                case FileFormat.FacadeWalk:
                    FacWalkFormat.ParseMemory(data);
                    failureReason = null;
                    return true;
                default:
                    failureReason = null;
                    return false;
            }
        }
        catch (Exception ex) when (IsSkippableAssetParseFailure(ex))
        {
            failureReason = ex.Message;
            return false;
        }
    }

    private static bool IsUnsupportedArchiveFormat(InvalidDataException exception) =>
        exception.Message.StartsWith("Unsupported DAT magic ", StringComparison.Ordinal);

    private static bool IsSkippableAssetParseFailure(Exception exception) =>
        exception is ArgumentOutOfRangeException or InvalidDataException;

    private static string NormalizeVirtualPath(string path) =>
        path.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');

    private static IProgress<float>? CreateWeightedProgress(IProgress<float>? progress, float offset, float span)
    {
        if (progress is null)
            return null;

        return new Progress<float>(value => progress.Report(offset + value * span));
    }

    private sealed class InstallFileSet
    {
        public Dictionary<string, ReadOnlyMemory<byte>> Files { get; } = new(StringComparer.OrdinalIgnoreCase);

        public List<EditorSkippedArchiveCandidate> SkippedArchiveCandidates { get; } = [];

        public List<EditorSkippedAsset> SkippedAssets { get; } = [];

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
