using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// One reverse reference from an asset to an art resource identifier.
/// </summary>
public sealed class EditorArtReference
{
    /// <summary>
    /// Referencing asset.
    /// </summary>
    public required EditorAssetEntry Asset { get; init; }

    /// <summary>
    /// Parsed format of the referencing asset.
    /// </summary>
    public FileFormat Format => Asset.Format;

    /// <summary>
    /// Referenced art resource identifier.
    /// </summary>
    public required uint ArtId { get; init; }

    /// <summary>
    /// Number of references to this art identifier found inside the asset.
    /// </summary>
    public required int Count { get; init; }
}
