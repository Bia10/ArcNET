using ArcNET.Formats;
using SharedSaveGameUpdates = ArcNET.GameData.SaveGames.SaveGameUpdates;

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
        JumpFiles = new PendingAssetUpdates<JmpFile>(
            save.JumpFiles,
            new Dictionary<string, JmpFile>(StringComparer.OrdinalIgnoreCase)
        );
        MapProperties = new PendingAssetUpdates<MapProperties>(
            save.MapPropertiesList,
            new Dictionary<string, MapProperties>(StringComparer.OrdinalIgnoreCase)
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

    public PendingAssetUpdates<JmpFile> JumpFiles { get; }

    public PendingAssetUpdates<MapProperties> MapProperties { get; }

    public PendingAssetUpdates<TownMapFog> TownMapFogs { get; }

    public PendingAssetUpdates<DataSavFile> DataSavFiles { get; }

    public PendingAssetUpdates<Data2SavFile> Data2SavFiles { get; }

    public PendingAssetUpdates<byte[]> RawFiles { get; }

    public bool HasPending =>
        MobileMdys.Count > 0
        || Messages.Count > 0
        || JumpFiles.Count > 0
        || MapProperties.Count > 0
        || TownMapFogs.Count > 0
        || DataSavFiles.Count > 0
        || Data2SavFiles.Count > 0
        || RawFiles.Count > 0;

    public SaveGameUpdates? ToSaveGameUpdates(SaveInfo? updatedInfo)
    {
        if (updatedInfo is null && !HasPending)
            return null;

        return new SaveGameUpdates
        {
            UpdatedInfo = updatedInfo,
            UpdatedMobileMdys = MobileMdys.PendingOrNull,
            UpdatedMessages = Messages.PendingOrNull,
            UpdatedJumpFiles = JumpFiles.PendingOrNull,
            UpdatedMapProperties = MapProperties.PendingOrNull,
            UpdatedTownMapFogs = TownMapFogs.PendingOrNull,
            UpdatedDataSavFiles = DataSavFiles.PendingOrNull,
            UpdatedData2SavFiles = Data2SavFiles.PendingOrNull,
            RawFileUpdates = RawFiles.PendingOrNull,
        };
    }

    public SharedSaveGameUpdates? ToSharedSaveGameUpdates(SaveInfo? updatedInfo)
    {
        if (updatedInfo is null && !HasPending)
            return null;

        return new SharedSaveGameUpdates
        {
            UpdatedInfo = updatedInfo,
            UpdatedMobileMdys = MobileMdys.PendingOrNull,
            UpdatedMessages = Messages.PendingOrNull,
            UpdatedJumpFiles = JumpFiles.PendingOrNull,
            UpdatedMapProperties = MapProperties.PendingOrNull,
            UpdatedTownMapFogs = TownMapFogs.PendingOrNull,
            UpdatedDataSavFiles = DataSavFiles.PendingOrNull,
            UpdatedData2SavFiles = Data2SavFiles.PendingOrNull,
            RawFileUpdates = RawFiles.PendingOrNull,
        };
    }
}
