using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.GameData;

/// <summary>
/// High-level loader that discovers and categorizes game data files in a directory tree.
/// Replaces the old <c>Parser.LoadLocalData</c> method.
/// </summary>
public static class GameDataLoader
{
    /// <summary>Discovers all files grouped by their <see cref="FileFormat"/>.</summary>
    /// <param name="dirPath">Root directory to search recursively.</param>
    /// <returns>A dictionary mapping format to matched file paths.</returns>
    public static IReadOnlyDictionary<FileFormat, IReadOnlyList<string>> DiscoverFiles(string dirPath)
    {
        if (!Directory.Exists(dirPath))
            throw new DirectoryNotFoundException($"Directory not found: {dirPath}");

        var allFiles = Directory.EnumerateFiles(dirPath, "*.*", SearchOption.AllDirectories);
        var result = new Dictionary<FileFormat, List<string>>();

        foreach (var format in Enum.GetValues<FileFormat>())
            result[format] = [];

        foreach (var file in allFiles)
        {
            var format = FileFormatExtensions.FromPath(file);

            // FacadeWalk uses filename pattern instead of extension
            format = ResolveFacadeWalkFormat(format, Path.GetFileName(file));

            result[format].Add(file);
        }

        return result.ToDictionary(kvp => kvp.Key, kvp => (IReadOnlyList<string>)kvp.Value.AsReadOnly());
    }

    /// <summary>Parses all .mes message files in the given directory and returns a merged lookup.</summary>
    public static IReadOnlyDictionary<int, string> LoadMessages(string dirPath)
    {
        var files = DiscoverFiles(dirPath);
        var result = new Dictionary<int, string>();

        if (!files.TryGetValue(FileFormat.Message, out var mesFiles))
            return result;

        foreach (var file in mesFiles)
        {
            var mesFile = MessageFormat.ParseFile(file);
            foreach (var entry in mesFile.Entries)
            {
                if (!result.TryAdd(entry.Index, entry.Text))
                    throw new InvalidOperationException(
                        $"Duplicate message index {entry.Index} in '{file}'. The index was already defined in a previously loaded .mes file."
                    );
            }
        }

        return result;
    }

    /// <summary>
    /// Loads all discoverable game data from <paramref name="dirPath"/> into a new <see cref="GameDataStore"/>.
    /// Reports progress in the range [0, 1] while loading.
    /// </summary>
    public static async Task<GameDataStore> LoadFromDirectoryAsync(
        string dirPath,
        IProgress<float>? progress = null,
        CancellationToken ct = default
    )
    {
        if (!Directory.Exists(dirPath))
            throw new DirectoryNotFoundException($"Directory not found: {dirPath}");

        var files = await Task.Run(() => DiscoverFiles(dirPath), ct).ConfigureAwait(false);
        return await LoadFromDiscoveredFilesAsync(files, progress, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Loads game data from pre-loaded byte buffers keyed by filename.
    /// This overload never touches the filesystem — suitable for editors and unit tests.
    /// </summary>
    public static async Task<GameDataStore> LoadFromMemoryAsync(
        IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files,
        IProgress<float>? progress = null,
        CancellationToken ct = default
    )
    {
        ArgumentNullException.ThrowIfNull(files);

        var store = new GameDataStore();
        var entries = files.ToList();
        var total = entries.Count;

        for (var i = 0; i < total; i++)
        {
            ct.ThrowIfCancellationRequested();
            var (name, memory) = (entries[i].Key, entries[i].Value);
            var format = FileFormatExtensions.FromPath(name);

            format = ResolveFacadeWalkFormat(format, Path.GetFileName(name));

            await LoadEntryFromMemoryAsync(store, format, memory, name, ct).ConfigureAwait(false);
            progress?.Report((i + 1f) / total);
        }

        return store;
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns <see cref="FileFormat.FacadeWalk"/> when <paramref name="current"/> is
    /// <see cref="FileFormat.Unknown"/> and <paramref name="fileName"/> matches the
    /// <c>facwalk.*</c> naming convention; otherwise returns <paramref name="current"/> unchanged.
    /// </summary>
    private static FileFormat ResolveFacadeWalkFormat(FileFormat current, string fileName) =>
        current == FileFormat.Unknown && fileName.StartsWith("facwalk.", StringComparison.OrdinalIgnoreCase)
            ? FileFormat.FacadeWalk
            : current;

    private static async Task<GameDataStore> LoadFromDiscoveredFilesAsync(
        IReadOnlyDictionary<FileFormat, IReadOnlyList<string>> files,
        IProgress<float>? progress,
        CancellationToken ct
    )
    {
        var store = new GameDataStore();

        // Count total files for progress reporting
        var total = files.Values.Sum(l => l.Count);
        var done = 0;

        foreach (var (format, paths) in files)
        {
            foreach (var path in paths)
            {
                ct.ThrowIfCancellationRequested();
                await LoadEntryFromFileAsync(store, format, path, ct).ConfigureAwait(false);
                progress?.Report(++done / (float)total);
            }
        }

        return store;
    }

    private static async Task LoadEntryFromFileAsync(
        GameDataStore store,
        FileFormat format,
        string path,
        CancellationToken ct
    )
    {
        var bytes = await File.ReadAllBytesAsync(path, ct).ConfigureAwait(false);
        await LoadEntryFromMemoryAsync(store, format, bytes, Path.GetFileName(path), ct).ConfigureAwait(false);
    }

    private static Task LoadEntryFromMemoryAsync(
        GameDataStore store,
        FileFormat format,
        ReadOnlyMemory<byte> memory,
        string sourcePath,
        CancellationToken ct
    ) =>
        format switch
        {
            FileFormat.Message => Task.Run(
                () =>
                {
                    var mesFile = MessageFormat.ParseMemory(memory);
                    var name = Path.GetFileName(sourcePath);
                    foreach (var e in mesFile.Entries)
                        store.AddMessage(e, name);
                },
                ct
            ),
            FileFormat.Sector => Task.Run(
                () =>
                {
                    var sector = SectorFormat.ParseMemory(memory);
                    store.AddSector(sector, Path.GetFileName(sourcePath));
                },
                ct
            ),
            FileFormat.Proto => Task.Run(
                () =>
                {
                    var proto = ProtoFormat.ParseMemory(memory);
                    store.AddProto(proto, Path.GetFileName(sourcePath));
                    store.AddObject(proto.Header);
                },
                ct
            ),
            FileFormat.Mob => Task.Run(
                () =>
                {
                    var mob = MobFormat.ParseMemory(memory);
                    store.AddMob(mob, Path.GetFileName(sourcePath));
                    store.AddObject(mob.Header);
                },
                ct
            ),
            _ => Task.CompletedTask,
        };
}
