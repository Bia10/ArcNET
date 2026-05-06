using ArcNET.Core.Primitives;

namespace ArcNET.Editor;

internal readonly record struct EditorMapRenderSpriteCacheKey(
    ArtId ArtId,
    EditorMapRenderQueueItemKind? RenderItemKind,
    int RotationIndex,
    int FrameIndex
);
