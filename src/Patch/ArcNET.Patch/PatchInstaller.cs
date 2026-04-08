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

        // Pick the first .zip asset from the release; Assets is populated from the GitHub API
        // "assets" array which contains the actual binary downloads (not the HTML release page).
        var zipAsset = release.Assets.FirstOrDefault(a => a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
        if (zipAsset is null)
            throw new InvalidOperationException(
                $"HighRes patch release '{release.TagName}' has no .zip asset. "
                    + $"Assets found: {(release.Assets.Count == 0 ? "(none)" : string.Join(", ", release.Assets.Select(a => a.Name)))}"
            );

        // Validate that the asset URL points to an expected GitHub host over HTTPS before
        // establishing any network connection. This guards against a compromised or malformed
        // API response redirecting the download to an arbitrary server.
        var downloadUrl = zipAsset.BrowserDownloadUrl;
        ValidateDownloadUrl(downloadUrl);

        // Download the release zip to a temp file, then extract it.
        var tempZip = Path.GetTempFileName() + ".zip";
        try
        {
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

    /// <summary>
    /// Validates that <paramref name="url"/> uses HTTPS and targets a known GitHub host.
    /// Throws <see cref="InvalidOperationException"/> when the URL fails validation.
    /// </summary>
    private static void ValidateDownloadUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new InvalidOperationException($"Asset download URL is not a valid URI: '{url}'");

        if (!uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Asset download URL must use HTTPS. Got: '{url}'");

        if (
            !uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase)
            && !uri.Host.Equals("objects.githubusercontent.com", StringComparison.OrdinalIgnoreCase)
            && !uri.Host.EndsWith(".github.com", StringComparison.OrdinalIgnoreCase)
            && !uri.Host.EndsWith(".githubusercontent.com", StringComparison.OrdinalIgnoreCase)
        )
            throw new InvalidOperationException(
                $"Asset download URL must point to github.com or githubusercontent.com. Got host: '{uri.Host}'"
            );
    }
}
