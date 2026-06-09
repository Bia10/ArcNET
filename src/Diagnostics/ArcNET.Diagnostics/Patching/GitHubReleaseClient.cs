using System.Net.Http.Json;

namespace ArcNET.Patch;

/// <summary>GitHub release query helpers (replaces the old LibGit2Sharp-based GitHub class).</summary>
public static class GitHubReleaseClient
{
    private const string HighResPatchRepoApi =
        "https://api.github.com/repos/ArcNET-Modding/HighResPatch/releases/latest";

    // A single shared instance is the correct .NET pattern for HttpClient.
    // Creating one per request causes socket exhaustion (TIME_WAIT port leak) under load.
    private static readonly HttpClient s_http = CreateSharedClient();

    private static HttpClient CreateSharedClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("ArcNET/1.0");
        return client;
    }

    /// <summary>Fetches the latest release metadata for the HighRes patch.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task<GitHubRelease?> GetLatestHighResPatchReleaseAsync(
        CancellationToken cancellationToken = default
    )
    {
        return await s_http
            .GetFromJsonAsync(HighResPatchRepoApi, PatchJsonContext.Default.GitHubRelease, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Downloads a file from <paramref name="url"/> to <paramref name="destinationPath"/>.</summary>
    public static async Task DownloadFileAsync(
        string url,
        string destinationPath,
        CancellationToken cancellationToken = default
    )
    {
        using var response = await s_http.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var fs = File.Create(destinationPath);
        await response.Content.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
    }
}
