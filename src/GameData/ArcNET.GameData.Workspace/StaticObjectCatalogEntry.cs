namespace ArcNET.GameData.Workspace;

public sealed record class StaticObjectCatalogEntry(
    string SourceKindText,
    string DisplayName,
    string ObjectType,
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
