namespace ArcNET.Editor;

/// <summary>
/// Host-facing summary of the project/session state restored by an applied history entry.
/// This mirrors the session-managed project metadata without requiring history consumers to
/// persist or reopen a full <see cref="EditorProject"/>.
/// </summary>
public sealed class EditorSessionProjectStateSummary
{
    /// <summary>
    /// Active asset path restored by the history entry, if any.
    /// </summary>
    public string? ActiveAssetPath { get; init; }

    /// <summary>
    /// Open-document state restored by the history entry.
    /// </summary>
    public required IReadOnlyList<EditorProjectOpenAsset> OpenAssets { get; init; }

    /// <summary>
    /// Bookmark state restored by the history entry.
    /// </summary>
    public required IReadOnlyList<EditorProjectBookmark> Bookmarks { get; init; }

    /// <summary>
    /// Typed map-view state restored by the history entry.
    /// </summary>
    public required IReadOnlyList<EditorProjectMapViewState> MapViewStates { get; init; }

    /// <summary>
    /// View-state entries restored by the history entry.
    /// </summary>
    public required IReadOnlyList<EditorProjectViewState> ViewStates { get; init; }

    /// <summary>
    /// Tool-state entries restored by the history entry.
    /// </summary>
    public required IReadOnlyList<EditorProjectToolState> ToolStates { get; init; }
}
