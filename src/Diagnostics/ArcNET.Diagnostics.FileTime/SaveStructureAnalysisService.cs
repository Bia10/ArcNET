using System.Buffers.Binary;
using ArcNET.Core;
using ArcNET.Core.Primitives;
using ArcNET.Formats;
using ArcNET.GameData.SaveGames;
using Bia.ValueBuffers;

namespace ArcNET.Diagnostics;

public static class SaveStructureAnalysisService
{
    public static SaveStructureAnalysisSnapshot Create(string gsiPath, string tfaiPath, string tfafPath) =>
        Create(SaveSlotLoadService.LoadFiles(gsiPath, tfaiPath, tfafPath));

    public static SaveStructureAnalysisSnapshot Create(LoadedSave save)
    {
        ArgumentNullException.ThrowIfNull(save);

        var info = save.Info;
        var totalMilliseconds = (long)info.GameTimeDays * 86_400_000L + info.GameTimeMs;
        var clock = new SaveGameClockSnapshot(
            info.GameTimeDays + 1,
            (int)(totalMilliseconds / 3_600_000L % 24),
            (int)(totalMilliseconds / 60_000L % 60),
            (int)(totalMilliseconds / 1_000L % 60)
        );

        return new SaveStructureAnalysisSnapshot(
            info.DisplayName,
            info.LeaderName,
            info.LeaderLevel,
            info.LeaderPortraitId,
            info.ModuleName,
            info.MapId,
            info.LeaderTileX,
            info.LeaderTileY,
            clock,
            save.Files.Count,
            CreateExtensionSummary(save),
            CreateExploredAreaCoverage(save),
            CreateMapWorldStateSummary(save)
        );
    }

    private static IReadOnlyList<SaveEmbeddedFileExtensionSnapshot> CreateExtensionSummary(LoadedSave save) =>
        [
            .. save
                .Files.GroupBy(static entry => Path.GetExtension(entry.Key).ToLowerInvariant())
                .OrderBy(static group => group.Key, StringComparer.Ordinal)
                .Select(static group => new SaveEmbeddedFileExtensionSnapshot(
                    group.Key,
                    group.Key.Length > 0 ? group.Key : "(no ext)",
                    group.Count(),
                    group.Sum(static entry => (long)entry.Value.Length)
                )),
        ];

    private static IReadOnlyList<SaveExploredAreaCoverageSnapshot> CreateExploredAreaCoverage(LoadedSave save) =>
        [
            .. save
                .TownMapFogs.OrderBy(
                    static entry => Path.GetFileNameWithoutExtension(entry.Key),
                    StringComparer.OrdinalIgnoreCase
                )
                .Select(static entry => new SaveExploredAreaCoverageSnapshot(
                    Path.GetFileNameWithoutExtension(entry.Key),
                    entry.Value.RevealedTiles,
                    entry.Value.TotalTiles,
                    entry.Value.CoveragePercent
                )),
        ];

    private static IReadOnlyList<SaveMapWorldStateSnapshot> CreateMapWorldStateSummary(LoadedSave save) =>
        [
            .. save
                .Files.Where(static entry => entry.Key.StartsWith("maps/", StringComparison.OrdinalIgnoreCase))
                .GroupBy(static entry => GetMapName(entry.Key), StringComparer.OrdinalIgnoreCase)
                .OrderBy(static group => group.Key, StringComparer.OrdinalIgnoreCase)
                .Select(CreateMapWorldState),
        ];

    private static SaveMapWorldStateSnapshot CreateMapWorldState(
        IGrouping<string, KeyValuePair<string, byte[]>> mapGroup
    )
    {
        var destroyedBytes = GetMapFileBytes(mapGroup, "mobile.des");
        var modifiedBytes = GetMapFileBytes(mapGroup, "mobile.md");
        var dynamicBytes = GetMapFileBytes(mapGroup, "mobile.mdy");

        return new SaveMapWorldStateSnapshot(
            mapGroup.Key,
            CountDestroyedObjects(destroyedBytes),
            CountModifiedObjects(modifiedBytes),
            CountDynamicMobiles(dynamicBytes),
            mapGroup.Count(static entry =>
                Path.GetExtension(entry.Key).Equals(".dif", StringComparison.OrdinalIgnoreCase)
            )
        );
    }

    private static byte[]? GetMapFileBytes(IGrouping<string, KeyValuePair<string, byte[]>> mapGroup, string fileName) =>
        mapGroup
            .Where(entry => Path.GetFileName(entry.Key).Equals(fileName, StringComparison.OrdinalIgnoreCase))
            .Select(static entry => entry.Value)
            .FirstOrDefault();

    private static string GetMapName(string path)
    {
        var relativePath = path["maps/".Length..];
        var slashIndex = relativePath.IndexOf('/');
        return slashIndex >= 0 ? relativePath[..slashIndex] : relativePath;
    }

    private static int CountDestroyedObjects(byte[]? data) =>
        data is { Length: > 0 } && data.Length % 24 == 0 ? data.Length / 24 : 0;

    private static int CountDynamicMobiles(byte[]? data)
    {
        if (data is not { Length: > 0 })
            return 0;

        const uint startMarker = 0x12344321u;
        var count = 0;
        for (var index = 0; index + 4 <= data.Length; index += 4)
        {
            if (BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(index, 4)) == startMarker)
                count++;
        }

        return count;
    }

    private static int CountModifiedObjects(byte[]? data)
    {
        if (data is not { Length: > 0 })
            return 0;

        const int objectIdSize = 24;
        const uint startMarker = 0x12344321u;
        const uint endMarker = 0x23455432u;
        var span = data.AsSpan();
        var count = 0;
        var position = 0;

        while (position + objectIdSize + 8 <= data.Length)
        {
            position += objectIdSize;
            var version = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(position, 4));
            position += 4;
            if (version is not (0x08 or 0x77))
                break;

            var start = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(position, 4));
            position += 4;
            if (start != startMarker)
                break;

            count++;
            var remaining = span.Slice(position);

#pragma warning disable CA2014 // 256-B seed; ValueByteBuffer grows via ArrayPool (not stack) when the record is larger
            Span<byte> initialBuffer = stackalloc byte[256];
#pragma warning restore CA2014
            using var combinedBuffer = new ValueByteBuffer(initialBuffer);
            combinedBuffer.WriteInt32LittleEndian(version);
            combinedBuffer.Write(remaining);

            try
            {
                var reader = new SpanReader(combinedBuffer.WrittenSpan);
                MobFormat.Parse(ref reader);
                var consumed = reader.Position - 4;
                position += consumed;
                if (
                    position + 4 <= data.Length
                    && BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(position, 4)) == endMarker
                )
                {
                    position += 4;
                }
            }
            catch (Exception)
            {
                var foundEnd = false;
                for (var index = 0; index <= remaining.Length - 4; index++)
                {
                    if (BinaryPrimitives.ReadUInt32LittleEndian(remaining.Slice(index, 4)) != endMarker)
                        continue;

                    position += index + 4;
                    foundEnd = true;
                    break;
                }

                if (!foundEnd)
                    break;
            }
        }

        return count;
    }
}
