using System.Buffers.Binary;
using System.Collections.Frozen;
using System.IO.MemoryMappedFiles;
using ArcNET.Core;

namespace ArcNET.Archive;

internal static class DatDirectoryReader
{
    // FourCC magic constants:
    //   SDL_FOURCC(A,B,C,D) = A | (B<<8) | (C<<16) | (D<<24)  (little-endian on-wire)
    //   SDL_FOURCC(' ','T','A','D') = 0x44415420
    //   SDL_FOURCC('1','T','A','D') = 0x44415431
    private const uint FourCcDat = 0x44415420u; // ' TAD'
    private const uint FourCcDat1 = 0x44415431u; // '1TAD'

    public static FrozenDictionary<string, ArchiveEntry> Read(MemoryMappedFile mmf, long fileLength)
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
        _ = footerReader.ReadUInt32();
        var entryTableOffset = footerReader.ReadUInt32();

        if (magic != FourCcDat && magic != FourCcDat1)
            throw new InvalidDataException(
                $"Unsupported DAT magic 0x{magic:X8}. Expected 0x{FourCcDat:X8} (DAT) or 0x{FourCcDat1:X8} (DAT1)."
            );

        var tableSizePos = fileLength - 4 - entryTableOffset;
        using var sizeView = mmf.CreateViewStream(tableSizePos, 4, MemoryMappedFileAccess.Read);
        Span<byte> sizeBuf = stackalloc byte[4];
        sizeView.ReadExactly(sizeBuf);
        var entryTableSize = BinaryPrimitives.ReadUInt32LittleEndian(sizeBuf);
        var baseOffset = fileLength - entryTableSize - entryTableOffset;

        var tableDataPos = tableSizePos + 4;
        var tableDataSize = (int)(fileLength - tableDataPos);
        using var tableView = mmf.CreateViewStream(tableDataPos, tableDataSize, MemoryMappedFileAccess.Read);
        var tableBuffer = new byte[tableDataSize];
        tableView.ReadExactly(tableBuffer);

        return ReadEntries(tableBuffer, baseOffset);
    }

    private static FrozenDictionary<string, ArchiveEntry> ReadEntries(byte[] tableBuffer, long baseOffset)
    {
        var reader = new SpanReader(tableBuffer);
        var entriesCount = reader.ReadInt32();
        var entries = new Dictionary<string, ArchiveEntry>(entriesCount, StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < entriesCount; index++)
        {
            if (reader.Remaining < 4)
                break;

            var nameLen = reader.ReadInt32();
            if (nameLen <= 0 || nameLen > reader.Remaining)
                break;

            var nameBytes = reader.ReadBytes(nameLen);
            var nullIndex = nameBytes.IndexOf((byte)0);
            var path = System.Text.Encoding.ASCII.GetString(nullIndex >= 0 ? nameBytes[..nullIndex] : nameBytes);

            reader.Skip(4);

            var flags = (DatEntryFlags)reader.ReadUInt32();
            var uncompressedSize = reader.ReadInt32();
            var compressedSize = reader.ReadInt32();
            var relativeOffset = reader.ReadInt32();

            if ((flags & DatEntryFlags.Directory) != 0)
                continue;

            entries[path] = new ArchiveEntry
            {
                Path = path,
                Flags = flags,
                UncompressedSize = uncompressedSize,
                CompressedSize = compressedSize,
                Offset = checked((int)(relativeOffset + baseOffset)),
            };
        }

        return entries.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }
}
