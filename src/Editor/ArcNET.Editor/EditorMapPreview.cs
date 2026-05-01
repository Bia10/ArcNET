namespace ArcNET.Editor;

/// <summary>
/// One dense, top-down text preview built from an <see cref="EditorMapProjection"/>.
/// </summary>
public sealed class EditorMapPreview
{
    /// <summary>
    /// Map name that owns the preview projection.
    /// </summary>
    public required string MapName { get; init; }

    /// <summary>
    /// Preview mode used to classify occupied sector cells.
    /// </summary>
    public required EditorMapPreviewMode Mode { get; init; }

    /// <summary>
    /// Dense preview width in sector cells.
    /// </summary>
    public required int Width { get; init; }

    /// <summary>
    /// Dense preview height in sector cells.
    /// </summary>
    public required int Height { get; init; }

    /// <summary>
    /// Human-readable legend for the preview glyphs.
    /// </summary>
    public required string Legend { get; init; }

    /// <summary>
    /// Preview rows ordered from top to bottom.
    /// </summary>
    public required IReadOnlyList<string> Rows { get; init; }
}
