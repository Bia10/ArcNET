using System.Diagnostics;
using System.Threading.Channels;
using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.GameData;

/// <summary>
/// High-level loader that discovers and categorizes game data files in a directory tree.
/// Replaces the old <c>Parser.LoadLocalData</c> method.
/// </summary>
public static class GameDataLoader
{
    private delegate Action<GameDataStore>? MemoryLoadHandler(ReadOnlyMemory<byte> memory, string sourcePath);
    private const int MaxParseParallelism = 64;
    private const int MinParseParallelism = 4;
    private const int MaxReadParallelism = 256;
    private const int MinReadParallelism = 8;
    private const int MaxLoadedEntryBufferSize = 256;

    private static readonly IReadOnlyDictionary<FileFormat, MemoryLoadHandler> s_memoryLoadHandlers = new Dictionary<
        FileFormat,
        MemoryLoadHandler
    >
    {
        [FileFormat.Message] = static (memory, sourcePath) =>
        {
            var mesFile = MessageFormat.ParseMemory(memory);
            var normalizedPath = NormalizeSourcePath(sourcePath);
            return store =>
            {
                foreach (var entry in mesFile.Entries)
                    store.AddMessage(entry, normalizedPath);
            };
        },
        [FileFormat.Sector] = static (memory, sourcePath) =>
        {
            var sector = SectorFormat.ParseMemory(memory);
            var normalizedPath = NormalizeSourcePath(sourcePath);
            return store => store.AddSector(sector, normalizedPath);
        },
        [FileFormat.Proto] = static (memory, sourcePath) =>
        {
            var proto = ProtoFormat.ParseMemory(memory);
            var normalizedPath = NormalizeSourcePath(sourcePath);
            return store =>
            {
                store.AddProto(proto, normalizedPath);
                store.AddObject(proto.Header);
            };
        },
        [FileFormat.Mob] = static (memory, sourcePath) =>
        {
            var mob = MobFormat.ParseMemory(memory);
            var normalizedPath = NormalizeSourcePath(sourcePath);
            return store =>
            {
                store.AddMob(mob, normalizedPath);
                store.AddObject(mob.Header);
            };
        },
        [FileFormat.Art] = static (memory, sourcePath) =>
        {
            var art = ArtFormat.ParseMetadataMemory(memory);
            var normalizedPath = NormalizeSourcePath(sourcePath);
            return store => store.AddArt(art, normalizedPath);
        },
        [FileFormat.Jmp] = static (memory, sourcePath) =>
        {
            var jumpFile = JmpFormat.ParseMemory(memory);
            var normalizedPath = NormalizeSourcePath(sourcePath);
            return store => store.AddJumpFile(jumpFile, normalizedPath);
        },
        [FileFormat.MapProperties] = static (memory, sourcePath) =>
        {
            var properties = MapPropertiesFormat.ParseMemory(memory);
            var normalizedPath = NormalizeSourcePath(sourcePath);
            return store => store.AddMapProperties(properties, normalizedPath);
        },
        [FileFormat.Script] = static (memory, sourcePath) =>
        {
            var script = ScriptFormat.ParseMemory(memory);
            var normalizedPath = NormalizeSourcePath(sourcePath);
            return store => store.AddScript(script, normalizedPath);
        },
        [FileFormat.Dialog] = static (memory, sourcePath) =>
        {
            var dialog = DialogFormat.ParseMemory(memory);
            var normalizedPath = NormalizeSourcePath(sourcePath);
            return store => store.AddDialog(dialog, normalizedPath);
        },
        [FileFormat.Terrain] = static (memory, sourcePath) =>
        {
            var terrain = TerrainFormat.ParseMemory(memory);
            var normalizedPath = NormalizeSourcePath(sourcePath);
            return store => store.AddTerrain(terrain, normalizedPath);
        },
        [FileFormat.FacadeWalk] = static (memory, sourcePath) =>
        {
            var facadeWalk = FacWalkFormat.ParseMemory(memory);
            var normalizedPath = NormalizeSourcePath(sourcePath);
            return store => store.AddFacadeWalk(facadeWalk, normalizedPath);
        },
    };

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
        CancellationToken ct = default,
        IProgress<GameDataLoadProgress>? loadProgress = null
    )
    {
        if (!Directory.Exists(dirPath))
            throw new DirectoryNotFoundException($"Directory not found: {dirPath}");

        loadProgress?.Report(new GameDataLoadProgress("Discovering game data files", 0f));
        var files = await Task.Run(() => DiscoverFiles(dirPath), ct).ConfigureAwait(false);
        var result = await LoadFromEntriesAsync(CreateFileEntries(dirPath, files), progress, ct, loadProgress)
            .ConfigureAwait(false);
        ThrowIfFailures(result.Failures);
        return result.Store;
    }

    /// <summary>
    /// Loads game data from pre-loaded byte buffers keyed by filename.
    /// This overload never touches the filesystem — suitable for editors and unit tests.
    /// </summary>
    public static async Task<GameDataStore> LoadFromMemoryAsync(
        IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files,
        IProgress<float>? progress = null,
        CancellationToken ct = default,
        IProgress<GameDataLoadProgress>? loadProgress = null
    )
    {
        ArgumentNullException.ThrowIfNull(files);

        var entries = files
            .Select(static pair =>
            {
                var format = FileFormatExtensions.FromPath(pair.Key);
                format = ResolveFacadeWalkFormat(format, Path.GetFileName(pair.Key));
                return GameDataLoadEntry.FromMemory(format, pair.Key, pair.Value);
            })
            .ToArray();

        var result = await LoadFromEntriesAsync(entries, progress, ct, loadProgress).ConfigureAwait(false);
        ThrowIfFailures(result.Failures);
        return result.Store;
    }

    /// <summary>
    /// Loads game data from caller-supplied content entries.
    /// Known parse failures are returned in <see cref="GameDataLoadResult.Failures"/> so callers can
    /// decide whether to skip those assets or fail the whole load.
    /// </summary>
    public static async Task<GameDataLoadResult> LoadFromEntriesAsync(
        IReadOnlyList<GameDataLoadEntry> entries,
        IProgress<float>? progress = null,
        CancellationToken ct = default,
        IProgress<GameDataLoadProgress>? loadProgress = null
    )
    {
        ArgumentNullException.ThrowIfNull(entries);

        var parsedEntries = await ParseEntriesAsync(entries, progress, ct, loadProgress).ConfigureAwait(false);
        return ApplyParsedEntries(parsedEntries);
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

    private static IReadOnlyList<GameDataLoadEntry> CreateFileEntries(
        string rootDir,
        IReadOnlyDictionary<FileFormat, IReadOnlyList<string>> files
    ) =>
        files
            .SelectMany(static pair => pair.Value.Select(path => new FileLoadEntry(pair.Key, path)))
            .Select(entry =>
                GameDataLoadEntry.FromFile(entry.Format, Path.GetRelativePath(rootDir, entry.Path), entry.Path)
            )
            .ToArray();

    private static async Task<ParsedLoadEntry[]> ParseEntriesAsync(
        IReadOnlyList<GameDataLoadEntry> entries,
        IProgress<float>? progress,
        CancellationToken ct,
        IProgress<GameDataLoadProgress>? loadProgress
    )
    {
        var parsedEntries = new ParsedLoadEntry[entries.Count];
        if (entries.Count == 0)
        {
            loadProgress?.Report(new GameDataLoadProgress("Parsing game data assets", 1f, 0, 0));
            return parsedEntries;
        }

        loadProgress?.Report(new GameDataLoadProgress("Parsing game data assets", 0f, 0, entries.Count));

        var parseParallelism = GetParseParallelism();
        var readParallelism = GetReadParallelism(parseParallelism);
        Debug.WriteLine(
            $"[GameDataLoader] CPU={Environment.ProcessorCount}, ParseParallelism={parseParallelism}, ReadParallelism={readParallelism}, TotalAssets={entries.Count}"
        );
        // Use a bounded channel to prevent unbounded memory growth if reading is faster than parsing.
        // AllowSynchronousContinuations MUST be false to ensure that the CPU-intensive parse continuations
        // do not hijack the I/O threads. Setting it to true destroys the parallelism pipeline.
        var loadedEntryChannel = Channel.CreateBounded<LoadedEntry>(
            new BoundedChannelOptions(GetLoadedEntryBufferSize(parseParallelism))
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleWriter = false,
                SingleReader = false, // Multiple parse workers read from the channel
                AllowSynchronousContinuations = false, // Critical: Decouple I/O and CPU work
            }
        );

        var parseWorkers = Enumerable
            .Range(0, parseParallelism)
            .Select(_ => Task.Run(() => ConsumeLoadedEntriesAsync(loadedEntryChannel.Reader, parsedEntries, ct), ct))
            .ToArray();

        Exception? loadFailure = null;
        try
        {
            await Parallel
                .ForEachAsync(
                    Enumerable.Range(0, entries.Count),
                    new ParallelOptions { CancellationToken = ct, MaxDegreeOfParallelism = readParallelism },
                    async (index, cancellationToken) =>
                    {
                        var entry = entries[index];
                        var memory = await entry.LoadContentAsync(cancellationToken).ConfigureAwait(false);
                        await loadedEntryChannel
                            .Writer.WriteAsync(new LoadedEntry(index, entry, memory), cancellationToken)
                            .ConfigureAwait(false);
                    }
                )
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            loadFailure = ex;
        }
        finally
        {
            loadedEntryChannel.Writer.TryComplete(loadFailure);
        }

        await Task.WhenAll(parseWorkers).ConfigureAwait(false);

        // Final progress report
        progress?.Report(1f);
        loadProgress?.Report(new GameDataLoadProgress("Parsing game data assets", 1f, entries.Count, entries.Count));

        return parsedEntries;
    }

    private static async Task ConsumeLoadedEntriesAsync(
        ChannelReader<LoadedEntry> reader,
        ParsedLoadEntry[] parsedEntries,
        CancellationToken ct
    )
    {
        await foreach (var loadedEntry in reader.ReadAllAsync(ct).ConfigureAwait(false))
        {
            parsedEntries[loadedEntry.Index] = ParseLoadedEntry(loadedEntry.Entry, loadedEntry.Memory);
        }
    }

    private static ParsedLoadEntry ParseLoadedEntry(GameDataLoadEntry entry, ReadOnlyMemory<byte> memory)
    {
        try
        {
            return new ParsedLoadEntry(
                entry.SourcePath,
                entry.Format,
                ParseEntryFromMemory(entry.Format, memory, entry.SourcePath),
                null
            );
        }
        catch (Exception ex) when (IsSkippableAssetParseFailure(ex))
        {
            return new ParsedLoadEntry(entry.SourcePath, entry.Format, null, ex.Message);
        }
    }

    private static int GetParseParallelism() =>
        Math.Clamp(Environment.ProcessorCount * 2, MinParseParallelism, MaxParseParallelism);

    private static int GetReadParallelism(int parseParallelism) =>
        Math.Clamp(parseParallelism * 8, MinReadParallelism, MaxReadParallelism);

    private static int GetLoadedEntryBufferSize(int parseParallelism) =>
        Math.Clamp(parseParallelism * 8, MinReadParallelism, MaxLoadedEntryBufferSize);

    private static Action<GameDataStore>? ParseEntryFromMemory(
        FileFormat format,
        ReadOnlyMemory<byte> memory,
        string sourcePath
    )
    {
        if (!s_memoryLoadHandlers.TryGetValue(format, out var handler))
            return null;

        return handler(memory, sourcePath);
    }

    private static GameDataLoadResult ApplyParsedEntries(IReadOnlyList<ParsedLoadEntry> parsedEntries)
    {
        var store = new GameDataStore();
        List<GameDataLoadFailure>? failures = null;
        for (var index = 0; index < parsedEntries.Count; index++)
        {
            var parsedEntry = parsedEntries[index];
            parsedEntry.ApplyAction?.Invoke(store);
            if (parsedEntry.FailureReason is null)
                continue;

            failures ??= [];
            failures.Add(
                new GameDataLoadFailure(parsedEntry.SourcePath, parsedEntry.Format, parsedEntry.FailureReason)
            );
        }

        return new GameDataLoadResult(store, failures ?? []);
    }

    private static void ThrowIfFailures(IReadOnlyList<GameDataLoadFailure> failures)
    {
        if (failures.Count == 0)
            return;

        var firstFailure = failures[0];
        var additionalFailureCount = failures.Count - 1;
        throw new InvalidDataException(
            additionalFailureCount == 0
                ? $"Failed to parse '{firstFailure.SourcePath}': {firstFailure.Reason}"
                : $"Failed to parse '{firstFailure.SourcePath}': {firstFailure.Reason} ({additionalFailureCount} additional failure(s) omitted)."
        );
    }

    private static bool IsSkippableAssetParseFailure(Exception exception) =>
        exception is ArgumentOutOfRangeException or InvalidDataException;

    private readonly record struct LoadedEntry(int Index, GameDataLoadEntry Entry, ReadOnlyMemory<byte> Memory);

    private readonly record struct FileLoadEntry(FileFormat Format, string Path);

    private readonly record struct ParsedLoadEntry(
        string SourcePath,
        FileFormat Format,
        Action<GameDataStore>? ApplyAction,
        string? FailureReason
    );

    private static string NormalizeSourcePath(string sourcePath) => ArcNET.Core.VirtualPath.Normalize(sourcePath);
}
