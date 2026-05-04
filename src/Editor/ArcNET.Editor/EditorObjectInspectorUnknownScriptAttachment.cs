using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// One non-empty unknown script attachment slot on an object/proto inspector target.
/// </summary>
public sealed class EditorObjectInspectorUnknownScriptAttachment
{
    /// <summary>
    /// Zero-based slot index in the raw object script array.
    /// </summary>
    public required int SlotIndex { get; init; }

    /// <summary>
    /// Raw script-slot flags.
    /// </summary>
    public required uint Flags { get; init; }

    /// <summary>
    /// Raw script-slot counters.
    /// </summary>
    public required uint Counters { get; init; }

    /// <summary>
    /// Referenced script identifier for the unknown slot.
    /// </summary>
    public required int ScriptId { get; init; }

    /// <summary>
    /// Resolved script detail when <see cref="ScriptId"/> maps to one loaded script asset.
    /// </summary>
    public EditorObjectInspectorScriptReference? Script { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when the slot references a script identifier that is not currently loaded.
    /// </summary>
    public bool IsMissingScript => ScriptId > 0 && Script is null;
}
