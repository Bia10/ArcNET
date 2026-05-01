namespace ArcNET.Editor;

/// <summary>
/// Typed map-preview configuration persisted with one project map view.
/// </summary>
public sealed class EditorProjectMapPreviewState
{
    /// <summary>
    /// Indicates whether the host was using the richer scene preview instead of an outline preview.
    /// </summary>
    public bool UseScenePreview { get; init; } = true;

    /// <summary>
    /// Selected outline preview mode when the host renders a map outline.
    /// </summary>
    public EditorMapPreviewMode OutlineMode { get; init; } = EditorMapPreviewMode.Combined;

    /// <summary>
    /// Indicates whether placed objects are visible in the scene preview.
    /// </summary>
    public bool ShowObjects { get; init; } = true;

    /// <summary>
    /// Indicates whether roofs are visible in the scene preview.
    /// </summary>
    public bool ShowRoofs { get; init; } = true;

    /// <summary>
    /// Indicates whether lights are visible in the scene preview.
    /// </summary>
    public bool ShowLights { get; init; } = true;

    /// <summary>
    /// Indicates whether blocked tiles are highlighted in the scene preview.
    /// </summary>
    public bool ShowBlockedTiles { get; init; } = true;

    /// <summary>
    /// Indicates whether sector and tile scripts are highlighted in the scene preview.
    /// </summary>
    public bool ShowScripts { get; init; } = true;
}
