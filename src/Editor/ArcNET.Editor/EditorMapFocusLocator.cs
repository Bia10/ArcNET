using System.Globalization;
using ArcNET.Core.Primitives;
using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.Editor;

/// <summary>
/// Resolves host-facing map focus targets from asset paths, proto identifiers, or placed object ids.
/// </summary>
public static class EditorMapFocusLocator
{
    private const int SectorTileAxisLength = 64;

    /// <summary>
    /// Resolves the first stable focusable map target for the supplied query.
    /// Supported inputs include <c>.mob</c>, <c>.sec</c>, <c>.pro</c>/<c>.proto</c> asset paths, proto numbers,
    /// and plain GUID object identifiers.
    /// </summary>
    public static EditorMapFocusTarget? FindTarget(EditorWorkspace workspace, string query)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        var trimmedQuery = query.Trim();
        var normalizedAssetQuery = NormalizeAssetLikeQuery(trimmedQuery);

        if (Guid.TryParse(trimmedQuery, out var objectGuid))
            return ChooseTarget(FindTargetsByObjectGuid(workspace, trimmedQuery, objectGuid));

        var asset = ResolveAsset(workspace, normalizedAssetQuery);
        if (asset is not null)
        {
            return asset.Format switch
            {
                FileFormat.Mob => ChooseTarget(
                    FindTargetsForMobAsset(workspace, trimmedQuery, asset.AssetPath, asset.AssetPath)
                ),
                FileFormat.Proto => ChooseTarget(FindTargetsForProtoAsset(workspace, trimmedQuery, asset.AssetPath)),
                FileFormat.Sector => FindTargetForSectorAsset(workspace, trimmedQuery, asset.AssetPath),
                _ => null,
            };
        }

        if (
            int.TryParse(trimmedQuery, NumberStyles.Integer, CultureInfo.InvariantCulture, out var protoNumber)
            && protoNumber > 0
        )
        {
            return ChooseTarget(
                FindTargetsForProtoNumber(
                    workspace,
                    trimmedQuery,
                    protoNumber,
                    workspace.Index.FindProtoDefinition(protoNumber)?.AssetPath
                )
            );
        }

