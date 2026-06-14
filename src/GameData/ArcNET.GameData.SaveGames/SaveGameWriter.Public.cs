namespace ArcNET.GameData.SaveGames;

/// <summary>
/// Shared save-slot writer surface that delegates to the legacy implementation
/// while accepting package-local update payloads.
/// </summary>
public static class SaveGameWriter
{
    public static void Save(
        LoadedSave original,
        string gsiPath,
        string tfaiPath,
        string tfafPath,
        SaveGameUpdates? updates = null
    ) => ArcNET.Editor.SaveGameWriter.Save(original, gsiPath, tfaiPath, tfafPath, SaveGameUpdates.ToLegacy(updates));

    public static void Save(LoadedSave original, string saveFolder, string slotName, SaveGameUpdates? updates = null) =>
        ArcNET.Editor.SaveGameWriter.Save(original, saveFolder, slotName, SaveGameUpdates.ToLegacy(updates));

    public static Task SaveAsync(
        LoadedSave original,
        string gsiPath,
        string tfaiPath,
        string tfafPath,
        SaveGameUpdates? updates = null,
        CancellationToken cancellationToken = default
    ) =>
        ArcNET.Editor.SaveGameWriter.SaveAsync(
            original,
            gsiPath,
            tfaiPath,
            tfafPath,
            SaveGameUpdates.ToLegacy(updates),
            cancellationToken
        );

    public static Task SaveAsync(
        LoadedSave original,
        string saveFolder,
        string slotName,
        SaveGameUpdates? updates = null,
        CancellationToken cancellationToken = default
    ) =>
        ArcNET.Editor.SaveGameWriter.SaveAsync(
            original,
            saveFolder,
            slotName,
            SaveGameUpdates.ToLegacy(updates),
            cancellationToken
        );
}
