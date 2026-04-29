using ArcNET.Formats;

namespace ArcNET.Editor;

internal sealed class PendingGameUpdates
{
    public PendingGameUpdates(LoadedSave save)
    {
        MobileMdys = new PendingAssetUpdates<MobileMdyFile>(
            save.MobileMdys,
            new Dictionary<string, MobileMdyFile>(StringComparer.OrdinalIgnoreCase)
        );
        Messages = new PendingAssetUpdates<MesFile>(
            save.Messages,
            new Dictionary<string, MesFile>(StringComparer.OrdinalIgnoreCase)
        );
        TownMapFogs = new PendingAssetUpdates<TownMapFog>(
            save.TownMapFogs,
            new Dictionary<string, TownMapFog>(StringComparer.OrdinalIgnoreCase)
        );
        DataSavFiles = new PendingAssetUpdates<DataSavFile>(
            save.DataSavFiles,
            new Dictionary<string, DataSavFile>(StringComparer.OrdinalIgnoreCase)
        );
        Data2SavFiles = new PendingAssetUpdates<Data2SavFile>(
            save.Data2SavFiles,
            new Dictionary<string, Data2SavFile>(StringComparer.OrdinalIgnoreCase)
        );
        RawFiles = new PendingAssetUpdates<byte[]>(
            save.RawFiles,
            new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        );
    }

    public PendingAssetUpdates<MobileMdyFile> MobileMdys { get; }

    public PendingAssetUpdates<MesFile> Messages { get; }

    public PendingAssetUpdates<TownMapFog> TownMapFogs { get; }

    public PendingAssetUpdates<DataSavFile> DataSavFiles { get; }

    public PendingAssetUpdates<Data2SavFile> Data2SavFiles { get; }

    public PendingAssetUpdates<byte[]> RawFiles { get; }

    public bool HasPending => SaveAssetRegistry.HasPendingUpdates(this);

    public SaveGameUpdates? ToSaveGameUpdates(SaveInfo? updatedInfo)
    {
        if (updatedInfo is null && !HasPending)
            return null;

        return SaveAssetRegistry.ToSaveGameUpdates(this, updatedInfo);
    }
}
