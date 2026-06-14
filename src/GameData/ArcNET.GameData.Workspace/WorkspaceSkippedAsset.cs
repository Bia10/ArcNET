using ArcNET.Formats;

namespace ArcNET.GameData.Workspace;

/// <summary>
/// One winning workspace asset that was discovered but skipped because it could not be parsed.
/// </summary>
public sealed class WorkspaceSkippedAsset
{
    public required string AssetPath { get; init; }

    public required FileFormat Format { get; init; }

    public required WorkspaceAssetSourceKind SourceKind { get; init; }

    public required string SourcePath { get; init; }

    public string? SourceEntryPath { get; init; }

    public required string Reason { get; init; }
}
