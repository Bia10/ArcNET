using ArcNET.Formats;
using ArcNET.GameData;

namespace ArcNET.Editor;

internal static class EditorAssetCatalogBuilder
{
    public static EditorAssetCatalog CreateForContentDirectory(string contentDirectory, GameDataStore gameData)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentDirectory);
        ArgumentNullException.ThrowIfNull(gameData);

        return Create(
            gameData,
            assetPath =>
                (EditorAssetSourceKind.LooseFile, GetLooseAssetPath(contentDirectory, assetPath), (string?)null)
        );
    }

    public static EditorAssetCatalog CreateForInstall(
        GameDataStore gameData,
        IReadOnlyDictionary<
            string,
            (EditorAssetSourceKind SourceKind, string SourcePath, string? SourceEntryPath)
        > assetSources
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
            }
        );
    }

    private static EditorAssetCatalog Create(
        GameDataStore gameData,
        Func<string, (EditorAssetSourceKind SourceKind, string SourcePath, string? SourceEntryPath)> resolveSource
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

        return EditorAssetCatalog.Create(entries);
    }

    private static void AddEntries<T>(
        List<EditorAssetEntry> entries,
        IReadOnlyDictionary<string, IReadOnlyList<T>> assetsBySource,
        FileFormat format,
        Func<string, (EditorAssetSourceKind SourceKind, string SourcePath, string? SourceEntryPath)> resolveSource
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
                    SourceKind = source.SourceKind,
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

    private static string NormalizeAssetPath(string assetPath) =>
        assetPath.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
}
