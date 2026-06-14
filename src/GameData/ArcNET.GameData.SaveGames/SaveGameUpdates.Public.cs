using ArcNET.Formats;

namespace ArcNET.GameData.SaveGames;

/// <summary>
/// Shared save-update payload that mirrors the legacy editor surface while keeping
/// non-editor callers on the package-local namespace.
/// </summary>
public sealed record SaveGameUpdates
{
    public SaveInfo? UpdatedInfo { get; init; }

    public IReadOnlyDictionary<string, MobData>? UpdatedMobiles { get; init; }

    public IReadOnlyDictionary<string, Sector>? UpdatedSectors { get; init; }

    public IReadOnlyDictionary<string, JmpFile>? UpdatedJumpFiles { get; init; }

    public IReadOnlyDictionary<string, MapProperties>? UpdatedMapProperties { get; init; }

    public IReadOnlyDictionary<string, MesFile>? UpdatedMessages { get; init; }

    public IReadOnlyDictionary<string, TownMapFog>? UpdatedTownMapFogs { get; init; }

    public IReadOnlyDictionary<string, DataSavFile>? UpdatedDataSavFiles { get; init; }

    public IReadOnlyDictionary<string, Data2SavFile>? UpdatedData2SavFiles { get; init; }

    public IReadOnlyDictionary<string, ScrFile>? UpdatedScripts { get; init; }

    public IReadOnlyDictionary<string, DlgFile>? UpdatedDialogs { get; init; }

    public IReadOnlyDictionary<string, MobileMdFile>? UpdatedMobileMds { get; init; }

    public IReadOnlyDictionary<string, MobileMdyFile>? UpdatedMobileMdys { get; init; }

    public IReadOnlyDictionary<string, byte[]>? RawFileUpdates { get; init; }

    internal static ArcNET.Editor.SaveGameUpdates? ToLegacy(SaveGameUpdates? updates) =>
        updates is null
            ? null
            : new ArcNET.Editor.SaveGameUpdates
            {
                UpdatedInfo = updates.UpdatedInfo,
                UpdatedMobiles = updates.UpdatedMobiles,
                UpdatedSectors = updates.UpdatedSectors,
                UpdatedJumpFiles = updates.UpdatedJumpFiles,
                UpdatedMapProperties = updates.UpdatedMapProperties,
                UpdatedMessages = updates.UpdatedMessages,
                UpdatedTownMapFogs = updates.UpdatedTownMapFogs,
                UpdatedDataSavFiles = updates.UpdatedDataSavFiles,
                UpdatedData2SavFiles = updates.UpdatedData2SavFiles,
                UpdatedScripts = updates.UpdatedScripts,
                UpdatedDialogs = updates.UpdatedDialogs,
                UpdatedMobileMds = updates.UpdatedMobileMds,
                UpdatedMobileMdys = updates.UpdatedMobileMdys,
                RawFileUpdates = updates.RawFileUpdates,
            };
}
