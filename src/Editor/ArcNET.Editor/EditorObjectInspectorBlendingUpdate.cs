using ArcNET.Core.Primitives;
using ArcNET.GameObjects;

namespace ArcNET.Editor;

/// <summary>
/// Staged blending-pane update for one object/proto inspector target.
/// Null properties preserve the current value.
/// </summary>
public sealed class EditorObjectInspectorBlendingUpdate
{
    public ObjFBlitFlags? BlitFlags { get; init; }

    public Color? BlitColor { get; init; }

    public int? BlitAlpha { get; init; }

    public int? BlitScale { get; init; }

    public int? Material { get; init; }

    public bool HasChanges =>
        BlitFlags.HasValue || BlitColor.HasValue || BlitAlpha.HasValue || BlitScale.HasValue || Material.HasValue;
}
