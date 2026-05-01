using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// One dialog asset plus higher-level graph semantics derived from its entries.
/// </summary>
public sealed class EditorDialogDefinition
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
    /// Dialog identifier derived from the numeric prefix in the asset path.
    /// </summary>
    public required int DialogId { get; init; }

    /// <summary>
    /// Total entry count in the dialog file.
    /// </summary>
    public required int EntryCount { get; init; }

    /// <summary>
    /// Number of NPC reply lines.
    /// </summary>
    public required int NpcEntryCount { get; init; }

    /// <summary>
    /// Number of PC option lines.
    /// </summary>
    public required int PcOptionCount { get; init; }

    /// <summary>
    /// Number of engine control entries such as <c>E:</c>, <c>R:</c>, or <c>T:</c>.
    /// </summary>
    public required int ControlEntryCount { get; init; }

    /// <summary>
    /// Number of positive response edges that target another dialog entry.
    /// </summary>
    public required int TransitionCount { get; init; }

    /// <summary>
    /// Number of terminal entries whose response value ends the conversation.
    /// </summary>
    public required int TerminalEntryCount { get; init; }

    /// <summary>
    /// Ordered dialog nodes with preserved entry content plus derived graph flags.
    /// </summary>
    public required IReadOnlyList<EditorDialogNode> Nodes { get; init; }

    /// <summary>
    /// Entry numbers with no inbound positive responses from other entries in the same dialog.
    /// </summary>
    public required IReadOnlyList<int> RootEntryNumbers { get; init; }

    /// <summary>
    /// Positive response targets referenced by entries but not defined inside the same dialog.
    /// </summary>
    public required IReadOnlyList<int> MissingResponseTargetNumbers { get; init; }

    /// <summary>
    /// Whether the dialog references any missing response targets.
    /// </summary>
    public bool HasMissingResponseTargets => MissingResponseTargetNumbers.Count > 0;
}
