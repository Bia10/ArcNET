namespace ArcNET.Editor;

/// <summary>
/// High-level dialog-node classification derived from one dialog entry.
/// </summary>
public enum EditorDialogNodeKind
{
    /// <summary>
    /// Standard NPC reply line.
    /// </summary>
    NpcReply = 0,

    /// <summary>
    /// Player option line gated by an intelligence requirement.
    /// </summary>
    PcOption = 1,

    /// <summary>
    /// Engine control entry such as <c>E:</c>, <c>R:</c>, or <c>T:</c>.
    /// </summary>
    Control = 2,
}
