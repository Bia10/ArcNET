using System.Globalization;
using ArcNET.Core.Primitives;
using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.GameData.Workspace;

/// <summary>
/// Projects placed-object browser entries directly from loaded workspace game data.
/// </summary>
public static class WorkspaceStaticObjectCatalogBuilder
{
    public static IReadOnlyList<WorkspaceStaticObjectCatalogEntry> Build(
        GameDataStore gameData,
        IReadOnlyList<WorkspacePrototypeCatalogEntry> prototypeEntries
    )
    {
        ArgumentNullException.ThrowIfNull(gameData);
        ArgumentNullException.ThrowIfNull(prototypeEntries);

        return Build(gameData, prototypeEntries.ToDictionary(static entry => entry.ProtoNumber));
    }

    public static IReadOnlyList<WorkspaceStaticObjectCatalogEntry> Build(
        GameDataStore gameData,
        IReadOnlyDictionary<int, WorkspacePrototypeCatalogEntry> prototypesByNumber
    )
    {
        ArgumentNullException.ThrowIfNull(gameData);
        ArgumentNullException.ThrowIfNull(prototypesByNumber);

        List<WorkspaceStaticObjectCatalogEntry> entries = [];

        foreach (var (assetPath, mobs) in gameData.MobsBySource)
        {
            for (var mobIndex = 0; mobIndex < mobs.Count; mobIndex++)
                entries.Add(CreateEntry("Mob asset", assetPath, mobs[mobIndex], prototypesByNumber));
        }

        foreach (var (assetPath, sectors) in gameData.SectorsBySource)
        {
            for (var sectorIndex = 0; sectorIndex < sectors.Count; sectorIndex++)
            {
                var sector = sectors[sectorIndex];
                for (var mobIndex = 0; mobIndex < sector.Objects.Count; mobIndex++)
                    entries.Add(CreateEntry("Sector object", assetPath, sector.Objects[mobIndex], prototypesByNumber));
            }
        }

        return
        [
            .. entries
                .OrderBy(static entry => entry.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static entry => entry.SourceAssetPath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static entry => entry.ProtoNumber ?? int.MaxValue)
                .ThenBy(static entry => entry.ObjectGuidText, StringComparer.OrdinalIgnoreCase),
        ];
    }

    private static WorkspaceStaticObjectCatalogEntry CreateEntry(
        string sourceKindText,
        string sourceAssetPath,
        MobData mob,
        IReadOnlyDictionary<int, WorkspacePrototypeCatalogEntry> prototypesByNumber
    )
    {
        var header = mob.Header;
        var protoNumber = header.ProtoId.GetProtoNumber();
        var prototypeEntry =
            protoNumber.HasValue && prototypesByNumber.TryGetValue(protoNumber.Value, out var resolvedPrototypeEntry)
                ? resolvedPrototypeEntry
                : null;
        var prototypeText =
            prototypeEntry is not null ? FormatPrototypeText(prototypeEntry)
            : protoNumber.HasValue ? $"proto {protoNumber.Value.ToString(CultureInfo.InvariantCulture)}"
            : "Prototype unavailable";
        var displayName = prototypeEntry?.DisplayName ?? prototypeEntry?.AssetPath ?? header.GameObjectType.ToString();
        var objectGuidText =
            header.ObjectId.OidType == GameObjectGuid.OidTypeGuid ? header.ObjectId.Id.ToString() : string.Empty;

        return new WorkspaceStaticObjectCatalogEntry(
            sourceKindText,
            displayName,
            header.GameObjectType,
            header.ObjectId.ToString(),
            objectGuidText,
            protoNumber,
            prototypeText,
            ResolvePlacedOrPrototypeArtId(mob, ObjectField.CurrentAid, prototypeEntry?.CurrentArtId),
            ResolvePlacedOrPrototypeArtId(mob, ObjectField.DestroyedAid, prototypeEntry?.DestroyedArtId),
            sourceAssetPath,
            TryFormatObjectLocation(mob),
            $"{sourceKindText} - {sourceAssetPath}",
            ResolvePlacedOrPrototypePortalFlags(mob, prototypeEntry),
            ResolvePlacedOrPrototypeContainerFlags(mob, prototypeEntry),
            ResolvePlacedOrPrototypeSceneryFlags(mob, prototypeEntry),
            ResolvePlacedOrPrototypePortalInt32(
                mob,
                ObjectField.PortalLockDifficulty,
                prototypeEntry?.PortalLockDifficulty
            ),
            ResolvePlacedOrPrototypePortalInt32(mob, ObjectField.PortalKeyId, prototypeEntry?.PortalKeyId),
            ResolvePlacedOrPrototypeContainerInt32(
                mob,
                ObjectField.ContainerLockDifficulty,
                prototypeEntry?.ContainerLockDifficulty
            ),
            ResolvePlacedOrPrototypeContainerInt32(mob, ObjectField.ContainerKeyId, prototypeEntry?.ContainerKeyId)
        );
    }

