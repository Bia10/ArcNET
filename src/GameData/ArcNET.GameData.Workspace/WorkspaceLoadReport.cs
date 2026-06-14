namespace ArcNET.GameData.Workspace;

/// <summary>
/// Diagnostics collected while loading one install-backed or module-backed workspace content set.
/// </summary>
public sealed class WorkspaceLoadReport(
    IReadOnlyList<WorkspaceSkippedArchiveCandidate> skippedArchiveCandidates,
    IReadOnlyList<WorkspaceSkippedAsset> skippedAssets
)
{
    public static WorkspaceLoadReport Empty { get; } = new([], []);

    public IReadOnlyList<WorkspaceSkippedArchiveCandidate> SkippedArchiveCandidates { get; } = skippedArchiveCandidates;

    public IReadOnlyList<WorkspaceSkippedAsset> SkippedAssets { get; } = skippedAssets;

    public bool HasSkippedInputs => SkippedArchiveCandidates.Count > 0 || SkippedAssets.Count > 0;
}
