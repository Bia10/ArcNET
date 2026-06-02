using ArcNET.Core.Primitives;

namespace ArcNET.Editor;

internal readonly record struct EditorMapRenderSpriteAssetPathCacheKey(
    ArtId ArtId,
    EditorMapRenderQueueItemKind? RenderItemKind
);
