namespace ArcNET.Patch;

/// <summary>
/// Identifies which Arcanum-compatible runtime executable ArcNET should launch.
/// </summary>
public enum ArcanumExecutableKind : byte
{
    /// <summary>
    /// Resolve the executable from the supplied path and requested launch overrides.
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Original Windows executable (<c>Arcanum.exe</c>).
    /// </summary>
    Classic = 1,

    /// <summary>
    /// Community Edition executable (<c>arcanum-ce.exe</c> / <c>arcanum-ce</c>).
    /// </summary>
    CommunityEdition = 2,
}
