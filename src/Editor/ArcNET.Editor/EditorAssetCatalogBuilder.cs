using ArcNET.Formats;
using ArcNET.GameData;
using ArcNET.GameData.Workspace;

namespace ArcNET.Editor;

internal static class EditorAssetCatalogBuilder
{
    public static EditorAssetCatalog CreateForContentDirectory(string contentDirectory, GameDataStore gameData)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentDirectory);
        ArgumentNullException.ThrowIfNull(gameData);

        return Create(
            gameData,
            assetPath => new WorkspaceAssetSource
            {
                SourceKind = WorkspaceAssetSourceKind.LooseFile,
                SourcePath = GetLooseAssetPath(contentDirectory, assetPath),
                SourceEntryPath = null,
            }
        );
    }

    public static EditorAssetCatalog CreateForInstall(
        GameDataStore gameData,
        IReadOnlyDictionary<string, WorkspaceAssetSource> assetSources
    )
    {
        ArgumentNullException.ThrowIfNull(gameData);
        ArgumentNullException.ThrowIfNull(assetSources);

        return Create(
            gameData,
            assetPath =>
            {
                if (!assetSources.TryGetValue(assetPath, out var assetSource))
                    throw new InvalidOperationException($"No provenance was recorded for asset '{assetPath}'.");

                return assetSource;
            },
            assetSources
        );
    }

    private static EditorAssetCatalog Create(
        GameDataStore gameData,
        Func<string, WorkspaceAssetSource> resolveSource,
        IReadOnlyDictionary<string, WorkspaceAssetSource>? discoveredAssetSources = null
    )
    {
        var entries = new List<EditorAssetEntry>();

        AddEntries(entries, gameData.MessagesBySource, FileFormat.Message, resolveSource);
        AddEntries(entries, gameData.SectorsBySource, FileFormat.Sector, resolveSource);
        AddEntries(entries, gameData.ProtosBySource, FileFormat.Proto, resolveSource);
        AddEntries(entries, gameData.MobsBySource, FileFormat.Mob, resolveSource);
        AddEntries(entries, gameData.ArtsBySource, FileFormat.Art, resolveSource);
        AddEntries(entries, gameData.JumpFilesBySource, FileFormat.Jmp, resolveSource);
        AddEntries(entries, gameData.MapPropertiesBySource, FileFormat.MapProperties, resolveSource);
        AddEntries(entries, gameData.ScriptsBySource, FileFormat.Script, resolveSource);
        AddEntries(entries, gameData.DialogsBySource, FileFormat.Dialog, resolveSource);
        AddEntries(entries, gameData.TerrainsBySource, FileFormat.Terrain, resolveSource);
        AddEntries(entries, gameData.FacadeWalksBySource, FileFormat.FacadeWalk, resolveSource);
        AddDiscoveredEntries(entries, discoveredAssetSources);

        return EditorAssetCatalog.Create(entries);
    }

    private static void AddEntries<T>(
        List<EditorAssetEntry> entries,
        IReadOnlyDictionary<string, IReadOnlyList<T>> assetsBySource,
        FileFormat format,
        Func<string, WorkspaceAssetSource> resolveSource
    )
    {
        foreach (var (assetPath, assets) in assetsBySource.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            var source = resolveSource(assetPath);
            entries.Add(
                new EditorAssetEntry
                {
                    AssetPath = NormalizeAssetPath(assetPath),
                    Format = format,
                    ItemCount = assets.Count,
                    SourceKind = EditorAssetSourceKindAdapter.FromWorkspaceSourceKind(source.SourceKind),
                    SourcePath = source.SourcePath,
                    SourceEntryPath = source.SourceEntryPath,
                }
            );
        }
    }

    private static void AddDiscoveredEntries(
        List<EditorAssetEntry> entries,
        IReadOnlyDictionary<string, WorkspaceAssetSource>? discoveredAssetSources
    )
    {
        if (discoveredAssetSources is null || discoveredAssetSources.Count == 0)
            return;

        var knownPaths = entries.Select(static entry => entry.AssetPath).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (
            var (assetPath, source) in discoveredAssetSources.OrderBy(
                pair => pair.Key,
                StringComparer.OrdinalIgnoreCase
            )
        )
        {
            var normalizedPath = NormalizeAssetPath(assetPath);
            if (!knownPaths.Add(normalizedPath))
                continue;

            var format = ResolveDiscoveredFormat(normalizedPath);
            if (format is FileFormat.Unknown or FileFormat.DataArchive)
                continue;

            entries.Add(
                new EditorAssetEntry
                {
                    AssetPath = normalizedPath,
                    Format = format,
                    ItemCount = 1,
                    SourceKind = EditorAssetSourceKindAdapter.FromWorkspaceSourceKind(source.SourceKind),
                    SourcePath = source.SourcePath,
                    SourceEntryPath = source.SourceEntryPath,
                }
            );
        }
    }

    private static string GetLooseAssetPath(string contentDirectory, string assetPath) =>
        Path.GetFullPath(
            Path.Combine(contentDirectory, NormalizeAssetPath(assetPath).Replace('/', Path.DirectorySeparatorChar))
        );

    private static FileFormat ResolveDiscoveredFormat(string assetPath)
    {
        var format = FileFormatExtensions.FromPath(assetPath);
        return
            format == FileFormat.Unknown
            && Path.GetFileName(assetPath).StartsWith("facwalk.", StringComparison.OrdinalIgnoreCase)
            ? FileFormat.FacadeWalk
            : format;
    }

    private static string NormalizeAssetPath(string assetPath) => ArcNET.Core.VirtualPath.Normalize(assetPath);
}
