using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ArcNET.Patch;

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
