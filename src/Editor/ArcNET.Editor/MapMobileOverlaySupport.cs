using ArcNET.Core;
using ArcNET.Core.Primitives;
using ArcNET.Formats;
using ArcNET.GameData;
using ArcNET.GameObjects;

namespace ArcNET.Editor;

internal static class MapMobileOverlaySupport
{
    private const int PackedMapMobileHeaderSize = 16;
    private const string MapsPrefix = "maps/";
    private const string MobileMdFileName = "mobile.md";

    public static bool IsPackedMapMobileAssetPath(string assetPath)
    {
        if (Path.GetExtension(assetPath).Length != 0)
            return false;

        if (!assetPath.StartsWith(MapsPrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var relativePath = assetPath[MapsPrefix.Length..];
        return relativePath.Length > 0 && !relativePath.Contains('/');
    }

    public static bool IsMapMobileDiffPath(string assetPath) => TryExtractMapNameFromMobileMdPath(assetPath, out _);

    public static bool TryExpandPackedMapMobileAsset(
        string packedAssetPath,
        ReadOnlyMemory<byte> packedData,
        out IReadOnlyList<PackedMapMobAsset> mapMobs
    )
    {
        mapMobs = [];
        if (!IsPackedMapMobileAssetPath(packedAssetPath))
            return false;

        if (!TryDecodeMapNameFromPackedPath(packedAssetPath, out var mapName))
            return false;

        if (packedData.Length <= PackedMapMobileHeaderSize)
            return false;

        var payload = packedData.Span[PackedMapMobileHeaderSize..];
        var reader = new SpanReader(payload);
        var extractedAssets = new List<PackedMapMobAsset>();
        var pathsInUse = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (reader.Remaining > 0)
        {
            var start = reader.Position;
            MobData mob;
            try
            {
                mob = MobFormat.Parse(ref reader);
            }
            catch (Exception ex) when (ex is InvalidDataException or ArgumentOutOfRangeException)
            {
                mapMobs = [];
                return false;
            }

            if (reader.Position <= start)
            {
                mapMobs = [];
                return false;
            }

            var end = reader.Position;
            var mobBytes = payload[start..end].ToArray();

            var candidatePath = BuildPackedMobAssetPath(mapName, mob.Header.ObjectId);
            var suffix = 1;
            while (!pathsInUse.Add(candidatePath))
            {
                candidatePath = BuildPackedMobAssetPath(mapName, mob.Header.ObjectId, suffix);
                suffix++;
            }

            extractedAssets.Add(new PackedMapMobAsset(candidatePath, mobBytes));
        }

        if (extractedAssets.Count == 0)
            return false;

        mapMobs = extractedAssets;
        return true;
    }

    public static bool TryParseMobileMd(ReadOnlyMemory<byte> fileContent, out MobileMdFile mobileMd)
    {
        try
        {
            mobileMd = MobileMdFormat.ParseMemory(fileContent);
            return true;
        }
        catch (Exception ex) when (ex is InvalidDataException or ArgumentOutOfRangeException)
        {
            mobileMd = default!;
            return false;
        }
    }

    public static GameDataStore ApplyMobileMdOverlays(
        GameDataStore gameData,
        IReadOnlyDictionary<string, MobileMdFile> mobileMdsByPath
    )
    {
        ArgumentNullException.ThrowIfNull(gameData);
        ArgumentNullException.ThrowIfNull(mobileMdsByPath);

        if (mobileMdsByPath.Count == 0 || gameData.MobsBySource.Count == 0)
            return gameData;

        var mapMobAssetIndex = BuildMapMobAssetIndex(gameData.MobsBySource);
        if (mapMobAssetIndex.Count == 0)
            return gameData;

        Dictionary<string, MobData>? updatedMobs = null;
        foreach (var (mobileMdPath, mobileMd) in mobileMdsByPath)
        {
            if (!TryExtractMapNameFromMobileMdPath(mobileMdPath, out var mapName))
                continue;

            if (!mapMobAssetIndex.TryGetValue(mapName, out var objectIdToMobPath))
                continue;

            foreach (var record in mobileMd.Records)
            {
                if (record.Data is null)
                    continue;

                if (!objectIdToMobPath.TryGetValue(record.MapObjectId, out var mobAssetPath))
                    continue;

                updatedMobs ??= new Dictionary<string, MobData>(StringComparer.OrdinalIgnoreCase);
                updatedMobs[mobAssetPath] = record.Data;
            }
        }

        return updatedMobs is null
            ? gameData
            : GameDataStoreSnapshotBuilder.CloneWithAssetReplacements(gameData, updatedMobs: updatedMobs);
    }

    private static Dictionary<string, Dictionary<GameObjectGuid, string>> BuildMapMobAssetIndex(
        IReadOnlyDictionary<string, IReadOnlyList<MobData>> mobsBySource
    )
    {
        var mapMobAssetIndex = new Dictionary<string, Dictionary<GameObjectGuid, string>>(
            StringComparer.OrdinalIgnoreCase
        );
        foreach (var (mobAssetPath, mobs) in mobsBySource)
        {
            if (!TryExtractMapName(mobAssetPath, out var mapName))
                continue;

            if (!mapMobAssetIndex.TryGetValue(mapName, out var objectIdLookup))
            {
                objectIdLookup = new Dictionary<GameObjectGuid, string>();
                mapMobAssetIndex[mapName] = objectIdLookup;
            }

            foreach (var mob in mobs)
                objectIdLookup[mob.Header.ObjectId] = mobAssetPath;
        }

        return mapMobAssetIndex;
    }

    private static bool TryDecodeMapNameFromPackedPath(string packedAssetPath, out string mapName)
    {
        mapName = string.Empty;
        if (!IsPackedMapMobileAssetPath(packedAssetPath))
            return false;

        var obfuscatedName = packedAssetPath[MapsPrefix.Length..];
        if (obfuscatedName.Length == 0)
            return false;

        mapName = DecodeMapName(obfuscatedName);
        return mapName.Length > 0;
    }

    private static string DecodeMapName(string obfuscatedName)
    {
        var chars = obfuscatedName.Reverse().ToArray();
        for (var index = 0; index < chars.Length; index++)
            chars[index] = Rot13(chars[index]);
        return new string(chars);
    }

    private static char Rot13(char value) =>
        value switch
        {
            >= 'a' and <= 'z' => (char)('a' + ((value - 'a' + 13) % 26)),
            >= 'A' and <= 'Z' => (char)('A' + ((value - 'A' + 13) % 26)),
            _ => value,
        };

    private static string BuildPackedMobAssetPath(string mapName, GameObjectGuid objectId, int suffix = 0)
    {
        var guidToken =
            objectId.Id == Guid.Empty
                ? "00000000_0000_0000_0000_000000000000"
                : objectId.Id.ToString("D").ToUpperInvariant().Replace('-', '_');
        var basePath = $"maps/{mapName}/mobile/G_{guidToken}.mob";
        return suffix == 0 ? basePath : $"maps/{mapName}/mobile/G_{guidToken}_{suffix}.mob";
    }

    private static bool TryExtractMapNameFromMobileMdPath(string assetPath, out string mapName)
    {
        mapName = string.Empty;
        if (!assetPath.StartsWith(MapsPrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var relativePath = assetPath[MapsPrefix.Length..];
        var separatorIndex = relativePath.IndexOf('/');
        if (separatorIndex <= 0)
            return false;

        mapName = relativePath[..separatorIndex];
        var fileName = relativePath[(separatorIndex + 1)..];
        return fileName.Equals(MobileMdFileName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryExtractMapName(string assetPath, out string mapName)
    {
        mapName = string.Empty;
        if (!assetPath.StartsWith(MapsPrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var relativePath = assetPath[MapsPrefix.Length..];
        var separatorIndex = relativePath.IndexOf('/');
        if (separatorIndex <= 0)
            return false;

        mapName = relativePath[..separatorIndex];
        return mapName.Length > 0;
    }

    public sealed record PackedMapMobAsset(string AssetPath, byte[] MobBytes);
}
