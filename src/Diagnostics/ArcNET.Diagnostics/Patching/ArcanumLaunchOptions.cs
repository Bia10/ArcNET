namespace ArcNET.Patch;

/// <summary>
/// Launch-time overrides that ArcNET can apply without modifying arcanum-ce.
/// </summary>
public sealed class ArcanumLaunchOptions
{
    /// <summary>
    /// Requested runtime executable kind. <see cref="ArcanumExecutableKind.Auto"/> resolves the best match from the game path.
    /// </summary>
    public ArcanumExecutableKind ExecutableKind { get; init; }

    /// <summary>
    /// Preferred SDL renderer backend for Community Edition launches.
    /// <see cref="SdlRenderDriver.Auto"/> leaves the game's default intact.
    /// </summary>
    public SdlRenderDriver RenderDriver { get; init; }

    /// <summary>
    /// Adds the Community Edition <c>-window</c> launch flag.
    /// </summary>
    public bool Windowed { get; init; }

    /// <summary>
    /// Optional Community Edition launch-time width override. Must be paired with <see cref="Height"/>.
    /// </summary>
    public int? Width { get; init; }

    /// <summary>
    /// Optional Community Edition launch-time height override. Must be paired with <see cref="Width"/>.
    /// </summary>
    public int? Height { get; init; }

    /// <summary>
    /// Additional launch arguments appended after ArcNET-managed overrides.
    /// </summary>
    public IReadOnlyList<string> AdditionalArguments { get; init; } = [];
}
