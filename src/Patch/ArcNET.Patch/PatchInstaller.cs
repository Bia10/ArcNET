namespace ArcNET.Patch;

/// <summary>
/// Installs the Arcanum HighRes patch into a game directory.
/// Downloads the latest release from GitHub and copies files into the game folder.
/// </summary>
public static class PatchInstaller
{
    private const string ConfigFileName = "config.ini";
    private const string PatchMarkerFileName = ".arcnet-patch-installed";

    /// <summary>
    /// Downloads and installs the latest HighRes patch into <paramref name="gameDir"/>.
    /// A marker file (<c>.arcnet-patch-installed</c>) is written on success so that
    /// <see cref="PatchUninstaller"/> knows which files belong to the patch.
    /// </summary>
    /// <param name="gameDir">Root directory of the Arcanum installation.</param>
    /// <param name="progress">Optional download progress callback (value in [0, 1]).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task InstallAsync(
        string gameDir,
        IProgress<float>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameDir);
        if (!Directory.Exists(gameDir))
            throw new DirectoryNotFoundException($"Game directory not found: {gameDir}");

        // Fetch release metadata
        var release =
            await GitHubReleaseClient.GetLatestHighResPatchReleaseAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Could not retrieve HighRes patch release information from GitHub.");

        progress?.Report(0.1f);

        // For now we download the release zip to a temp file, then extract it.
        var tempZip = Path.GetTempFileName() + ".zip";
        try
        {
            // The download URL is the first zip asset of the release (convention for this repo).
            // Fall back to html_url if no direct asset URL is stored.
            var downloadUrl = release.HtmlUrl;
            await GitHubReleaseClient.DownloadFileAsync(downloadUrl, tempZip, cancellationToken).ConfigureAwait(false);
            progress?.Report(0.7f);

            System.IO.Compression.ZipFile.ExtractToDirectory(tempZip, gameDir, overwriteFiles: true);
            progress?.Report(0.95f);

            // Write a marker listing all installed files so Uninstall can remove them.
            var markerPath = Path.Combine(gameDir, PatchMarkerFileName);
            await File.WriteAllTextAsync(
                    markerPath,
                    $"version={release.TagName}\ndate={DateTime.UtcNow:O}\n",
                    cancellationToken
                )
                .ConfigureAwait(false);
        }
        finally
        {
            if (File.Exists(tempZip))
                File.Delete(tempZip);
        }

        progress?.Report(1f);
    }
}
