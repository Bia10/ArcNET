using System.IO.Compression;
using System.IO.MemoryMappedFiles;

namespace ArcNET.Archive;

internal static class DatEntryReader
{
    public static Stream OpenEntry(MemoryMappedFile mmf, ArchiveEntry entry)
    {
        var rawStream = mmf.CreateViewStream(entry.Offset, entry.CompressedSize, MemoryMappedFileAccess.Read);
        if (!entry.IsCompressed)
            return rawStream;

        return new ZLibStream(rawStream, CompressionMode.Decompress, leaveOpen: false);
    }

    public static ReadOnlyMemory<byte> ReadEntryData(MemoryMappedFile mmf, ArchiveEntry entry)
    {
        var buffer = new byte[entry.UncompressedSize];
        using var stream = OpenEntry(mmf, entry);
        stream.ReadExactly(buffer);
        return buffer;
    }
}
