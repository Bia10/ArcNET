using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ArcNET.Patch;

/// <summary>A single downloadable asset attached to a GitHub release.</summary>
public sealed class GitHubReleaseAsset
{
    /// <summary>Gets the asset file name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the direct download URL for the asset binary.</summary>
    [JsonPropertyName("browser_download_url")]
    public string BrowserDownloadUrl { get; init; } = string.Empty;

    /// <summary>Gets the MIME content type reported by GitHub.</summary>
    [JsonPropertyName("content_type")]
    public string ContentType { get; init; } = string.Empty;
}

/// <summary>A GitHub release entry from the releases API.</summary>
public sealed class GitHubRelease
{
    /// <summary>Gets the release tag name.</summary>
    [JsonPropertyName("tag_name")]
    public string TagName { get; init; } = string.Empty;

    /// <summary>Gets the HTML URL of the release.</summary>
    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; init; } = string.Empty;

    /// <summary>Gets the release name / title.</summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the release body text.</summary>
    [JsonPropertyName("body")]
    public string Body { get; init; } = string.Empty;

    /// <summary>Gets the list of downloadable assets attached to this release.</summary>
    [JsonPropertyName("assets")]
    public IReadOnlyList<GitHubReleaseAsset> Assets { get; init; } = [];
}

/// <summary>Source-generated JSON serializer context for the Patch assembly.</summary>
[JsonSerializable(typeof(GitHubRelease))]
[JsonSerializable(typeof(GitHubReleaseAsset))]
[JsonSerializable(typeof(IReadOnlyList<GitHubReleaseAsset>))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = false)]
internal sealed partial class PatchJsonContext : JsonSerializerContext { }

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
