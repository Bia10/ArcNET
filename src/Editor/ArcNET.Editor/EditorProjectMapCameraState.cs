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
}
