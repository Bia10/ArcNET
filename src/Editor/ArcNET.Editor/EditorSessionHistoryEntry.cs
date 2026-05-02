namespace ArcNET.Editor;

/// <summary>
/// One applied or persisted session change group recorded in the undo/redo history.
/// </summary>
public sealed class EditorSessionHistoryEntry
{
    /// <summary>
    /// Host-supplied or synthesized label for the change group.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// UTC timestamp when the change group was applied or saved.
    /// </summary>
    public required DateTimeOffset RecordedAtUtc { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when the change group was persisted to disk.
    /// </summary>
    public required bool PersistedToDisk { get; init; }

    /// <summary>
    /// Immutable summary of the assets affected by the recorded change group.
    /// </summary>
    public required IReadOnlyList<EditorSessionChange> Changes { get; init; }

    /// <summary>
    /// Project/session state restored when this history entry is applied or redone.
    /// </summary>
    public required EditorSessionProjectStateSummary ProjectState { get; init; }
}
