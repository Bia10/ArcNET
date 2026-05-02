using ArcNET.GameData;

namespace ArcNET.Editor;

internal static class EditorWorkspaceSnapshotBuilder
{
    public static EditorWorkspace Build(
        EditorWorkspace baseline,
        GameDataStore? gameData = null,
        LoadedSave? save = null
    )
    {
        ArgumentNullException.ThrowIfNull(baseline);

        var effectiveGameData = gameData ?? baseline.GameData;
        var effectiveSave = save ?? baseline.Save;
        var (index, validation) = ReferenceEquals(effectiveGameData, baseline.GameData)
            ? (baseline.Index, baseline.Validation)
            : EditorAssetIndexBuilder.Create(effectiveGameData, baseline.Assets, baseline.InstallationType);

        return new EditorWorkspace
        {
            ContentDirectory = baseline.ContentDirectory,
            GameDirectory = baseline.GameDirectory,
            Module = baseline.Module,
            InstallationType = baseline.InstallationType,
            GameData = effectiveGameData,
            Assets = baseline.Assets,
            AudioAssets = baseline.AudioAssets,
            Index = index,
            LoadReport = baseline.LoadReport,
            Validation = validation,
            AudioAssetData = baseline.AudioAssetData,
            Save = effectiveSave,
            SaveFolder = baseline.SaveFolder,
            SaveSlotName = baseline.SaveSlotName,
        };
    }
}
