using System.Buffers;
using System.Buffers.Binary;
using System.Text;
using ArcNET.Core;

namespace ArcNET.Archive;

/// <summary>Packs a directory of files into an Arcanum DAT archive.</summary>
public static class DatPacker
{
    /// <summary>
    /// Recursively packs all files under <paramref name="inputDir"/> into the DAT archive
    /// at <paramref name="outputPath"/>.
    /// </summary>
    /// <param name="inputDir">Source directory to pack.</param>
    /// <param name="outputPath">Destination archive file path.</param>
    /// <param name="progress">Optional progress callback (value in [0, 1]).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task PackAsync(
        string inputDir,
        string outputPath,
        IProgress<float>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputDir);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        if (!Directory.Exists(inputDir))
            throw new DirectoryNotFoundException($"Input directory not found: {inputDir}");

        var rootFull = Path.GetFullPath(inputDir);
        var files = Directory.GetFiles(inputDir, "*", SearchOption.AllDirectories);

        // Build the entry metadata (offsets filled in after data is written)
        var entryInfos = new List<(string VirtualPath, int Size, int StartOffset)>(files.Length);

        var outputDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDir))
            Directory.CreateDirectory(outputDir);

        await using var output = new FileStream(
            outputPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 65_536,
            FileOptions.Asynchronous
        );

        // Write all file data sequentially
        for (var i = 0; i < files.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fileFull = Path.GetFullPath(files[i]);
            var relativeRaw = fileFull[rootFull.Length..]
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var virtualPath = relativeRaw.Replace(Path.DirectorySeparatorChar, '\\');

            var fileData = await File.ReadAllBytesAsync(fileFull, cancellationToken).ConfigureAwait(false);
            var startOffset = (int)output.Position;

            await output.WriteAsync(fileData, cancellationToken).ConfigureAwait(false);
            entryInfos.Add((virtualPath, fileData.Length, startOffset));

            progress?.Report((float)(i + 1) / (files.Length + 1));
        }

        // Write directory table in Arcanum DAT format:
        //   entry_table_size (4): absolute position of entries_count
        //   entries_count (4)
        //   per-entry: nameLen(4) + name+NUL(nameLen) + skip(4) + flags(4) + uncompSize(4) + compSize(4) + offset(4)
        var tableSizePos = (int)output.Position;
        var entriesCountPos = tableSizePos + 4;
        var dirBuf = new ArrayBufferWriter<byte>();
        var dirWriter = new SpanWriter(dirBuf);

        // entry_table_size = position of entries_count
        dirWriter.WriteUInt32((uint)entriesCountPos);
        // entries_count
        dirWriter.WriteInt32(entryInfos.Count);

        Span<byte> stackNameBuf = stackalloc byte[StackAllocPolicy.MaxStackAllocBytes];
        foreach (var (virtualPath, size, startOffset) in entryInfos)
        {
            // Encode the virtual path as ASCII without a heap allocation.
            // Arcanum paths are always ASCII and well under MaxStackAllocBytes (256).
            var byteCount = Encoding.ASCII.GetByteCount(virtualPath);
            var nameLen = byteCount + 1; // includes null terminator
            dirWriter.WriteInt32(nameLen);
            if (byteCount <= StackAllocPolicy.MaxStackAllocBytes)
            {
                var nameBuf = stackNameBuf[..byteCount];
                Encoding.ASCII.GetBytes(virtualPath, nameBuf);
                dirWriter.WriteBytes(nameBuf);
            }
            else
            {
                var rented = ArrayPool<byte>.Shared.Rent(byteCount);
                try
                {
                    Encoding.ASCII.GetBytes(virtualPath, rented.AsSpan(0, byteCount));
                    dirWriter.WriteBytes(rented.AsSpan(0, byteCount));
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(rented);
                }
            }

            dirWriter.WriteByte(0); // null terminator
            dirWriter.WriteUInt32(0u); // unknown skip field
            dirWriter.WriteUInt32(0x001u); // flags: Plain
            dirWriter.WriteInt32(size); // uncompressedSize
            dirWriter.WriteInt32(size); // compressedSize (= size for plain)
            dirWriter.WriteInt32(startOffset); // absolute file offset
        }

        await output.WriteAsync(dirBuf.WrittenMemory, cancellationToken).ConfigureAwait(false);

        // Write 12-byte footer: magic(4) + nameTableSize(4) + entryTableOffset(4)
        //   entryTableOffset = fileLength - 4 - tableSizePos  (keeps baseOffset = 0)
        var fileLength = (int)(output.Position + 12);
        var entryTableOffset = fileLength - 4 - tableSizePos;
        Span<byte> footer = stackalloc byte[12];
        BinaryPrimitives.WriteUInt32LittleEndian(footer, 0x44415420u); // 'DAT ' magic
        BinaryPrimitives.WriteUInt32LittleEndian(footer[4..], 0u); // nameTableSize (unused)
        BinaryPrimitives.WriteUInt32LittleEndian(footer[8..], (uint)entryTableOffset);
        // 12 bytes into a 65 KiB-buffered FileStream — write synchronously to avoid heap allocation.
        output.Write(footer);

        progress?.Report(1f);
    }
}
