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
    private const int MaxLoadedEntryBufferSize = 128;
    private const long MinLoadedEntryRetainedByteBudget = 32L * 1024L * 1024L;
    private const long MaxLoadedEntryRetainedByteBudget = 256L * 1024L * 1024L;
    private const long UnknownEntryEstimatedLength = 64L * 1024L;

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
        var loadedEntryByteBudget = GetLoadedEntryByteBudget(parseParallelism);
        Debug.WriteLine(
            $"[GameDataLoader] CPU={Environment.ProcessorCount}, ParseParallelism={parseParallelism}, ReadParallelism={readParallelism}, BufferedEntries={GetLoadedEntryBufferSize(parseParallelism)}, RetainedByteBudget={loadedEntryByteBudget}, TotalAssets={entries.Count}"
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
        var inFlightByteBudget = new InFlightByteBudget(loadedEntryByteBudget);

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
                        var byteLease = await inFlightByteBudget
                            .AcquireAsync(GetEffectiveEstimatedContentLength(entry), cancellationToken)
                            .ConfigureAwait(false);

                        try
                        {
                            var memory = await entry.LoadContentAsync(cancellationToken).ConfigureAwait(false);
                            await loadedEntryChannel
                                .Writer.WriteAsync(new LoadedEntry(index, entry, memory, byteLease), cancellationToken)
                                .ConfigureAwait(false);
                        }
                        catch
                        {
                            byteLease.Dispose();
                            throw;
                        }
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
            try
            {
                parsedEntries[loadedEntry.Index] = ParseLoadedEntry(loadedEntry.Entry, loadedEntry.Memory);
            }
            finally
            {
                loadedEntry.ByteLease.Dispose();
            }
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
        Math.Clamp(parseParallelism * 4, MinReadParallelism, MaxReadParallelism);

    private static int GetLoadedEntryBufferSize(int parseParallelism) =>
        Math.Clamp(parseParallelism * 2, MinParseParallelism, MaxLoadedEntryBufferSize);

    private static long GetLoadedEntryByteBudget(int parseParallelism) =>
        Math.Clamp(
            parseParallelism * 16L * 1024L * 1024L,
            MinLoadedEntryRetainedByteBudget,
            MaxLoadedEntryRetainedByteBudget
        );

    private static long GetEffectiveEstimatedContentLength(GameDataLoadEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return entry.EstimatedContentLength > 0 ? entry.EstimatedContentLength : UnknownEntryEstimatedLength;
    }

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

    private sealed class InFlightByteBudget(long maxRetainedBytes)
    {
        private readonly object _gate = new();
        private readonly LinkedList<Waiter> _waiters = [];
        private readonly long _maxRetainedBytes = Math.Max(maxRetainedBytes, 1L);
        private long _retainedBytes;

        public ValueTask<Lease> AcquireAsync(long requestedBytes, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var normalizedBytes = Math.Clamp(Math.Max(requestedBytes, 1L), 1L, _maxRetainedBytes);

            lock (_gate)
            {
                if (_waiters.Count == 0 && _retainedBytes + normalizedBytes <= _maxRetainedBytes)
                {
                    _retainedBytes += normalizedBytes;
                    return ValueTask.FromResult(new Lease(this, normalizedBytes));
                }

                var waiter = new Waiter(this, normalizedBytes);
                waiter.Node = _waiters.AddLast(waiter);
                if (cancellationToken.CanBeCanceled)
                {
                    waiter.Registration = cancellationToken.Register(static state => ((Waiter)state!).Cancel(), waiter);
                }

                return new ValueTask<Lease>(waiter.Task);
            }
        }

        private void Cancel(Waiter waiter)
        {
            lock (_gate)
            {
                if (waiter.Node is null)
                    return;

                _waiters.Remove(waiter.Node);
                waiter.Node = null;
            }

            waiter.Registration.Dispose();
            waiter.TrySetCanceled();
        }

        private void Release(long releasedBytes)
        {
            List<Waiter>? readyWaiters = null;

            lock (_gate)
            {
                _retainedBytes = Math.Max(0L, _retainedBytes - releasedBytes);

                while (_waiters.First is { } node)
                {
                    var waiter = node.Value;
                    if (_retainedBytes + waiter.RequestedBytes > _maxRetainedBytes)
                        break;

                    _waiters.Remove(node);
                    waiter.Node = null;
                    _retainedBytes += waiter.RequestedBytes;
                    readyWaiters ??= [];
                    readyWaiters.Add(waiter);
                }
            }

            if (readyWaiters is null)
                return;

            for (var index = 0; index < readyWaiters.Count; index++)
            {
                var waiter = readyWaiters[index];
                waiter.Registration.Dispose();
                waiter.TrySetResult(new Lease(this, waiter.RequestedBytes));
            }
        }

        internal sealed class Lease(InFlightByteBudget owner, long retainedBytes) : IDisposable
        {
            private InFlightByteBudget? _owner = owner;
            private readonly long _retainedBytes = retainedBytes;

            public void Dispose()
            {
                var owner = Interlocked.Exchange(ref _owner, null);
                owner?.Release(_retainedBytes);
            }
        }

        private sealed class Waiter(InFlightByteBudget owner, long requestedBytes)
        {
            private readonly TaskCompletionSource<Lease> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public InFlightByteBudget Owner { get; } = owner;
            public long RequestedBytes { get; } = requestedBytes;
            public LinkedListNode<Waiter>? Node { get; set; }
            public CancellationTokenRegistration Registration { get; set; }
            public Task<Lease> Task => _tcs.Task;

            public void Cancel() => Owner.Cancel(this);

            public void TrySetCanceled() => _tcs.TrySetCanceled();

            public void TrySetResult(Lease lease) => _tcs.TrySetResult(lease);
        }
    }

    private readonly record struct LoadedEntry(
        int Index,
        GameDataLoadEntry Entry,
        ReadOnlyMemory<byte> Memory,
        InFlightByteBudget.Lease ByteLease
    );

    private readonly record struct FileLoadEntry(FileFormat Format, string Path);

    private readonly record struct ParsedLoadEntry(
        string SourcePath,
        FileFormat Format,
        Action<GameDataStore>? ApplyAction,
        string? FailureReason
    );

    private static string NormalizeSourcePath(string sourcePath) => ArcNET.Core.VirtualPath.Normalize(sourcePath);
}
