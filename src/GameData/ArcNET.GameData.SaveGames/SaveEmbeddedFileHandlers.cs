using ArcNET.Formats;

namespace ArcNET.GameData.SaveGames;

internal static class SaveEmbeddedFileHandlers
{
    public static bool TryParse(
        string path,
        string fileName,
        FileFormat format,
        ReadOnlyMemory<byte> memory,
        LoadedSaveBuilder builder
    ) => SaveAssetRegistryCore.TryParse(path, fileName, format, memory, builder);
}
