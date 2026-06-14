using ArcNET.Core.Primitives;

namespace ArcNET.GameData.Workspace;

/// <summary>
/// One tile-art entry projected from loaded workspace game data.
/// </summary>
public sealed record class WorkspaceTileArtCatalogEntry(ArtId ArtId, string DisplayName, string AssetPath);
