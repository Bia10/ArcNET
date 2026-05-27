using ArcNET.Core.Primitives;
using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// Render-ready CE light-system mask projected into the normalized scene render space.
/// </summary>
public sealed class EditorMapLightRenderItem
{
    public required string SectorAssetPath { get; init; }
    public required int MapTileX { get; init; }
    public required int MapTileY { get; init; }
    public required Location Tile { get; init; }
    public required ArtId ArtId { get; init; }
    public required int DrawOrder { get; init; }
    public required double AnchorX { get; init; }
    public required double AnchorY { get; init; }
    public required uint SuggestedTintColor { get; init; }
    public required double SuggestedOpacity { get; init; }
    public required SectorLightFlags Flags { get; init; }
}
