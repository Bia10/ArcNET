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
