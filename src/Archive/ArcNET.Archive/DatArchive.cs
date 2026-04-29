using System.Collections.Frozen;
using System.IO.MemoryMappedFiles;

namespace ArcNET.Archive;

/// <summary>
/// Provides read access to Arcanum DAT archive files (both arcanum1.dat-style and
/// Modules\Arcanum.dat-style — same binary format).
/// </summary>
public sealed class DatArchive : IDisposable
{
    private readonly MemoryMappedFile _mmf;
    private readonly FrozenDictionary<string, ArchiveEntry> _entries;
    private bool _disposed;

    private DatArchive(MemoryMappedFile mmf, FrozenDictionary<string, ArchiveEntry> entries)
    {
        _mmf = mmf;
        _entries = entries;
    }

    /// <summary>Gets all entries in this archive (excludes directory pseudo-entries).</summary>
    public IReadOnlyCollection<ArchiveEntry> Entries => _entries.Values;

    /// <summary>Opens an archive file and reads its directory.</summary>
    public static DatArchive Open(string path)
    {
        var fi = new FileInfo(path);
        if (!fi.Exists)
            throw new FileNotFoundException("DAT archive not found.", path);

        var mmf = MemoryMappedFile.CreateFromFile(
            path,
            FileMode.Open,
            mapName: null,
            fi.Length,
            MemoryMappedFileAccess.Read
        );
        try
        {
            var entries = DatDirectoryReader.Read(mmf, fi.Length);
            return new DatArchive(mmf, entries);
        }
        catch (Exception)
        {
            mmf.Dispose();
            throw;
        }
    }

    /// <summary>Finds an entry by its virtual path (case-insensitive, backslash or forward-slash).</summary>
    public ArchiveEntry? FindEntry(string virtualPath)
    {
        var normalized = DatArchivePath.Normalize(virtualPath);
        return _entries.TryGetValue(normalized, out var entry) ? entry : null;
    }

    /// <summary>Returns the decompressed bytes of the given entry.</summary>
    public ReadOnlyMemory<byte> GetEntryData(string name)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_entries.TryGetValue(DatArchivePath.Normalize(name), out var entry))
            throw new KeyNotFoundException($"Entry not found: {name}");
        return DatEntryReader.ReadEntryData(_mmf, entry);
    }

    /// <summary>Opens a readable stream over the given entry, decompressing on-the-fly if needed.</summary>
    public Stream OpenEntry(string name)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_entries.TryGetValue(DatArchivePath.Normalize(name), out var entry))
            throw new KeyNotFoundException($"Entry not found: {name}");

        return DatEntryReader.OpenEntry(_mmf, entry);
    }

    /// <summary>Reads and decompresses the given entry.</summary>
    public byte[] ReadEntry(ArchiveEntry entry)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return DatEntryReader.ReadEntryData(_mmf, entry).ToArray();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _mmf.Dispose();
    }
}
