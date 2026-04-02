using ArcNET.Core;

namespace ArcNET.Archive;

/// <summary>
/// Provides read access to Arcanum DAT archive files.
/// </summary>
public sealed class DatArchive : IDisposable
{
    private readonly FileStream _stream;
    private readonly IReadOnlyList<ArchiveEntry> _entries;
    private bool _disposed;

    private DatArchive(FileStream stream, IReadOnlyList<ArchiveEntry> entries)
    {
        _stream = stream;
        _entries = entries;
    }

    /// <summary>Gets all entries in this archive.</summary>
    public IReadOnlyList<ArchiveEntry> Entries => _entries;

    /// <summary>Opens an archive file and reads its directory.</summary>
    public static DatArchive Open(string path)
    {
        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        try
        {
            var entries = ReadDirectory(stream);
            return new DatArchive(stream, entries);
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    /// <summary>Finds an entry by its virtual path (case-insensitive).</summary>
    public ArchiveEntry? FindEntry(string virtualPath) =>
        _entries.FirstOrDefault(e => string.Equals(e.Path, virtualPath, StringComparison.OrdinalIgnoreCase));

    /// <summary>Reads the raw bytes of the given entry.</summary>
    public byte[] ReadEntry(ArchiveEntry entry)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _stream.Seek(entry.Offset, SeekOrigin.Begin);
        var buffer = new byte[entry.UncompressedSize];
        _ = _stream.Read(buffer, 0, buffer.Length);
        return buffer;
    }

    private static IReadOnlyList<ArchiveEntry> ReadDirectory(FileStream stream)
    {
        // Read last 8 bytes: [directory offset (4)] [archive size (4)]
        stream.Seek(-8, SeekOrigin.End);
        var footer = new byte[8];
        _ = stream.Read(footer, 0, 8);
        var reader = new SpanReader(footer);
        var dirOffset = reader.ReadInt32();
        _ = reader.ReadInt32(); // archive size — not needed

        stream.Seek(dirOffset, SeekOrigin.Begin);
        var dirBytes = new byte[stream.Length - dirOffset - 8];
        _ = stream.Read(dirBytes, 0, dirBytes.Length);
        var dirReader = new SpanReader(dirBytes);

        var entries = new List<ArchiveEntry>();
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

            entries.Add(
                new ArchiveEntry
                {
                    Path = path,
                    UncompressedSize = uncompressedSize,
                    CompressedSize = compressedSize,
                    Offset = offset,
                }
            );
        }

        return entries;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _stream.Dispose();
    }
}
