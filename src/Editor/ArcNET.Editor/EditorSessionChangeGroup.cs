namespace ArcNET.Editor;

/// <summary>
/// One explicit, labeled session change group that can be applied, saved, or discarded as a unit.
/// </summary>
public sealed class EditorSessionChangeGroup
{
    private readonly EditorWorkspaceSession _session;
    private bool _completed;

    internal EditorSessionChangeGroup(EditorWorkspaceSession session, string? label)
    {
        _session = session;
        Label = string.IsNullOrWhiteSpace(label) ? null : label.Trim();
    }

    /// <summary>
    /// Optional host-supplied label recorded in session history when this group is applied or saved.
    /// </summary>
    public string? Label { get; }

    /// <summary>
    /// Applies the currently staged session changes as one labeled change group.
    /// </summary>
    public EditorWorkspace ApplyPendingChanges()
    {
        EnsureNotCompleted();
        _completed = true;
        return _session.ApplyPendingChanges(Label);
    }

    /// <summary>
    /// Applies and persists the currently staged session changes as one labeled change group.
    /// </summary>
    public EditorWorkspace SavePendingChanges()
    {
        EnsureNotCompleted();
        _completed = true;
        return _session.SavePendingChanges(Label);
    }

    /// <summary>
    /// Discards the currently staged session changes and completes the group without recording history.
    /// </summary>
    public EditorWorkspaceSession DiscardPendingChanges()
    {
        EnsureNotCompleted();
        _completed = true;
        return _session.DiscardPendingChanges();
    }

    private void EnsureNotCompleted()
    {
        if (_completed)
            throw new InvalidOperationException("This session change group has already been completed.");
    }
}
