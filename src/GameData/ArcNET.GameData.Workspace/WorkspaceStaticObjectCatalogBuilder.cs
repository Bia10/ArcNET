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
            sourceAssetPath,
            TryFormatObjectLocation(mob),
            $"{sourceKindText} - {sourceAssetPath}"
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
}
