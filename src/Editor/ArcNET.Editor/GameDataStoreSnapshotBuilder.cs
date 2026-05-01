using ArcNET.Formats;
using ArcNET.GameData;

namespace ArcNET.Editor;

internal static class GameDataStoreSnapshotBuilder
{
    public static GameDataStore CloneWithAssetReplacements(
        GameDataStore source,
        IReadOnlyDictionary<string, ScrFile>? updatedScripts = null,
        IReadOnlyDictionary<string, DlgFile>? updatedDialogs = null,
        IReadOnlyDictionary<string, Sector>? updatedSectors = null,
        IReadOnlyDictionary<string, ProtoData>? updatedProtos = null,
        IReadOnlyDictionary<string, MobData>? updatedMobs = null
    )
    {
        ArgumentNullException.ThrowIfNull(source);

        var snapshot = new GameDataStore();

        foreach (var header in source.Objects)
            snapshot.AddObject(header);

        CopyTrackedAssets(
            source.MessagesBySource,
            snapshot,
            null,
            static (store, asset, assetPath) => store.AddMessage(asset, assetPath)
        );
        CopyTrackedAssets(
            source.SectorsBySource,
            snapshot,
            updatedSectors,
            static (store, asset, assetPath) => store.AddSector(asset, assetPath)
        );
        CopyTrackedAssets(
            source.ProtosBySource,
            snapshot,
            updatedProtos,
            static (store, asset, assetPath) => store.AddProto(asset, assetPath)
        );
        CopyTrackedAssets(
            source.MobsBySource,
            snapshot,
            updatedMobs,
            static (store, asset, assetPath) => store.AddMob(asset, assetPath)
        );
        CopyTrackedAssets(
            source.ArtsBySource,
            snapshot,
            null,
            static (store, asset, assetPath) => store.AddArt(asset, assetPath)
        );
        CopyTrackedScripts(source.ScriptsBySource, snapshot, updatedScripts);
        CopyTrackedDialogs(source.DialogsBySource, snapshot, updatedDialogs);

        foreach (var dirtyObjectId in source.DirtyObjects)
            snapshot.MarkDirty(dirtyObjectId);

        return snapshot;
    }

    private static void CopyTrackedAssets<T>(
        IReadOnlyDictionary<string, IReadOnlyList<T>> assetsBySource,
        GameDataStore snapshot,
        IReadOnlyDictionary<string, T>? updatedAssets,
        Action<GameDataStore, T, string> add
    )
    {
        foreach (var (assetPath, assets) in assetsBySource)
        {
            if (updatedAssets is not null && updatedAssets.TryGetValue(assetPath, out var updatedAsset))
            {
                add(snapshot, updatedAsset, assetPath);
                continue;
            }

            foreach (var asset in assets)
                add(snapshot, asset, assetPath);
        }
    }

    private static void CopyTrackedScripts(
        IReadOnlyDictionary<string, IReadOnlyList<ScrFile>> scriptsBySource,
        GameDataStore snapshot,
        IReadOnlyDictionary<string, ScrFile>? updatedScripts
    )
    {
        foreach (var (assetPath, scripts) in scriptsBySource)
        {
            if (updatedScripts is not null && updatedScripts.TryGetValue(assetPath, out var updatedScript))
            {
                snapshot.AddScript(updatedScript, assetPath);
                continue;
            }

            foreach (var script in scripts)
                snapshot.AddScript(script, assetPath);
        }
    }

    private static void CopyTrackedDialogs(
        IReadOnlyDictionary<string, IReadOnlyList<DlgFile>> dialogsBySource,
        GameDataStore snapshot,
        IReadOnlyDictionary<string, DlgFile>? updatedDialogs
    )
    {
        foreach (var (assetPath, dialogs) in dialogsBySource)
        {
            if (updatedDialogs is not null && updatedDialogs.TryGetValue(assetPath, out var updatedDialog))
            {
                snapshot.AddDialog(updatedDialog, assetPath);
                continue;
            }

            foreach (var dialog in dialogs)
                snapshot.AddDialog(dialog, assetPath);
        }
    }
}
