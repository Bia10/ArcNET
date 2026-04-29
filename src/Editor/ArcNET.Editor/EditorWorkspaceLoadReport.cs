namespace ArcNET.Editor;

/// <summary>
/// Diagnostics collected while constructing an <see cref="EditorWorkspace"/>.
/// </summary>
public sealed class EditorWorkspaceLoadReport
{
    /// <summary>
    /// Empty load report used when no load diagnostics were collected.
    /// </summary>
    public static EditorWorkspaceLoadReport Empty { get; } =
        new() { SkippedArchiveCandidates = [], SkippedAssets = [] };

    /// <summary>
    /// Archive candidates that were discovered under the game install and skipped before loading.
    /// </summary>
    public required IReadOnlyList<EditorSkippedArchiveCandidate> SkippedArchiveCandidates { get; init; }

    /// <summary>
    /// Winning install-backed assets that were skipped because the current parsers could not load them.
    /// </summary>
    public required IReadOnlyList<EditorSkippedAsset> SkippedAssets { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when the load encountered any skipped archives or assets.
    /// </summary>
    public bool HasSkippedInputs => SkippedArchiveCandidates.Count > 0 || SkippedAssets.Count > 0;
}
