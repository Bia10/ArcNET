namespace ArcNET.Archive;

/// <summary>Entry flags for DAT archive entries.</summary>
[Flags]
public enum DatEntryFlags : uint
{
    Plain = 0x001, // uncompressed
    Compressed = 0x002, // zlib deflate (standard framing)
    InArchive = 0x100, // data lives inside the .dat
    Overridden = 0x200, // shadowed by an extracted copy
    Directory = 0x400, // directory pseudo-entry
    Ignored = 0x800, // from .dat.ignore file
}

/// <summary>A single file entry within a DAT archive.</summary>
public sealed class ArchiveEntry
{
    /// <summary>Gets the virtual path of this file within the archive.</summary>
    public required string Path { get; init; }

    /// <summary>Gets the entry flags.</summary>
    public required DatEntryFlags Flags { get; init; }

    /// <summary>Gets the uncompressed size in bytes.</summary>
    public required int UncompressedSize { get; init; }

    /// <summary>Gets the compressed size in bytes.</summary>
    public required int CompressedSize { get; init; }

    /// <summary>Gets the absolute byte offset within the archive file.</summary>
    public required int Offset { get; init; }

    /// <summary>Gets a value indicating whether this entry uses zlib compression.</summary>
    public bool IsCompressed => (Flags & DatEntryFlags.Compressed) != 0;

    /// <summary>Gets a value indicating whether this entry is a directory pseudo-entry.</summary>
    public bool IsDirectory => (Flags & DatEntryFlags.Directory) != 0;
}
