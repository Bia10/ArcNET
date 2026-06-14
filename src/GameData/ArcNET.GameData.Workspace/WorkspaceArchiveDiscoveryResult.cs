namespace ArcNET.GameData.Workspace;

public sealed class WorkspaceArchiveDiscoveryResult(
    IReadOnlyList<string> archivePaths,
    IReadOnlyList<WorkspaceSkippedArchiveCandidate> skippedArchiveCandidates
)
{
    public static WorkspaceArchiveDiscoveryResult Empty { get; } = new([], []);

    public IReadOnlyList<string> ArchivePaths { get; } = archivePaths;

    public IReadOnlyList<WorkspaceSkippedArchiveCandidate> SkippedArchiveCandidates { get; } = skippedArchiveCandidates;
}
