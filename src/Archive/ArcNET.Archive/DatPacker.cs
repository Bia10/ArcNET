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

        // Write directory table
        var dirOffset = (int)output.Position;
        var dirBuf = new ArrayBufferWriter<byte>();
        var dirWriter = new SpanWriter(dirBuf);

        foreach (var (virtualPath, size, startOffset) in entryInfos)
        {
            var nameBytes = Encoding.ASCII.GetBytes(virtualPath);
            dirWriter.WriteInt32(nameBytes.Length);
            dirWriter.WriteBytes(nameBytes);
            dirWriter.WriteInt32(size); // uncompressed size
            dirWriter.WriteInt32(0); // compressed size (0 = not compressed)
            dirWriter.WriteInt32(startOffset);
        }

        await output.WriteAsync(dirBuf.WrittenMemory, cancellationToken).ConfigureAwait(false);

        // Write 8-byte footer: [directory offset (4)] [archive size (4)]
        var archiveSize = (int)(output.Position + 8);
        Span<byte> footer = stackalloc byte[8];
        BinaryPrimitives.WriteInt32LittleEndian(footer, dirOffset);
        BinaryPrimitives.WriteInt32LittleEndian(footer[4..], archiveSize);
        await output.WriteAsync(footer.ToArray(), cancellationToken).ConfigureAwait(false);

        progress?.Report(1f);
    }
}
