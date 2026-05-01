namespace ArcNET.Editor;

/// <summary>
/// One grouped bucket inside an <see cref="EditorSessionPendingChangeSummary"/>.
/// </summary>
public sealed class EditorSessionChangeKindSummary
{
    /// <summary>
    /// High-level change kind represented by this grouped bucket.
    /// </summary>
    public required EditorSessionChangeKind Kind { get; init; }

    /// <summary>
    /// Changed targets grouped under <see cref="Kind"/> in stable display order.
    /// </summary>
    public required IReadOnlyList<string> Targets { get; init; }

    /// <summary>
    /// Number of changed targets represented by this grouped bucket.
    /// </summary>
    public int Count => Targets.Count;
}
