using System.Collections.Frozen;
using System.IO.MemoryMappedFiles;
using ArcNET.Core;

namespace ArcNET.Archive;

/// <summary>
/// Provides read access to Arcanum DAT archive files using memory-mapped I/O.
/// The archive is never fully loaded into RAM; entry extraction uses zero-copy MMF views.
/// </summary>
public sealed class DatArchive : IDisposable
{
    private readonly MemoryMappedFile _mmf;
    private readonly long _fileLength;
    private readonly FrozenDictionary<string, ArchiveEntry> _entries;
    private bool _disposed;

    private DatArchive(MemoryMappedFile mmf, long fileLength, FrozenDictionary<string, ArchiveEntry> entries)
    {
        _mmf = mmf;
        _fileLength = fileLength;
        _entries = entries;
    }

    /// <summary>Gets all entries in this archive.</summary>
    public IReadOnlyCollection<ArchiveEntry> Entries => _entries.Values;

    /// <summary>Opens an archive file and reads its directory using a memory-mapped view.</summary>
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
            var entries = ReadDirectory(mmf, fi.Length);
            return new DatArchive(mmf, fi.Length, entries);
        }
        catch
        {
            mmf.Dispose();
            throw;
        }
    }

    /// <summary>Finds an entry by its virtual path (case-insensitive).</summary>
    public ArchiveEntry? FindEntry(string virtualPath) =>
        _entries.TryGetValue(virtualPath, out var entry) ? entry : null;

    /// <summary>
    /// Returns the raw bytes of the given entry.
    /// The data is copied from the memory-mapped view into a new <see cref="ReadOnlyMemory{T}"/>.
    /// </summary>
    public ReadOnlyMemory<byte> GetEntryData(string name)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_entries.TryGetValue(name, out var entry))
            throw new KeyNotFoundException($"Entry not found: {name}");

        var buf = new byte[entry.UncompressedSize];
        using var view = _mmf.CreateViewStream(entry.Offset, entry.UncompressedSize, MemoryMappedFileAccess.Read);
        _ = view.Read(buf, 0, buf.Length);
        return buf;
    }

    /// <summary>Opens a streaming view over the given entry without loading the full archive.</summary>
    public Stream OpenEntry(string name)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_entries.TryGetValue(name, out var entry))
            throw new KeyNotFoundException($"Entry not found: {name}");

        return _mmf.CreateViewStream(entry.Offset, entry.UncompressedSize, MemoryMappedFileAccess.Read);
    }

    /// <summary>Reads the raw bytes of the given entry (legacy overload, allocates).</summary>
    public byte[] ReadEntry(ArchiveEntry entry)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var buf = new byte[entry.UncompressedSize];
        using var view = _mmf.CreateViewStream(entry.Offset, entry.UncompressedSize, MemoryMappedFileAccess.Read);
        _ = view.Read(buf, 0, buf.Length);
        return buf;
    }

    private static FrozenDictionary<string, ArchiveEntry> ReadDirectory(MemoryMappedFile mmf, long fileLength)
    {
        // Footer: last 8 bytes = [directory offset (4)] [archive size (4)]
        const int FooterSize = 8;
        using var footerView = mmf.CreateViewStream(fileLength - FooterSize, FooterSize, MemoryMappedFileAccess.Read);
        var footer = new byte[FooterSize];
        _ = footerView.Read(footer, 0, FooterSize);

        var footerReader = new SpanReader(footer);
        var dirOffset = footerReader.ReadInt32();
        _ = footerReader.ReadInt32(); // archive size — not used

        var dirSize = (int)(fileLength - dirOffset - FooterSize);
        using var dirView = mmf.CreateViewStream(dirOffset, dirSize, MemoryMappedFileAccess.Read);
        var dirBytes = new byte[dirSize];
        _ = dirView.Read(dirBytes, 0, dirSize);
        var dirReader = new SpanReader(dirBytes);

        var dict = new Dictionary<string, ArchiveEntry>(StringComparer.OrdinalIgnoreCase);
        while (dirReader.Remaining >= 4)
        {
            var nameLen = dirReader.ReadInt32();
            if (nameLen <= 0 || nameLen > dirReader.Remaining)
                break;

            var nameBytes = dirReader.ReadBytes(nameLen);
            var path = System.Text.Encoding.ASCII.GetString(nameBytes);
            var uncompressedSize = dirReader.ReadInt32();
            var compressedSize = dirReader.ReadInt32();
            var offset = dirReader.ReadInt32();

            dict[path] = new ArchiveEntry
            {
                Path = path,
                UncompressedSize = uncompressedSize,
                CompressedSize = compressedSize,
                Offset = offset,
            };
        }

        return dict.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
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
