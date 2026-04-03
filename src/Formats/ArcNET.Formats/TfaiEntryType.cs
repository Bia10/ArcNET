namespace ArcNET.Formats;

/// <summary>
/// Entry type tag used in a TFAI save-game archive index stream.
/// </summary>
public enum TfaiEntryType : byte
{
    /// <summary>A file entry with a name and a payload size in the TFAF blob.</summary>
    File = 0,

    /// <summary>A directory entry that introduces a named group of children.</summary>
    Directory = 1,

    /// <summary>Closes the current directory scope.</summary>
    EndOfDirectory = 2,

    /// <summary>Terminates the entire index stream.</summary>
    EndOfFile = 3,
}
