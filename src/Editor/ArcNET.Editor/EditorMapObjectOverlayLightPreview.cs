using ArcNET.Core.Primitives;

namespace ArcNET.Editor;

/// <summary>
/// One CE overlay-light attachment projected from <c>OBJ_F_OVERLAY_LIGHT_*</c> object fields.
/// </summary>
public sealed class EditorMapObjectOverlayLightPreview
{
    /// <summary>
    /// Overlay light flags copied from the corresponding CE array slot when available.
    /// </summary>
    public int Flags { get; init; }

    /// <summary>
    /// Overlay light art identifier.
    /// </summary>
    public required ArtId ArtId { get; init; }

    /// <summary>
    /// Overlay light color decoded from CE's packed <c>0xRRGGBB</c> value when present.
    /// </summary>
    public Color? Color { get; init; }
}
