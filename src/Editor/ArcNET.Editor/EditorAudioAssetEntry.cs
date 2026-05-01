namespace ArcNET.Editor;

/// <summary>
/// Describes one loaded audio asset exposed through an <see cref="EditorWorkspace"/>.
/// </summary>
public sealed class EditorAudioAssetEntry
{
    /// <summary>
    /// Relative or virtual asset path using forward slashes.
    /// </summary>
    public required string AssetPath { get; init; }

    /// <summary>
    /// Backing source kind that supplied the winning version of the asset.
    /// </summary>
    public required EditorAssetSourceKind SourceKind { get; init; }

    /// <summary>
    /// Absolute loose-file path or absolute DAT archive path that supplied this asset.
    /// </summary>
    public required string SourcePath { get; init; }

    /// <summary>
    /// Archive entry path using forward slashes when <see cref="SourceKind"/> is <see cref="EditorAssetSourceKind.DatArchive"/>;
    /// otherwise <see langword="null"/> for loose files.
    /// </summary>
    public string? SourceEntryPath { get; init; }

    /// <summary>
    /// Raw asset size in bytes.
    /// </summary>
    public required int ByteLength { get; init; }
}
