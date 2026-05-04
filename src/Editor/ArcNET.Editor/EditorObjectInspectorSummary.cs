using ArcNET.GameObjects;

namespace ArcNET.Editor;

/// <summary>
/// Host-facing summary for one object/proto inspector target.
/// </summary>
public sealed class EditorObjectInspectorSummary
{
    /// <summary>
    /// Resolved inspector target kind.
    /// </summary>
    public required EditorObjectInspectorTargetKind TargetKind { get; init; }

    /// <summary>
    /// Optional tracked selection context that produced this summary.
    /// When absent, the summary was created directly from one proto definition lookup.
    /// </summary>
    public EditorMapObjectSelectionSummary? SelectionSummary { get; init; }

    /// <summary>
    /// Selected placed object targeted by the inspector when <see cref="TargetKind"/> is
    /// <see cref="EditorObjectInspectorTargetKind.SelectedObject"/>.
    /// </summary>
    public EditorMapObjectPreview? SelectedObject { get; init; }

    /// <summary>
    /// Proto-backed inspector target when one loaded proto definition could be resolved.
    /// </summary>
    public EditorObjectPaletteEntry? Proto { get; init; }

    /// <summary>
    /// Resolved proto number when one was available from either the selected object or proto target.
    /// </summary>
    public int? ProtoNumber { get; init; }

    /// <summary>
    /// Object type of the current target when one could be resolved.
    /// </summary>
    public ObjectType? TargetObjectType { get; init; }

    /// <summary>
    /// Typed pane summaries for the current inspector target.
    /// </summary>
    public required IReadOnlyList<EditorObjectInspectorPaneSummary> Panes { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when the summary resolved one inspectable target.
    /// </summary>
    public bool CanInspect => TargetKind != EditorObjectInspectorTargetKind.None;

    /// <summary>
    /// Returns <see langword="true"/> when the summary was built from a tracked map selection.
    /// </summary>
    public bool HasSelectionContext => SelectionSummary is not null;

    /// <summary>
    /// Returns <see langword="true"/> when the summary targets one selected placed object.
    /// </summary>
    public bool HasSelectedObject => SelectedObject is not null;

    /// <summary>
    /// Returns <see langword="true"/> when one loaded proto definition was resolved for the target.
    /// </summary>
    public bool HasProto => Proto is not null;
}
