namespace ArcNET.Editor;

/// <summary>
/// One staged target inside an <see cref="EditorSessionPendingChangeSummary"/> with optional dependency context.
/// </summary>
public sealed class EditorSessionPendingChangeTargetSummary
{
    /// <summary>
    /// High-level change kind for this staged target.
    /// </summary>
    public required EditorSessionChangeKind Kind { get; init; }

    /// <summary>
    /// Target identifier for this staged change, usually an asset path or save slot name.
    /// </summary>
    public required string Target { get; init; }

    /// <summary>
    /// Dependency context for staged game-data asset targets when the workspace index can resolve one.
    /// </summary>
    public EditorAssetDependencySummary? DependencySummary { get; init; }

    /// <summary>
    /// Repair candidates currently available for this staged target.
    /// </summary>
    public required IReadOnlyList<EditorSessionValidationRepairCandidate> RepairCandidates { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when <see cref="DependencySummary"/> was resolved for this staged target.
    /// </summary>
    public bool HasDependencySummary => DependencySummary is not null;

    /// <summary>
    /// Returns <see langword="true"/> when this staged target currently has one or more repair candidates.
    /// </summary>
    public bool CanRepairFromSession => RepairCandidates.Count > 0;

    /// <summary>
    /// Number of currently available repair candidates for this staged target.
    /// </summary>
    public int RepairCandidateCount => RepairCandidates.Count;
}