    private static string FormatPrototypeText(WorkspacePrototypeCatalogEntry prototypeEntry) =>
        $"{prototypeEntry.DisplayName ?? prototypeEntry.AssetPath} [{prototypeEntry.ProtoNumber.ToString(CultureInfo.InvariantCulture)}]";

    private static string TryFormatObjectLocation(MobData mob)
    {
        var location = mob.GetProperty(ObjectField.Location)?.GetLocation();
        return location is { } tile
            ? $"Tile ({tile.X.ToString(CultureInfo.InvariantCulture)}, {tile.Y.ToString(CultureInfo.InvariantCulture)})"
            : "Tile unavailable";
    }

    private static ArtId? ResolvePlacedOrPrototypeArtId(MobData mob, ObjectField field, ArtId? prototypeArtId)
    {
        var property = mob.GetProperty(field);
        if (property is not null)
        {
            var artId = new ArtId(unchecked((uint)property.GetInt32()));
            if (IsValidArtId(artId))
                return artId;
        }

        return IsValidArtId(prototypeArtId) ? prototypeArtId : null;
    }

    private static PortalFlags? ResolvePlacedOrPrototypePortalFlags(
        MobData mob,
        WorkspacePrototypeCatalogEntry? prototypeEntry
    )
    {
        if (mob.Header.GameObjectType is not ObjectType.Portal)
            return null;

        var value = TryGetInt32Property(mob, ObjectField.PortalFlags);
        return value.HasValue ? unchecked((PortalFlags)(uint)value.Value) : prototypeEntry?.PortalFlags;
    }

    private static ContainerFlags? ResolvePlacedOrPrototypeContainerFlags(
        MobData mob,
        WorkspacePrototypeCatalogEntry? prototypeEntry
    )
    {
        if (mob.Header.GameObjectType is not ObjectType.Container)
            return null;

        var value = TryGetInt32Property(mob, ObjectField.ContainerFlags);
        return value.HasValue ? unchecked((ContainerFlags)(uint)value.Value) : prototypeEntry?.ContainerFlags;
    }

    private static SceneryFlags? ResolvePlacedOrPrototypeSceneryFlags(
        MobData mob,
        WorkspacePrototypeCatalogEntry? prototypeEntry
    )
    {
        if (mob.Header.GameObjectType is not ObjectType.Scenery)
            return null;

        var value = TryGetInt32Property(mob, ObjectField.SceneryFlags);
        return value.HasValue ? unchecked((SceneryFlags)(uint)value.Value) : prototypeEntry?.SceneryFlags;
    }

    private static int? ResolvePlacedOrPrototypePortalInt32(MobData mob, ObjectField field, int? prototypeValue)
    {
        if (mob.Header.GameObjectType is not ObjectType.Portal)
            return null;

        return TryGetInt32Property(mob, field) ?? prototypeValue;
    }

    private static int? ResolvePlacedOrPrototypeContainerInt32(MobData mob, ObjectField field, int? prototypeValue)
    {
        if (mob.Header.GameObjectType is not ObjectType.Container)
            return null;

        return TryGetInt32Property(mob, field) ?? prototypeValue;
    }

    private static int? TryGetInt32Property(MobData mob, ObjectField field)
    {
        var property = mob.GetProperty(field);
        return property is null ? null : property.GetInt32();
    }

    private static bool IsValidArtId(ArtId? artId) => artId is { Value: not 0u and not uint.MaxValue };
}
