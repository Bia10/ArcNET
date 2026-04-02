using ArcNET.Formats;

namespace ArcNET.GameData;

/// <summary>
/// Saves a <see cref="GameDataStore"/> back to disk or to in-memory byte arrays.
/// Symmetric counterpart to <see cref="GameDataLoader"/>.
/// </summary>
public sealed class GameDataSaver
{
    /// <summary>
    /// Writes all message strings from <paramref name="store"/> into a single .mes file
    /// at <paramref name="outputPath"/>; creates the directory if needed.
    /// </summary>
    public static void SaveMessagesToFile(GameDataStore store, string outputPath)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        using var writer = new StreamWriter(outputPath, append: false, System.Text.Encoding.ASCII);
        for (var i = 0; i < store.Messages.Count; i++)
            writer.WriteLine($"{{{i}}}{{{store.Messages[i]}}}");
    }

    /// <summary>
    /// Serializes all message strings from <paramref name="store"/> to a .mes-formatted
    /// byte array without touching the filesystem.
    /// </summary>
    public static byte[] SaveMessagesToMemory(GameDataStore store)
    {
        ArgumentNullException.ThrowIfNull(store);

        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms, System.Text.Encoding.ASCII, leaveOpen: true);
        for (var i = 0; i < store.Messages.Count; i++)
            writer.WriteLine($"{{{i}}}{{{store.Messages[i]}}}");

        writer.Flush();
        return ms.ToArray();
    }

    /// <summary>
    /// Writes all changed data from <paramref name="store"/> to files inside <paramref name="outputDir"/>.
    /// Creates subdirectories as needed. Reports progress in [0, 1].
    /// </summary>
    public static async Task SaveToDirectoryAsync(
        GameDataStore store,
        string outputDir,
        IProgress<float>? progress = null,
        CancellationToken ct = default
    )
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDir);

        Directory.CreateDirectory(outputDir);

        // Persist messages as a single combined .mes file.
        if (store.Messages.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            var mesPath = Path.Combine(outputDir, "game.mes");
            await Task.Run(() => SaveMessagesToFile(store, mesPath), ct).ConfigureAwait(false);
        }

        progress?.Report(1f);
    }

    /// <summary>
    /// Serializes all data from <paramref name="store"/> to in-memory byte arrays keyed by
    /// virtual filename. No filesystem access is performed.
    /// </summary>
    public static IReadOnlyDictionary<string, byte[]> SaveToMemory(GameDataStore store)
    {
        ArgumentNullException.ThrowIfNull(store);

        var result = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        if (store.Messages.Count > 0)
            result["game.mes"] = SaveMessagesToMemory(store);

        return result;
    }
}
