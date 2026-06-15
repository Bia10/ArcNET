using ArcNET.Formats;

namespace ArcNET.GameData.SaveGames;

internal static class SaveGamePayloadComposer
{
    public static Dictionary<string, byte[]> Compose(LoadedSave original, SaveGameUpdates? updates)
    {
        var files = new Dictionary<string, byte[]>(original.Files, StringComparer.OrdinalIgnoreCase);
        SaveAssetRegistryCore.ApplyUpdates(updates, files);

        return files;
    }
}
