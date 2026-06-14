namespace ArcNET.GameData.Workspace;

/// <summary>
/// Physical source metadata for one winning workspace asset.
/// </summary>
public sealed class WorkspaceAssetSource
{
    public required WorkspaceAssetSourceKind SourceKind { get; init; }

    public required string SourcePath { get; init; }

    public string? SourceEntryPath { get; init; }
}
