using ArcNET.GameObjects;

namespace ArcNET.Editor;

/// <summary>
/// Staged container-pane update for one object/proto inspector target.
/// Null properties preserve the current value.
/// </summary>
public sealed class EditorObjectInspectorContainerUpdate
{
    public int? LockDifficulty { get; init; }

    public int? KeyId { get; init; }

    public ContainerFlags? ContainerFlags { get; init; }

    public bool HasChanges => LockDifficulty.HasValue || KeyId.HasValue || ContainerFlags.HasValue;
}
