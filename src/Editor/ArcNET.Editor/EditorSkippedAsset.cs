using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// One winning install-backed asset that was skipped because the current parsers could not load it.
/// </summary>
public sealed class EditorSkippedAsset
{
    /// <summary>
    /// Normalized workspace asset path.
    /// </summary>
    public required string AssetPath { get; init; }

    /// <summary>
    /// Parsed format inferred from <see cref="AssetPath"/>.
    /// </summary>
    public required FileFormat Format { get; init; }

    /// <summary>
    /// Winning source kind for the skipped asset.
    /// </summary>
    public required EditorAssetSourceKind SourceKind { get; init; }

    /// <summary>
    /// Physical archive or loose-file path that won for this asset.
    /// </summary>
    public required string SourcePath { get; init; }

    /// <summary>
    /// Archive entry path when the winning source came from a DAT archive; otherwise <see langword="null"/>.
    /// </summary>
    public string? SourceEntryPath { get; init; }

    /// <summary>
    /// User-facing reason the asset was skipped.
    /// </summary>
    public required string Reason { get; init; }
}