        return null;
    }

    private static EditorMapFocusTarget? FindTargetForSectorAsset(
        EditorWorkspace workspace,
        string query,
        string sectorAssetPath
    )
    {
        if (!TryResolveSectorProjection(workspace, sectorAssetPath, out var mapName, out var sectorProjection))
            return null;

        const short centerTile = SectorTileAxisLength / 2;
        return new EditorMapFocusTarget
        {
            Query = query,
            MapName = mapName,
            SectorAssetPath = sectorProjection.Asset.AssetPath,
            Tile = new Location(centerTile, centerTile),
            CenterTileX = (sectorProjection.LocalX * SectorTileAxisLength) + centerTile,
            CenterTileY = (sectorProjection.LocalY * SectorTileAxisLength) + centerTile,
            FocusAssetPath = sectorProjection.Asset.AssetPath,
            SourceAssetPath = sectorProjection.Asset.AssetPath,
            MatchCount = 1,
        };
    }

    private static IReadOnlyList<EditorMapFocusTarget> FindTargetsForProtoAsset(
        EditorWorkspace workspace,
        string query,
        string protoAssetPath
    )
    {
        var protoNumber =
            workspace.Index.FindAssetDependencySummary(protoAssetPath)?.DefinedProtoNumber
            ?? workspace.FindProto(protoAssetPath)?.Header.ObjectId.GetProtoNumber();
        if (protoNumber is null or <= 0)
            return [];

        return FindTargetsForProtoNumber(workspace, query, protoNumber.Value, protoAssetPath);
    }

    private static IReadOnlyList<EditorMapFocusTarget> FindTargetsForProtoNumber(
        EditorWorkspace workspace,
        string query,
        int protoNumber,
        string? focusAssetPath
    )
    {
        var candidates = new List<MapFocusCandidate>();
        foreach (
            var reference in workspace
                .Index.FindProtoReferences(protoNumber)
                .OrderBy(static reference => reference.Asset.AssetPath, StringComparer.OrdinalIgnoreCase)
        )
        {
            switch (reference.Format)
            {
                case FileFormat.Mob:
                    if (!workspace.GameData.MobsBySource.TryGetValue(reference.Asset.AssetPath, out var mobs))
                        continue;

                    for (var mobIndex = 0; mobIndex < mobs.Count; mobIndex++)
                    {
                        var mob = mobs[mobIndex];
                        if (mob.Header.ProtoId.GetProtoNumber() != protoNumber)
                            continue;

                        if (
                            TryBuildMapMobCandidate(
                                workspace,
                                reference.Asset.AssetPath,
                                mob,
                                focusAssetPath ?? reference.Asset.AssetPath,
                                out var candidate
                            )
                        )
                        {
                            candidates.Add(candidate with { ProtoNumber = protoNumber });
                        }
                    }

                    break;

                case FileFormat.Sector:
                    if (
                        !TryResolveSectorProjection(
                            workspace,
                            reference.Asset.AssetPath,
                            out var mapName,
                            out var sectorProjection
                        ) || !workspace.GameData.SectorsBySource.TryGetValue(reference.Asset.AssetPath, out var sectors)
                    )
                    {
                        continue;
                    }

                    for (var sectorIndex = 0; sectorIndex < sectors.Count; sectorIndex++)
                    {
                        var sector = sectors[sectorIndex];
                        for (var objectIndex = 0; objectIndex < sector.Objects.Count; objectIndex++)
                        {
                            var mob = sector.Objects[objectIndex];
                            if (mob.Header.ProtoId.GetProtoNumber() != protoNumber)
                                continue;

                            if (
                                TryBuildSectorObjectCandidate(
                                    mapName,
                                    sectorProjection,
                                    reference.Asset.AssetPath,
                                    mob,
                                    focusAssetPath ?? reference.Asset.AssetPath,
                                    out var candidate
                                )
                            )
                            {
                                candidates.Add(candidate with { ProtoNumber = protoNumber });
                            }
                        }
                    }

                    break;
            }
        }

        return CreateTargets(query, candidates);
    }

    private static IReadOnlyList<EditorMapFocusTarget> FindTargetsForMobAsset(
        EditorWorkspace workspace,
        string query,
        string mobAssetPath,
        string focusAssetPath
    )
    {
        if (!workspace.GameData.MobsBySource.TryGetValue(mobAssetPath, out var mobs) || mobs.Count == 0)
            return [];

        var candidates = new List<MapFocusCandidate>(mobs.Count);
        for (var index = 0; index < mobs.Count; index++)
        {
            if (TryBuildMapMobCandidate(workspace, mobAssetPath, mobs[index], focusAssetPath, out var candidate))
                candidates.Add(candidate);
        }

        return CreateTargets(query, candidates);
    }

    private static IReadOnlyList<EditorMapFocusTarget> FindTargetsByObjectGuid(
        EditorWorkspace workspace,
        string query,
        Guid objectGuid
    )
    {
        var candidates = new List<MapFocusCandidate>();

        foreach (
            var (assetPath, mobs) in workspace.GameData.MobsBySource.OrderBy(
                static pair => pair.Key,
                StringComparer.OrdinalIgnoreCase
            )
        )
        {
            for (var mobIndex = 0; mobIndex < mobs.Count; mobIndex++)
            {
                var mob = mobs[mobIndex];
                if (mob.Header.ObjectId.OidType != GameObjectGuid.OidTypeGuid || mob.Header.ObjectId.Id != objectGuid)
                    continue;

                if (TryBuildMapMobCandidate(workspace, assetPath, mob, assetPath, out var candidate))
                    candidates.Add(candidate);
            }
        }

        foreach (
            var (assetPath, sectors) in workspace.GameData.SectorsBySource.OrderBy(
                static pair => pair.Key,
                StringComparer.OrdinalIgnoreCase
            )
        )
        {
            if (!TryResolveSectorProjection(workspace, assetPath, out var mapName, out var sectorProjection))
                continue;

            for (var sectorIndex = 0; sectorIndex < sectors.Count; sectorIndex++)
            {
                var sector = sectors[sectorIndex];
                for (var objectIndex = 0; objectIndex < sector.Objects.Count; objectIndex++)
                {
                    var mob = sector.Objects[objectIndex];
                    if (
                        mob.Header.ObjectId.OidType != GameObjectGuid.OidTypeGuid
                        || mob.Header.ObjectId.Id != objectGuid
                    )
                        continue;

                    if (
                        TryBuildSectorObjectCandidate(
                            mapName,
                            sectorProjection,
                            assetPath,
                            mob,
                            assetPath,
                            out var candidate
                        )
                    )
                        candidates.Add(candidate);
                }
            }
        }

        return CreateTargets(query, candidates);
    }

    private static IReadOnlyList<EditorMapFocusTarget> CreateTargets(
        string query,
        IReadOnlyList<MapFocusCandidate> candidates
    )
    {
        if (candidates.Count == 0)
            return [];

        var orderedCandidates = candidates
            .OrderBy(static candidate => candidate.MapName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static candidate => candidate.SourceAssetPath ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static candidate => candidate.CenterTileY)
            .ThenBy(static candidate => candidate.CenterTileX)
            .ToArray();
        var matchCount = orderedCandidates.Length;

        return
        [
            .. orderedCandidates.Select(candidate => new EditorMapFocusTarget
            {
                Query = query,
                MapName = candidate.MapName,
                SectorAssetPath = candidate.SectorAssetPath,
                Tile = candidate.Tile,
                CenterTileX = candidate.CenterTileX,
                CenterTileY = candidate.CenterTileY,
                ObjectId = candidate.ObjectId,
                ProtoNumber = candidate.ProtoNumber,
                FocusAssetPath = candidate.FocusAssetPath,
                SourceAssetPath = candidate.SourceAssetPath,
                MatchCount = matchCount,
            }),
        ];
    }

    private static EditorMapFocusTarget? ChooseTarget(IReadOnlyList<EditorMapFocusTarget> targets) =>
        targets.Count == 0 ? null : targets[0];

    private static bool TryBuildMapMobCandidate(
        EditorWorkspace workspace,
        string mobAssetPath,
        MobData mob,
        string focusAssetPath,
        out MapFocusCandidate candidate
    )
    {
        candidate = default;

        var mapName = workspace.Index.FindAssetMap(mobAssetPath);
        if (string.IsNullOrWhiteSpace(mapName))
            return false;

        var projection = workspace.Index.FindMapProjection(mapName);
        if (projection is null || !TryProjectMapMobLocation(projection, mob, out var sectorProjection, out var tile))
            return false;

        candidate = new MapFocusCandidate(
            mapName,
            sectorProjection.Asset.AssetPath,
            tile,
            (sectorProjection.LocalX * SectorTileAxisLength) + tile.X,
            (sectorProjection.LocalY * SectorTileAxisLength) + tile.Y,
            mob.Header.ObjectId,
            mob.Header.ProtoId.GetProtoNumber() > 0 ? mob.Header.ProtoId.GetProtoNumber() : null,
            focusAssetPath,
            mobAssetPath
        );
        return true;
    }

    private static bool TryBuildSectorObjectCandidate(
        string mapName,
        EditorMapSectorProjection sectorProjection,
        string sourceAssetPath,
        MobData mob,
        string focusAssetPath,
        out MapFocusCandidate candidate
    )
    {
        candidate = default;
        if (!TryGetLocation(mob, out var tile))
            return false;

        candidate = new MapFocusCandidate(
            mapName,
            sectorProjection.Asset.AssetPath,
            tile,
            (sectorProjection.LocalX * SectorTileAxisLength) + tile.X,
            (sectorProjection.LocalY * SectorTileAxisLength) + tile.Y,
            mob.Header.ObjectId,
            mob.Header.ProtoId.GetProtoNumber() > 0 ? mob.Header.ProtoId.GetProtoNumber() : null,
            focusAssetPath,
            sourceAssetPath
        );
        return true;
    }

    private static bool TryResolveSectorProjection(
        EditorWorkspace workspace,
        string sectorAssetPath,
        out string mapName,
        out EditorMapSectorProjection sectorProjection
    )
    {
        mapName = string.Empty;
        sectorProjection = null!;

        var resolvedMapName = workspace.Index.FindAssetMap(sectorAssetPath);
        if (string.IsNullOrWhiteSpace(resolvedMapName))
            return false;

        var projection = workspace.Index.FindMapProjection(resolvedMapName);
        if (projection is null)
            return false;

        var resolvedSectorProjection = projection.Sectors.FirstOrDefault(candidate =>
            string.Equals(candidate.Asset.AssetPath, sectorAssetPath, StringComparison.OrdinalIgnoreCase)
        );
        if (resolvedSectorProjection is null)
            return false;

        mapName = resolvedMapName;
        sectorProjection = resolvedSectorProjection;
        return true;
    }

    private static bool TryProjectMapMobLocation(
        EditorMapProjection projection,
        MobData mob,
        out EditorMapSectorProjection sectorProjection,
        out Location tile
    )
    {
        sectorProjection = null!;
        tile = default;

        if (!TryGetLocation(mob, out var absoluteLocation))
            return false;

        var sectorX = FloorDivide(absoluteLocation.X, SectorTileAxisLength);
        var sectorY = FloorDivide(absoluteLocation.Y, SectorTileAxisLength);
        var resolvedSectorProjection = projection.Sectors.FirstOrDefault(candidate =>
            candidate.SectorX == sectorX && candidate.SectorY == sectorY
        );
        if (resolvedSectorProjection is null)
            return false;

        sectorProjection = resolvedSectorProjection;
        tile = new Location(
            checked((short)PositiveModulo(absoluteLocation.X, SectorTileAxisLength)),
            checked((short)PositiveModulo(absoluteLocation.Y, SectorTileAxisLength))
        );
        return true;
    }

    private static bool TryGetLocation(MobData mob, out Location location)
    {
        location = default;
        var property = mob.GetProperty(ObjectField.ObjFLocation);
        if (property is null || property.ParseNote is not null)
            return false;

        try
        {
            var (tileX, tileY) = property.GetLocation();
            location = new Location(checked((short)tileX), checked((short)tileY));
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static EditorAssetEntry? ResolveAsset(EditorWorkspace workspace, string query)
    {
        var preferredFormat = GetPreferredAssetFormat(query);
        var candidateAssets = preferredFormat is { } format
            ? workspace.Assets.FindByFormat(format)
            : workspace.Assets.Entries;

        if (
            workspace.Assets.Find(query) is { } exactAsset
            && (preferredFormat is null || exactAsset.Format == preferredFormat)
        )
            return exactAsset;

        var fileName = Path.GetFileName(query);
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            var fileNameMatches = candidateAssets
                .Where(asset =>
                    string.Equals(Path.GetFileName(asset.AssetPath), fileName, StringComparison.OrdinalIgnoreCase)
                )
                .ToArray();
            if (fileNameMatches.Length == 1)
                return fileNameMatches[0];
        }

        var pathMatches = candidateAssets
            .Where(asset => asset.AssetPath.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return pathMatches.Length == 1 ? pathMatches[0] : null;
    }

    private static FileFormat? GetPreferredAssetFormat(string query)
    {
        var extension = Path.GetExtension(query);
        if (string.IsNullOrWhiteSpace(extension))
            return null;

        return extension.ToLowerInvariant() switch
        {
            ".mob" => FileFormat.Mob,
            ".sec" => FileFormat.Sector,
            ".pro" or ".proto" => FileFormat.Proto,
            _ => null,
        };
    }

    private static string NormalizeAssetLikeQuery(string query)
    {
        var normalized = query.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
        return normalized.EndsWith(".proto", StringComparison.OrdinalIgnoreCase) ? normalized[..^2] : normalized;
    }

    private static int FloorDivide(int value, int divisor)
    {
        var quotient = value / divisor;
        var remainder = value % divisor;
        return remainder < 0 ? quotient - 1 : quotient;
    }

    private static int PositiveModulo(int value, int divisor)
    {
        var remainder = value % divisor;
        return remainder < 0 ? remainder + divisor : remainder;
    }

    private readonly record struct MapFocusCandidate(
        string MapName,
        string SectorAssetPath,
        Location Tile,
        double CenterTileX,
        double CenterTileY,
        GameObjectGuid? ObjectId,
        int? ProtoNumber,
        string? FocusAssetPath,
        string? SourceAssetPath
    );
}
