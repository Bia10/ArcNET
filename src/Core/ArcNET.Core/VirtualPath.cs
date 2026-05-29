namespace ArcNET.Core;

/// <summary>
/// Centralized cross-platform path normalization utilities for virtual asset paths.
/// </summary>
public static class VirtualPath
{
    /// <summary>
    /// Normalizes a relative or virtual asset path into a standard forward-slash representation.
    /// Safely handles Windows backslashes on all platforms.
    /// </summary>
    public static string Normalize(string path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;

        return path.Replace('\\', '/')
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/');
    }
}
