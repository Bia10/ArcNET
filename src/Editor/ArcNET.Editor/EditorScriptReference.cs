using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// One reverse reference from an asset to a script identifier.
/// </summary>
public sealed class EditorScriptReference
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
    /// Referenced script identifier.
    /// </summary>
    public required int ScriptId { get; init; }

    /// <summary>
    /// Number of references to this script identifier found inside the asset.
    /// </summary>
    public required int Count { get; init; }
}
