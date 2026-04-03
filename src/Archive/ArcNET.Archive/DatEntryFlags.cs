namespace ArcNET.Archive;

/// <summary>Entry flags for DAT archive entries.</summary>
[Flags]
public enum DatEntryFlags : ushort
{
    /// <summary>No flags set.</summary>
    None = 0,

    /// <summary>Data is stored uncompressed.</summary>
    Plain = 0x1,

    /// <summary>Data is zlib-deflate compressed (standard framing).</summary>
    Compressed = 0x2,

    /// <summary>Data lives inside the .dat container.</summary>
    InArchive = 0x100,

    /// <summary>Entry is shadowed by an extracted copy on disk.</summary>
    Overridden = 0x200,

    /// <summary>Pseudo-entry representing a directory node.</summary>
    Directory = 0x400,

    /// <summary>Entry is suppressed via a .dat.ignore file.</summary>
    Ignored = 0x800,
}
