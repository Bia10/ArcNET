using ArcNET.Formats;
using ArcNET.GameData;

namespace ArcNET.Editor;

internal static class EditorWorkspaceSaveComposition
{
    public static GameDataStore OverlayWorldAssets(
        GameDataStore baseGameData,
        EditorAssetCatalog assets,
        LoadedSave? save
    )
    {
        ArgumentNullException.ThrowIfNull(baseGameData);
        ArgumentNullException.ThrowIfNull(assets);

        if (save is null)
            return baseGameData;

        var updatedMobs = CollectOverrides(save.Mobiles, assets, FileFormat.Mob);
        var updatedSectors = CollectOverrides(save.Sectors, assets, FileFormat.Sector);
        if (updatedMobs.Count == 0 && updatedSectors.Count == 0)
            return baseGameData;

        return GameDataStoreSnapshotBuilder.CloneWithAssetReplacements(
            baseGameData,
            updatedSectors: updatedSectors,
            updatedMobs: updatedMobs
        );
    }

    private static Dictionary<string, T> CollectOverrides<T>(
        IReadOnlyDictionary<string, T> saveAssets,
        EditorAssetCatalog assets,
        FileFormat expectedFormat
    )
        where T : class
    {
        var overrides = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);

        foreach (var (assetPath, value) in saveAssets)
        {
            if (assets.Find(assetPath)?.Format != expectedFormat)
                continue;

            overrides[assetPath] = value;
        }

        return overrides;
    }
}
