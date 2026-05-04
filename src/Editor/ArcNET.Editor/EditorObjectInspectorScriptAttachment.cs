using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// One known script attachment point on an object/proto inspector target.
/// </summary>
public sealed class EditorObjectInspectorScriptAttachment
{
    /// <summary>
    /// Typed known attachment point.
    /// </summary>
    public required ScriptAttachmentPoint AttachmentPoint { get; init; }

    /// <summary>
    /// Raw script-slot flags.
    /// </summary>
    public required uint Flags { get; init; }

    /// <summary>
    /// Raw script-slot counters.
    /// </summary>
    public required uint Counters { get; init; }

    /// <summary>
    /// Referenced script identifier, or 0 when the attachment point is empty.
    /// </summary>
    public required int ScriptId { get; init; }

    /// <summary>
    /// Resolved script detail when <see cref="ScriptId"/> maps to one loaded script asset.
    /// </summary>
    public EditorObjectInspectorScriptReference? Script { get; init; }

    /// <summary>
    /// Zero-based slot index in the raw object script array.
    /// </summary>
    public int SlotIndex => (int)AttachmentPoint;

    /// <summary>
    /// Returns <see langword="true"/> when the slot has no script, flags, or counters.
    /// </summary>
    public bool IsEmpty => ScriptId == 0 && Flags == 0 && Counters == 0;

    /// <summary>
    /// Returns <see langword="true"/> when the slot references a script identifier that is not currently loaded.
    /// </summary>
    public bool IsMissingScript => ScriptId > 0 && Script is null;
}
