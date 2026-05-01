using ArcNET.Core.Primitives;
using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// Preview-ready placement and appearance metadata for one sector light.
/// </summary>
public sealed class EditorMapLightPreview
{
    /// <summary>
    /// Light tile X coordinate within the sector.
    /// </summary>
    public required int TileX { get; init; }

    /// <summary>
    /// Light tile Y coordinate within the sector.
    /// </summary>
    public required int TileY { get; init; }

    /// <summary>
    /// Sub-tile X offset.
    /// </summary>
    public required int OffsetX { get; init; }

    /// <summary>
    /// Sub-tile Y offset.
    /// </summary>
    public required int OffsetY { get; init; }

    /// <summary>
    /// Light art identifier.
    /// </summary>
    public required ArtId ArtId { get; init; }

    /// <summary>
    /// Raw light behavior flags.
    /// </summary>
    public required SectorLightFlags Flags { get; init; }

    /// <summary>
    /// Palette index used by the light.
    /// </summary>
    public required int Palette { get; init; }

    /// <summary>
    /// Red channel.
    /// </summary>
    public required byte Red { get; init; }

    /// <summary>
    /// Green channel.
    /// </summary>
    public required byte Green { get; init; }

    /// <summary>
    /// Blue channel.
    /// </summary>
    public required byte Blue { get; init; }

    /// <summary>
    /// Packed tint color.
    /// </summary>
    public required uint TintColor { get; init; }
}
