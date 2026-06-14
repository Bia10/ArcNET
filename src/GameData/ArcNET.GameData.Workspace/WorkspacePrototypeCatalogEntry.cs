using ArcNET.Core.Primitives;
using ArcNET.GameObjects;

namespace ArcNET.GameData.Workspace;

/// <summary>
/// One prototype entry projected from loaded workspace game data.
/// </summary>
public sealed record class WorkspacePrototypeCatalogEntry(
    int ProtoNumber,
    ObjectType ObjectType,
    string AssetPath,
    string? DisplayName,
    string? Description,
    string? PaletteGroup,
    ArtId? CurrentArtId,
    string? ArtAssetPath
);
