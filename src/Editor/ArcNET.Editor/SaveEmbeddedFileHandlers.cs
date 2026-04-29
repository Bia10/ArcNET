using ArcNET.Formats;

namespace ArcNET.Editor;

internal static class SaveEmbeddedFileHandlers
{
    public static bool TryParse(
        string path,
        string fileName,
        FileFormat format,
        ReadOnlyMemory<byte> memory,
        LoadedSaveBuilder builder
    ) => SaveAssetRegistry.TryParse(path, fileName, format, memory, builder);
}
