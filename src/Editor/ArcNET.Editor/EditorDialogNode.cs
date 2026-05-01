namespace ArcNET.Editor;

/// <summary>
/// One dialog node exposed through the editor-facing dialog graph surface.
/// </summary>
public sealed class EditorDialogNode
{
    /// <summary>
    /// Entry number that uniquely identifies this node inside the dialog asset.
    /// </summary>
    public required int EntryNumber { get; init; }

    /// <summary>
    /// Main reply or option text.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// Raw gender field carried by the source dialog entry.
    /// </summary>
    public required string GenderField { get; init; }

    /// <summary>
    /// Minimum intelligence requirement for player-option entries; <c>0</c> for NPC replies.
    /// </summary>
    public required int IntelligenceRequirement { get; init; }

    /// <summary>
    /// Raw condition expression attached to the entry.
    /// </summary>
    public required string Conditions { get; init; }

    /// <summary>
    /// Raw action expression attached to the entry.
    /// </summary>
    public required string Actions { get; init; }

    /// <summary>
    /// Response target entry number. Positive values target another node, <c>0</c> ends the dialog,
    /// and negative values are preserved as non-graph control-like values.
    /// </summary>
    public required int ResponseTargetNumber { get; init; }

    /// <summary>
    /// High-level node kind derived from the entry text and IQ field.
    /// </summary>
    public required EditorDialogNodeKind Kind { get; init; }

    /// <summary>
    /// Whether no other positive response inside the dialog targets this node.
    /// </summary>
    public required bool IsRoot { get; init; }

    /// <summary>
    /// Whether this node references a positive response target that is not defined in the same dialog.
    /// </summary>
    public required bool HasMissingResponseTarget { get; init; }

    /// <summary>
    /// Whether the node transitions to another dialog node.
    /// </summary>
    public bool HasTransition => ResponseTargetNumber > 0;

    /// <summary>
    /// Whether the node ends the dialog.
    /// </summary>
    public bool IsTerminal => ResponseTargetNumber == 0;
}
