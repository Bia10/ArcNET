namespace ArcNET.Patch;

/// <summary>Removes a HighRes patch previously installed by <see cref="PatchInstaller"/>.</summary>
public static class PatchUninstaller
{
    private const string PatchMarkerFileName = ".arcnet-patch-installed";

    /// <summary>
    /// Removes the HighRes patch marker file from <paramref name="gameDir"/>.
    /// If the marker is absent the method returns without error (idempotent).
    /// </summary>
    /// <param name="gameDir">Root directory of the Arcanum installation.</param>
    public static void Uninstall(string gameDir)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameDir);

        var markerPath = Path.Combine(gameDir, PatchMarkerFileName);
        if (File.Exists(markerPath))
            File.Delete(markerPath);
    }

    /// <summary>Returns <see langword="true"/> when the HighRes patch marker is present in <paramref name="gameDir"/>.</summary>
    public static bool IsPatchInstalled(string gameDir)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameDir);
        return File.Exists(Path.Combine(gameDir, PatchMarkerFileName));
    }
}
