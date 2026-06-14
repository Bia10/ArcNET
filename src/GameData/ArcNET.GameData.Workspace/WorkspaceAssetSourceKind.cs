namespace ArcNET.GameData.Workspace;

/// <summary>
/// Identifies the backing source that supplied the winning version of one workspace asset.
/// </summary>
public enum WorkspaceAssetSourceKind
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
