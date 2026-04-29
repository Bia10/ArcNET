using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// One script asset plus higher-level attachment semantics derived from its slots.
/// </summary>
public sealed class EditorScriptDefinition
{
    /// <summary>
    /// Defining asset.
    /// </summary>
    public required EditorAssetEntry Asset { get; init; }

    /// <summary>
    /// Parsed format of the defining asset.
    /// </summary>
    public FileFormat Format => Asset.Format;

    /// <summary>
    /// Script identifier derived from the numeric prefix in the asset path.
    /// </summary>
    public required int ScriptId { get; init; }

    /// <summary>
    /// Human-readable script description.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Script behavior flags.
    /// </summary>
    public required ScriptFlags Flags { get; init; }

    /// <summary>
    /// Total slot count in the compiled script file.
    /// </summary>
    public required int EntryCount { get; init; }

    /// <summary>
    /// Number of non-empty attachment slots.
    /// </summary>
    public required int ActiveAttachmentCount { get; init; }

    /// <summary>
    /// Zero-based slot indexes that contain non-empty logic.
    /// </summary>
    public required IReadOnlyList<int> ActiveAttachmentSlots { get; init; }

    /// <summary>
    /// Known attachment-point enum values for the non-empty slots.
    /// Unknown slots are omitted from this list.
    /// </summary>
    public required IReadOnlyList<ScriptAttachmentPoint> ActiveAttachmentPoints { get; init; }

    /// <summary>
    /// Whether any non-empty slots fall outside the known <see cref="ScriptAttachmentPoint"/> enum.
    /// </summary>
    public bool HasUnknownAttachmentSlots => ActiveAttachmentSlots.Count != ActiveAttachmentPoints.Count;
}
