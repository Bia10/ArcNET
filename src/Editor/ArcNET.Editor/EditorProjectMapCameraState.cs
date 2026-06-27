namespace ArcNET.Editor;

/// <summary>
/// Typed map-camera state persisted with one project map view.
/// </summary>
public sealed class EditorProjectMapCameraState
{
    /// <summary>
    /// Horizontal map-camera center in tile coordinates.
    /// </summary>
    public double CenterTileX { get; init; }

    /// <summary>
    /// Vertical map-camera center in tile coordinates.
    /// </summary>
    public double CenterTileY { get; init; }

    /// <summary>
    /// Host-neutral zoom factor applied to the map camera.
    /// </summary>
    public double Zoom { get; init; } = 1d;

    /// <summary>
    /// Clockwise camera roll, in degrees, around the viewport center.
    /// </summary>
    public double RollDegrees { get; init; }

    /// <summary>
    /// Camera pitch, in degrees, applied as vertical render-space tilt.
    /// </summary>
    public double PitchDegrees { get; init; }

    /// <summary>
    /// Camera yaw, in degrees, applied as horizontal render-space tilt.
    /// </summary>
    public double YawDegrees { get; init; }
}
