using ArcNET.GameData.Workspace;

namespace ArcNET.Editor;

internal static class EditorAssetSourceKindAdapter
{
    public static EditorAssetSourceKind FromWorkspaceSourceKind(WorkspaceAssetSourceKind sourceKind) =>
        sourceKind switch
        {
            WorkspaceAssetSourceKind.LooseFile => EditorAssetSourceKind.LooseFile,
            WorkspaceAssetSourceKind.DatArchive => EditorAssetSourceKind.DatArchive,
            _ => throw new ArgumentOutOfRangeException(nameof(sourceKind), sourceKind, null),
        };
}
