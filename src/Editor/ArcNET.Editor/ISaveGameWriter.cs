using ArcNET.Formats;

namespace ArcNET.Editor;

internal interface ISaveGameWriter
{
    void Save(LoadedSave original, string saveFolder, string slotName, SaveGameUpdates? updates = null);

    void Save(LoadedSave original, string gsiPath, string tfaiPath, string tfafPath, SaveGameUpdates? updates = null);

    Task SaveAsync(
        LoadedSave original,
        string saveFolder,
        string slotName,
        SaveGameUpdates? updates = null,
        CancellationToken cancellationToken = default
    );

    Task SaveAsync(
        LoadedSave original,
        string gsiPath,
        string tfaiPath,
        string tfafPath,
        SaveGameUpdates? updates = null,
        CancellationToken cancellationToken = default
    );
}

internal sealed class DefaultSaveGameWriter : ISaveGameWriter
{
    public static ISaveGameWriter Instance { get; } = new DefaultSaveGameWriter();

    private DefaultSaveGameWriter() { }

    public void Save(LoadedSave original, string saveFolder, string slotName, SaveGameUpdates? updates = null) =>
        SaveGameWriter.Save(original, saveFolder, slotName, updates);

    public void Save(
        LoadedSave original,
        string gsiPath,
        string tfaiPath,
        string tfafPath,
        SaveGameUpdates? updates = null
    ) => SaveGameWriter.Save(original, gsiPath, tfaiPath, tfafPath, updates);

    public Task SaveAsync(
        LoadedSave original,
        string saveFolder,
        string slotName,
        SaveGameUpdates? updates = null,
        CancellationToken cancellationToken = default
    ) => SaveGameWriter.SaveAsync(original, saveFolder, slotName, updates, cancellationToken);

    public Task SaveAsync(
        LoadedSave original,
        string gsiPath,
        string tfaiPath,
        string tfafPath,
        SaveGameUpdates? updates = null,
        CancellationToken cancellationToken = default
    ) => SaveGameWriter.SaveAsync(original, gsiPath, tfaiPath, tfafPath, updates, cancellationToken);
}
