using System.Buffers.Binary;
using System.Collections.Frozen;
using System.IO.Compression;
using System.IO.MemoryMappedFiles;
using ArcNET.Core;

namespace ArcNET.Archive;

/// <summary>
/// Provides read access to Arcanum DAT archive files (both arcanum1.dat-style and
/// Modules\Arcanum.dat-style — same binary format).
/// </summary>
public sealed class DatArchive : IDisposable
{
    // FourCC magic constants:
    //   SDL_FOURCC(A,B,C,D) = A | (B<<8) | (C<<16) | (D<<24)  (little-endian on-wire)
    //   SDL_FOURCC(' ','T','A','D') = 0x44415420
    //   SDL_FOURCC('1','T','A','D') = 0x44415431
    private const uint FourCcDat = 0x44415420u; // ' TAD'
    private const uint FourCcDat1 = 0x44415431u; // '1TAD'

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
            var entries = ReadDirectory(mmf, fi.Length);
            return new DatArchive(mmf, fi.Length, entries);
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
        var normalized = virtualPath.Replace('/', '\\');
        return _entries.TryGetValue(normalized, out var entry) ? entry : null;
    }

    /// <summary>Returns the decompressed bytes of the given entry.</summary>
    public ReadOnlyMemory<byte> GetEntryData(string name)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_entries.TryGetValue(name.Replace('/', '\\'), out var entry))
            throw new KeyNotFoundException($"Entry not found: {name}");
        return ReadEntryData(entry);
    }

    /// <summary>Opens a readable stream over the given entry, decompressing on-the-fly if needed.</summary>
    public Stream OpenEntry(string name)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_entries.TryGetValue(name.Replace('/', '\\'), out var entry))
            throw new KeyNotFoundException($"Entry not found: {name}");

        var rawStream = _mmf.CreateViewStream(entry.Offset, entry.CompressedSize, MemoryMappedFileAccess.Read);
        if (!entry.IsCompressed)
            return rawStream;

        return new ZLibStream(rawStream, CompressionMode.Decompress, leaveOpen: false);
    }

    /// <summary>Reads and decompresses the given entry.</summary>
    public byte[] ReadEntry(ArchiveEntry entry)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return ReadEntryData(entry).ToArray();
    }

    private ReadOnlyMemory<byte> ReadEntryData(ArchiveEntry entry)
    {
        var buf = new byte[entry.UncompressedSize];
        using var rawStream = _mmf.CreateViewStream(entry.Offset, entry.CompressedSize, MemoryMappedFileAccess.Read);

        if (!entry.IsCompressed)
        {
            rawStream.ReadExactly(buf);
        }
        else
        {
            using var zlib = new ZLibStream(rawStream, CompressionMode.Decompress, leaveOpen: false);
            zlib.ReadExactly(buf);
        }

        return buf;
    }

    private static FrozenDictionary<string, ArchiveEntry> ReadDirectory(MemoryMappedFile mmf, long fileLength)
    {
        // ── Step 1: Read the 12-byte mandatory footer at end of file ──────────
        // Layout:
        //   filesize−12 : uint32  id             (FourCC magic)
        //   filesize−8  : uint32  name_table_size (malloc hint only)
        //   filesize−4  : uint32  entry_table_offset
        using var footerView = mmf.CreateViewStream(fileLength - 12, 12, MemoryMappedFileAccess.Read);
        Span<byte> footerBuf = stackalloc byte[12];
        footerView.ReadExactly(footerBuf);
        var footerReader = new SpanReader(footerBuf);
        var magic = footerReader.ReadUInt32();
        _ = footerReader.ReadUInt32(); // name_table_size — not used
        var entryTableOffset = footerReader.ReadUInt32();

        if (magic != FourCcDat && magic != FourCcDat1)
            throw new InvalidDataException(
                $"Unsupported DAT magic 0x{magic:X8}. Expected 0x{FourCcDat:X8} (DAT) or 0x{FourCcDat1:X8} (DAT1)."
            );

        // ── Step 2: Read entry_table_size (4 bytes immediately before the table)
        // Position: filesize − 4 − entry_table_offset
        var tableSizePos = fileLength - 4 - entryTableOffset;
        using var sizeView = mmf.CreateViewStream(tableSizePos, 4, MemoryMappedFileAccess.Read);
        Span<byte> sizeBuf = stackalloc byte[4];
        sizeView.ReadExactly(sizeBuf);
        var entryTableSize = BinaryPrimitives.ReadUInt32LittleEndian(sizeBuf);

        // ── Step 3: Compute base offset for all entry file offsets
        // entry->offset (on-disk, relative) + baseOffset = absolute position in file
        // baseOffset = filesize − entryTableSize − entryTableOffset
        var baseOffset = (long)fileLength - entryTableSize - entryTableOffset;

        // ── Step 4: Read the entry table (entries_count + entry records)
        // Starts 4 bytes after tableSizePos (past the entry_table_size field itself).
        // NOTE: entryTableSize is the absolute file position of entries_count — NOT a byte count.
        //       The actual byte span to read is from tableDataPos to EOF.
        var tableDataPos = tableSizePos + 4;
        var tableDataSize = (int)(fileLength - tableDataPos);
        using var tableView = mmf.CreateViewStream(tableDataPos, tableDataSize, MemoryMappedFileAccess.Read);
        var tableBuf = new byte[tableDataSize];
        tableView.ReadExactly(tableBuf);
        var r = new SpanReader(tableBuf);

        var entriesCount = r.ReadInt32();
        var dict = new Dictionary<string, ArchiveEntry>(entriesCount, StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < entriesCount; i++)
        {
            if (r.Remaining < 4)
                break;

            var nameLen = r.ReadInt32();
            if (nameLen <= 0 || nameLen > r.Remaining)
                break;

            var nameBytes = r.ReadBytes(nameLen);
            // nameLen includes the null terminator — find it to avoid a TrimEnd string allocation.
            var nullIdx = nameBytes.IndexOf((byte)0);
            var path = System.Text.Encoding.ASCII.GetString(nullIdx >= 0 ? nameBytes[..nullIdx] : nameBytes);

            r.Skip(4); // unknown field (unused)

            var flags = (DatEntryFlags)r.ReadUInt32();
            var uncompressedSize = r.ReadInt32();
            var compressedSize = r.ReadInt32();
            var relativeOffset = r.ReadInt32();
            var absoluteOffset = (int)(relativeOffset + baseOffset);

            // Skip directory pseudo-entries — they have no data
            if ((flags & DatEntryFlags.Directory) != 0)
                continue;

            dict[path] = new ArchiveEntry
            {
                Path = path,
                Flags = flags,
                UncompressedSize = uncompressedSize,
                CompressedSize = compressedSize,
                Offset = absoluteOffset,
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
