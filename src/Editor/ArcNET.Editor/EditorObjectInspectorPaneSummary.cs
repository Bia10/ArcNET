using ArcNET.GameObjects;

namespace ArcNET.Editor;

/// <summary>
/// Host-facing readiness summary for one object/proto inspector pane.
/// </summary>
public sealed class EditorObjectInspectorPaneSummary
{
    /// <summary>
    /// Stable pane identifier.
    /// </summary>
    public required EditorObjectInspectorPane Pane { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when the pane is relevant to the current target.
    /// </summary>
    public required bool IsApplicable { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when the SDK already exposes one typed contract for this pane.
    /// </summary>
    public required bool HasContract { get; init; }

    /// <summary>
    /// Optional explanation when <see cref="IsApplicable"/> is <see langword="false"/>.
    /// </summary>
    public string? UnavailableReason { get; init; }

    internal static IReadOnlyList<EditorObjectInspectorPaneSummary> CreateList(
        EditorObjectInspectorTargetKind targetKind,
        ObjectType? objectType
    )
    {
        if (targetKind == EditorObjectInspectorTargetKind.None)
        {
            const string unavailableReason =
                "The current selection does not resolve to one inspectable object or shared proto target.";

            return
            [
                Create(EditorObjectInspectorPane.Overview, isApplicable: false, hasContract: true, unavailableReason),
                Create(EditorObjectInspectorPane.Flags, isApplicable: false, hasContract: true, unavailableReason),
                Create(
                    EditorObjectInspectorPane.ScriptAttachments,
                    isApplicable: false,
                    hasContract: true,
                    unavailableReason
                ),
                Create(EditorObjectInspectorPane.Light, isApplicable: false, hasContract: true, unavailableReason),
                Create(
                    EditorObjectInspectorPane.CritterProgression,
                    isApplicable: false,
                    hasContract: true,
                    unavailableReason
                ),
                Create(EditorObjectInspectorPane.Generator, isApplicable: false, hasContract: true, unavailableReason),
                Create(EditorObjectInspectorPane.Blending, isApplicable: false, hasContract: true, unavailableReason),
            ];
        }

        var isCritterTarget = objectType is ObjectType.Pc or ObjectType.Npc;
        var isGeneratorTarget = objectType is ObjectType.Npc;

        return
        [
            Create(EditorObjectInspectorPane.Overview, isApplicable: true, hasContract: true),
            Create(EditorObjectInspectorPane.Flags, isApplicable: true, hasContract: true),
            Create(EditorObjectInspectorPane.ScriptAttachments, isApplicable: true, hasContract: true),
            Create(EditorObjectInspectorPane.Light, isApplicable: true, hasContract: true),
            Create(
                EditorObjectInspectorPane.CritterProgression,
                isApplicable: isCritterTarget,
                hasContract: true,
                isCritterTarget ? null : "Critter progression only applies to Pc/Npc targets."
            ),
            Create(
                EditorObjectInspectorPane.Generator,
                isApplicable: isGeneratorTarget,
                hasContract: true,
                isGeneratorTarget ? null : "Generator settings only apply to Npc targets."
            ),
            Create(EditorObjectInspectorPane.Blending, isApplicable: true, hasContract: true),
        ];
    }

    private static EditorObjectInspectorPaneSummary Create(
        EditorObjectInspectorPane pane,
        bool isApplicable,
        bool hasContract,
        string? unavailableReason = null
    ) =>
        new()
        {
            Pane = pane,
            IsApplicable = isApplicable,
            HasContract = hasContract,
            UnavailableReason = unavailableReason,
        };
}
