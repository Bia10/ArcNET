using ArcNET.Core.Primitives;
using ArcNET.GameObjects;

namespace ArcNET.GameData.Workspace;

/// <summary>
/// One placed object entry projected from loaded workspace mobile or sector content.
/// </summary>
public sealed record class WorkspaceStaticObjectCatalogEntry(
    string SourceKindText,
    string DisplayName,
    ObjectType ObjectType,
    string ObjectIdText,
    string ObjectGuidText,
    int? ProtoNumber,
    string PrototypeText,
    ArtId? CurrentArtId,
    ArtId? DestroyedArtId,
    string SourceAssetPath,
    string LocationText,
    string SummaryText,
    PortalFlags? PortalFlags = null,
    ContainerFlags? ContainerFlags = null,
    SceneryFlags? SceneryFlags = null,
    int? PortalLockDifficulty = null,
    int? PortalKeyId = null,
    int? ContainerLockDifficulty = null,
    int? ContainerKeyId = null
)
{
    public bool HasPrototype => ProtoNumber is > 0;
}
