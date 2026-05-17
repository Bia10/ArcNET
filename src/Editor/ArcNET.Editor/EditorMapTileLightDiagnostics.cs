namespace ArcNET.Editor;

/// <summary>
/// CE-style 3x3 tile light sample grid from <c>light.c::sub_4DA360</c>.
/// These samples drive the four <c>39x20</c> tile light lerp subrects in CE.
/// </summary>
public readonly record struct EditorMapTileLightDiagnostics(
    uint? TopLeft,
    uint? TopCenter,
    uint? TopRight,
    uint? MiddleLeft,
    uint? MiddleCenter,
    uint? MiddleRight,
    uint? BottomLeft,
    uint? BottomCenter,
    uint? BottomRight
)
{
    public bool HasAnySample =>
        TopLeft.HasValue
        || TopCenter.HasValue
        || TopRight.HasValue
        || MiddleLeft.HasValue
        || MiddleCenter.HasValue
        || MiddleRight.HasValue
        || BottomLeft.HasValue
        || BottomCenter.HasValue
        || BottomRight.HasValue;

    public bool HasInterpolationVariance
    {
        get
        {
            if (!HasAnySample)
                return false;

            var reference =
                TopLeft
                ?? TopCenter
                ?? TopRight
                ?? MiddleLeft
                ?? MiddleCenter
                ?? MiddleRight
                ?? BottomLeft
                ?? BottomCenter
                ?? BottomRight;

            return TopLeft != reference
                || TopCenter != reference
                || TopRight != reference
                || MiddleLeft != reference
                || MiddleCenter != reference
                || MiddleRight != reference
                || BottomLeft != reference
                || BottomCenter != reference
                || BottomRight != reference;
        }
    }
}
