using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// One reverse reference from an asset to a prototype number.
/// </summary>
public sealed class EditorProtoReference
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
    /// Referenced proto number.
    /// </summary>
    public required int ProtoNumber { get; init; }

    /// <summary>
    /// Number of references to this proto number found inside the asset.
    /// </summary>
    public required int Count { get; init; }
}
