namespace ArcNET.Editor;

/// <summary>
/// One archive candidate discovered under a game installation that was skipped before loading.
/// </summary>
public sealed class EditorSkippedArchiveCandidate
{
    /// <summary>
    /// Archive candidate path on disk.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// User-facing reason the archive candidate was skipped.
    /// </summary>
    public required string Reason { get; init; }
}
