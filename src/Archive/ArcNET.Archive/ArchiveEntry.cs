namespace ArcNET.Archive;

/// <summary>A single file entry within a DAT archive.</summary>
public sealed class ArchiveEntry
{
    /// <summary>Gets the virtual path of this file within the archive.</summary>
    public required string Path { get; init; }

    /// <summary>Gets the uncompressed size in bytes.</summary>
    public required int UncompressedSize { get; init; }

    /// <summary>Gets the compressed size in bytes (0 if not compressed).</summary>
    public required int CompressedSize { get; init; }

    /// <summary>Gets the byte offset within the archive file.</summary>
    public required int Offset { get; init; }

    /// <summary>Gets a value indicating whether this entry is compressed.</summary>
    public bool IsCompressed => CompressedSize > 0;
}
