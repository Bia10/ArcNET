namespace ArcNET.GameData.SaveGames;

/// <summary>
/// Shared save-slot loader surface that delegates to the legacy editor-namespaced implementation
/// while returning the package-local <see cref="LoadedSave"/> view.
/// </summary>
public static class SaveGameLoader
{
    public static LoadedSave Load(string gsiPath, string tfaiPath, string tfafPath) =>
        LoadedSave.FromLegacy(ArcNET.Editor.SaveGameLoader.Load(gsiPath, tfaiPath, tfafPath));

    public static LoadedSave Load(string saveFolder, string slotName) =>
        LoadedSave.FromLegacy(ArcNET.Editor.SaveGameLoader.Load(saveFolder, slotName));

    public static async Task<LoadedSave> LoadAsync(
        string gsiPath,
        string tfaiPath,
        string tfafPath,
        IProgress<float>? progress = null,
        CancellationToken cancellationToken = default
    ) =>
        LoadedSave.FromLegacy(
            await ArcNET
                .Editor.SaveGameLoader.LoadAsync(gsiPath, tfaiPath, tfafPath, progress, cancellationToken)
                .ConfigureAwait(false)
        );

    public static async Task<LoadedSave> LoadAsync(
        string saveFolder,
        string slotName,
        IProgress<float>? progress = null,
        CancellationToken cancellationToken = default
    ) =>
        LoadedSave.FromLegacy(
            await ArcNET
                .Editor.SaveGameLoader.LoadAsync(saveFolder, slotName, progress, cancellationToken)
                .ConfigureAwait(false)
        );
}
