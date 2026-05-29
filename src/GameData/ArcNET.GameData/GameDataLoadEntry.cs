using ArcNET.Formats;

namespace ArcNET.GameData;

public sealed class GameDataLoadEntry(
    FileFormat format,
    string sourcePath,
    Func<CancellationToken, Task<ReadOnlyMemory<byte>>> loadContentAsync
)
{
    public FileFormat Format { get; } = format;

    public string SourcePath { get; } = NormalizeSourcePath(sourcePath);

    public Func<CancellationToken, Task<ReadOnlyMemory<byte>>> LoadContentAsync { get; } = loadContentAsync;

    public static GameDataLoadEntry FromFile(FileFormat format, string sourcePath, string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        return new(format, sourcePath, cancellationToken => LoadFileAsync(filePath, cancellationToken));
    }

    public static GameDataLoadEntry FromMemory(FileFormat format, string sourcePath, ReadOnlyMemory<byte> memory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        return new(format, sourcePath, _ => Task.FromResult(memory));
    }

    private static async Task<ReadOnlyMemory<byte>> LoadFileAsync(
        string filePath,
        CancellationToken cancellationToken
    ) => await File.ReadAllBytesAsync(filePath, cancellationToken).ConfigureAwait(false);

    private static string NormalizeSourcePath(string sourcePath) => ArcNET.Core.VirtualPath.Normalize(sourcePath);
}
