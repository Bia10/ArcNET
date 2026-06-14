namespace ArcNET.GameData.Workspace;

public sealed record class PrototypePaletteEntry(
    int ProtoNumber,
    string ObjectType,
    string AssetPath,
    string? DisplayName,
    string? Description,
    string? PaletteGroup,
    string? ArtAssetPath
);
