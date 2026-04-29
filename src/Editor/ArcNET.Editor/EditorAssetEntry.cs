using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// Describes one parsed game-data asset exposed through an <see cref="EditorWorkspace"/>.
/// </summary>
public sealed class EditorAssetEntry
{
    /// <summary>
    /// Relative or virtual asset path using forward slashes, for example <c>mes/game.mes</c>.
    /// </summary>
    public required string AssetPath { get; init; }

    /// <summary>
    /// Parsed file format for this asset.
    /// </summary>
    public required FileFormat Format { get; init; }

    /// <summary>
    /// Number of parsed records contributed by this asset.
    /// Message files can contribute multiple entries; sector, proto, and mob files usually contribute one.
    /// </summary>
    public required int ItemCount { get; init; }

    /// <summary>
    /// Backing source kind that supplied the winning version of this asset.
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
}
