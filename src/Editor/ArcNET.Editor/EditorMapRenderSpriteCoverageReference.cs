using ArcNET.Core.Primitives;

namespace ArcNET.Editor;

/// <summary>
/// One distinct sprite-frame request referenced by a paintable map scene.
/// </summary>
public sealed class EditorMapRenderSpriteCoverageReference
{
    public required ArtId ArtId { get; init; }

    public required EditorMapRenderQueueItemKind RenderItemKind { get; init; }

    public required int RotationIndex { get; init; }

    public required int ScalePercent { get; init; }

    public required bool IsShrunk { get; init; }

    public EditorMapRenderSpriteRequest CreateRequest() =>
        new()
        {
            RenderItemKind = RenderItemKind,
            RotationIndex = RotationIndex,
            ScalePercent = ScalePercent,
            IsShrunk = IsShrunk,
        };
}
