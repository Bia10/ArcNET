using System.Text.Json.Serialization;

namespace ArcNET.Patch;

/// <summary>Source-generated JSON serializer context for the Patch assembly.</summary>
[JsonSerializable(typeof(GitHubRelease))]
[JsonSerializable(typeof(GitHubReleaseAsset))]
[JsonSerializable(typeof(IReadOnlyList<GitHubReleaseAsset>))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = false)]
internal sealed partial class PatchJsonContext : JsonSerializerContext { }
