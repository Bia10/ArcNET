namespace ArcNET.Editor;

/// <summary>
/// Describes which persisted project assets a live session could reopen directly.
/// </summary>
public sealed class EditorProjectRestoreResult
{
    /// <summary>
    /// Normalized active asset path requested by the restored project, if any.
    /// </summary>
    public string? RequestedActiveAssetPath { get; init; }

    /// <summary>
    /// Normalized active asset path reopened directly by the session, if any.
    /// </summary>
    public string? RestoredActiveAssetPath { get; init; }

    /// <summary>
    /// Normalized asset paths reopened directly by the session.
    /// </summary>
    public IReadOnlyList<string> RestoredAssetPaths { get; init; } = [];

    /// <summary>
    /// Normalized project asset paths that were preserved for round-tripping but not reopened directly.
    /// </summary>
    public IReadOnlyList<string> SkippedAssetPaths { get; init; } = [];
}
