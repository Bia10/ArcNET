namespace ArcNET.Editor;

/// <summary>
/// Identifies the backing source that supplied an asset to the current workspace.
/// </summary>
public enum EditorAssetSourceKind
{
    /// <summary>
    /// The asset came from a loose file on disk.
    /// </summary>
    LooseFile,

    /// <summary>
    /// The asset came from an entry inside a DAT archive.
    /// </summary>
    DatArchive,
}
