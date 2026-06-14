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
    string SourceAssetPath,
    string LocationText,
    string SummaryText
)
{
    public bool HasPrototype => ProtoNumber is > 0;
}
