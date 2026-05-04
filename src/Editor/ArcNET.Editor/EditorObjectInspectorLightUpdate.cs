using ArcNET.Core.Primitives;

namespace ArcNET.Editor;

/// <summary>
/// Staged light-pane update for one object/proto inspector target.
/// Null properties preserve the current value.
/// </summary>
public sealed class EditorObjectInspectorLightUpdate
{
    public int? LightFlags { get; init; }

    public ArtId? LightArtId { get; init; }

    public Color? LightColor { get; init; }

    public int? OverlayLightFlags { get; init; }

    public IReadOnlyList<int>? OverlayLightArtIds { get; init; }

    public int? OverlayLightColor { get; init; }

    public bool HasChanges =>
        LightFlags.HasValue
        || LightArtId.HasValue
        || LightColor.HasValue
        || OverlayLightFlags.HasValue
        || OverlayLightArtIds is not null
        || OverlayLightColor.HasValue;
}
