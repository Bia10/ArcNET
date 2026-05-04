using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// Resolved compiled-script detail for one object/proto script attachment.
/// </summary>
public sealed class EditorObjectInspectorScriptReference
{
    /// <summary>
    /// Defining script asset.
    /// </summary>
    public required EditorAssetEntry Asset { get; init; }

    /// <summary>
    /// Script identifier referenced by the owning attachment slot.
    /// </summary>
    public required int ScriptId { get; init; }

    /// <summary>
    /// Current script description.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Current script flags.
    /// </summary>
    public required ScriptFlags Flags { get; init; }

    /// <summary>
    /// Total condition/action entry count in the resolved script.
    /// </summary>
    public required int EntryCount { get; init; }

    /// <summary>
    /// Number of non-empty attachment slots in the resolved script.
    /// </summary>
    public required int ActiveAttachmentCount { get; init; }

    /// <summary>
    /// Known attachment points currently used by the resolved script.
    /// </summary>
    public required IReadOnlyList<ScriptAttachmentPoint> ActiveAttachmentPoints { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when the resolved script uses one or more non-empty unknown attachment slots.
    /// </summary>
    public bool HasUnknownAttachmentSlots { get; init; }

    internal static EditorObjectInspectorScriptReference Create(EditorAssetEntry asset, ScrFile script, int scriptId)
    {
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentNullException.ThrowIfNull(script);

        var activeSlots = ScriptValidator.GetActiveAttachmentSlots(script).ToArray();
        var knownPoints = activeSlots
            .Where(static slot => ScriptValidator.IsKnownAttachmentSlot(slot))
            .Select(static slot => (ScriptAttachmentPoint)slot)
            .ToArray();

        return new EditorObjectInspectorScriptReference
        {
            Asset = asset,
            ScriptId = scriptId,
            Description = script.Description,
            Flags = script.Flags,
            EntryCount = script.Entries.Count,
            ActiveAttachmentCount = activeSlots.Length,
            ActiveAttachmentPoints = knownPoints,
            HasUnknownAttachmentSlots = activeSlots.Length != knownPoints.Length,
        };
    }
}
