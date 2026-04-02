using ArcNET.Formats;

namespace ArcNET.GameData;

/// <summary>
/// Saves a <see cref="GameDataStore"/> back to disk or to in-memory byte arrays.
/// Symmetric counterpart to <see cref="GameDataLoader"/>.
/// </summary>
public sealed class GameDataSaver
{
    /// <summary>
    /// Writes all message entries from <paramref name="store"/> into a single .mes file
    /// at <paramref name="outputPath"/>; creates the directory if needed.
    /// Each entry preserves its original index and optional sound-ID field.
    /// </summary>
    public static void SaveMessagesToFile(GameDataStore store, string outputPath)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        using var writer = new StreamWriter(outputPath, append: false, System.Text.Encoding.ASCII);
        WriteMessageEntries(writer, store.Messages);
    }

    /// <summary>
    /// Serializes all message entries from <paramref name="store"/> to a .mes-formatted
    /// byte array without touching the filesystem.
    /// Each entry preserves its original index and optional sound-ID field.
    /// </summary>
    public static byte[] SaveMessagesToMemory(GameDataStore store)
    {
        ArgumentNullException.ThrowIfNull(store);

        return WriteMessageEntriesToArray(store.Messages);
    }

    /// <summary>
    /// Writes all sectors from <paramref name="store"/> to <paramref name="outputDir"/>.
    /// Original source filenames are preserved when available; otherwise files are named
    /// <c>sector_000000.sec</c>, <c>sector_000001.sec</c>, …
    /// </summary>
    public static void SaveSectorsToDirectory(GameDataStore store, string outputDir)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDir);
        Directory.CreateDirectory(outputDir);

        if (store.SectorsBySource.Count > 0)
        {
            foreach (var (sourceName, sectors) in store.SectorsBySource)
            {
                var path = Path.Combine(outputDir, sourceName);
                foreach (var s in sectors)
                    SectorFormat.WriteToFile(in s, path);
            }
        }
        else
        {
            for (var i = 0; i < store.Sectors.Count; i++)
            {
                var path = Path.Combine(outputDir, FormattableString.Invariant($"sector_{i:D6}.sec"));
                var sector = store.Sectors[i];
                SectorFormat.WriteToFile(in sector, path);
            }
        }
    }

    /// <summary>
    /// Writes all prototypes from <paramref name="store"/> to <paramref name="outputDir"/>.
    /// Original source filenames are preserved when available; otherwise files are named
    /// <c>proto_000000.pro</c>, <c>proto_000001.pro</c>, …
    /// </summary>
    public static void SaveProtosToDirectory(GameDataStore store, string outputDir)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDir);
        Directory.CreateDirectory(outputDir);

        if (store.ProtosBySource.Count > 0)
        {
            foreach (var (sourceName, protos) in store.ProtosBySource)
            {
                var path = Path.Combine(outputDir, sourceName);
                foreach (var p in protos)
                    ProtoFormat.WriteToFile(in p, path);
            }
        }
        else
        {
            for (var i = 0; i < store.Protos.Count; i++)
            {
                var path = Path.Combine(outputDir, FormattableString.Invariant($"proto_{i:D6}.pro"));
                var proto = store.Protos[i];
                ProtoFormat.WriteToFile(in proto, path);
            }
        }
    }

    /// <summary>
    /// Writes all mobile objects from <paramref name="store"/> to <paramref name="outputDir"/>.
    /// Original source filenames are preserved when available; otherwise files are named
    /// <c>mob_000000.mob</c>, <c>mob_000001.mob</c>, …
    /// </summary>
    public static void SaveMobsToDirectory(GameDataStore store, string outputDir)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDir);
        Directory.CreateDirectory(outputDir);

        if (store.MobsBySource.Count > 0)
        {
            foreach (var (sourceName, mobs) in store.MobsBySource)
            {
                var path = Path.Combine(outputDir, sourceName);
                foreach (var m in mobs)
                    MobFormat.WriteToFile(in m, path);
            }
        }
        else
        {
            for (var i = 0; i < store.Mobs.Count; i++)
            {
                var path = Path.Combine(outputDir, FormattableString.Invariant($"mob_{i:D6}.mob"));
                var mob = store.Mobs[i];
                MobFormat.WriteToFile(in mob, path);
            }
        }
    }

    /// <summary>
    /// Writes all data from <paramref name="store"/> to files inside <paramref name="outputDir"/>.
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

        var steps = 0;
        var total =
            (store.Messages.Count > 0 ? 1 : 0)
            + (store.Sectors.Count > 0 ? 1 : 0)
            + (store.Protos.Count > 0 ? 1 : 0)
            + (store.Mobs.Count > 0 ? 1 : 0);

        if (total == 0)
        {
            progress?.Report(1f);
            return;
        }

        if (store.Messages.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Run(
                    () =>
                    {
                        if (store.MessagesBySource.Count > 0)
                        {
                            foreach (var (sourceName, entries) in store.MessagesBySource)
                                WriteMessageEntriesToFile(entries, Path.Combine(outputDir, sourceName));
                        }
                        else
                        {
                            WriteMessageEntriesToFile(store.Messages, Path.Combine(outputDir, "game.mes"));
                        }
                    },
                    ct
                )
                .ConfigureAwait(false);
            progress?.Report(++steps / (float)total);
        }

        if (store.Sectors.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            var sectorsDir = Path.Combine(outputDir, "sectors");
            await Task.Run(() => SaveSectorsToDirectory(store, sectorsDir), ct).ConfigureAwait(false);
            progress?.Report(++steps / (float)total);
        }

        if (store.Protos.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            var protosDir = Path.Combine(outputDir, "proto");
            await Task.Run(() => SaveProtosToDirectory(store, protosDir), ct).ConfigureAwait(false);
            progress?.Report(++steps / (float)total);
        }

        if (store.Mobs.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            var mobsDir = Path.Combine(outputDir, "mobile");
            await Task.Run(() => SaveMobsToDirectory(store, mobsDir), ct).ConfigureAwait(false);
            progress?.Report(++steps / (float)total);
        }
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
        {
            if (store.MessagesBySource.Count > 0)
            {
                foreach (var (sourceName, entries) in store.MessagesBySource)
                    result[sourceName] = WriteMessageEntriesToArray(entries);
            }
            else
            {
                result["game.mes"] = WriteMessageEntriesToArray(store.Messages);
            }
        }

        if (store.SectorsBySource.Count > 0)
        {
            foreach (var (sourceName, sectors) in store.SectorsBySource)
            {
                foreach (var s in sectors)
                    result[sourceName] = SectorFormat.WriteToArray(in s);
            }
        }
        else
        {
            for (var i = 0; i < store.Sectors.Count; i++)
            {
                var sector = store.Sectors[i];
                result[FormattableString.Invariant($"sector_{i:D6}.sec")] = SectorFormat.WriteToArray(in sector);
            }
        }

        if (store.ProtosBySource.Count > 0)
        {
            foreach (var (sourceName, protos) in store.ProtosBySource)
            {
                foreach (var p in protos)
                    result[sourceName] = ProtoFormat.WriteToArray(in p);
            }
        }
        else
        {
            for (var i = 0; i < store.Protos.Count; i++)
            {
                var proto = store.Protos[i];
                result[FormattableString.Invariant($"proto_{i:D6}.pro")] = ProtoFormat.WriteToArray(in proto);
            }
        }

        if (store.MobsBySource.Count > 0)
        {
            foreach (var (sourceName, mobs) in store.MobsBySource)
            {
                foreach (var m in mobs)
                    result[sourceName] = MobFormat.WriteToArray(in m);
            }
        }
        else
        {
            for (var i = 0; i < store.Mobs.Count; i++)
            {
                var mob = store.Mobs[i];
                result[FormattableString.Invariant($"mob_{i:D6}.mob")] = MobFormat.WriteToArray(in mob);
            }
        }

        return result;
    }

    // ── Private helpers ────────────────────────────────────────────────

    private static void WriteMessageEntries(TextWriter writer, IEnumerable<MessageEntry> entries)
    {
        foreach (var entry in entries)
        {
            if (entry.SoundId is not null)
                writer.WriteLine($"{{{entry.Index}}}{{{entry.SoundId}}}{{{entry.Text}}}");
            else
                writer.WriteLine($"{{{entry.Index}}}{{{entry.Text}}}");
        }
    }

    private static void WriteMessageEntriesToFile(IEnumerable<MessageEntry> entries, string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        using var writer = new StreamWriter(path, append: false, System.Text.Encoding.ASCII);
        WriteMessageEntries(writer, entries);
    }

    private static byte[] WriteMessageEntriesToArray(IEnumerable<MessageEntry> entries)
    {
        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms, System.Text.Encoding.ASCII, leaveOpen: true);
        WriteMessageEntries(writer, entries);
        writer.Flush();
        return ms.ToArray();
    }
}
