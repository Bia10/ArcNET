using System.Buffers.Binary;
using ArcNET.Core;
using ArcNET.Core.Primitives;
using ArcNET.Formats;
using Bia.ValueBuffers;

namespace ArcNET.Diagnostics;

public static class SaveEmbeddedFileAnalysisService
{
    public static SaveEmbeddedFileDetailSnapshot? TryAnalyze(string fileName, byte[] data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(data);

        var extension = Path.GetExtension(fileName);
        return extension.ToLowerInvariant() switch
        {
            ".mdy" => new SaveDynamicMobileFileDetailSnapshot(fileName, AnalyzeDynamicMobiles(data)),
            ".des" => new SaveDestroyedObjectsFileDetailSnapshot(fileName, AnalyzeDestroyedObjects(data)),
            ".md" => new SaveModifiedObjectsFileDetailSnapshot(fileName, AnalyzeModifiedObjects(data)),
            ".tmf" => new SaveTownMapFogFileDetailSnapshot(fileName, AnalyzeTownMapFog(data)),
            ".dif" when IsCompactDifFormat(data) => new SaveCompactDifFileDetailSnapshot(
                fileName,
                AnalyzeCompactDif(data)
            ),
            ".dat" when fileName.Equals("TimeEvent.dat", StringComparison.OrdinalIgnoreCase) =>
                new SaveTimeEventFileDetailSnapshot(fileName, AnalyzeTimeEvents(data)),
            _ => null,
        };
    }

    public static bool IsCompactDifFormat(byte[] data)
    {
        if (data.Length < 8)
            return false;

        var magic = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(0));
        if (magic == 8u)
        {
            if (
                data.Length >= 20
                && BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(12)) == 0x00000077u
                && BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(16)) == 0x12344321u
            )
            {
                return true;
            }

            return BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(8)) == 0x00000077u
                && BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(12)) == 0x12344321u;
        }

        return magic == 0x18u;
    }

    public static SaveDynamicMobileAnalysisSnapshot AnalyzeDynamicMobiles(ReadOnlyMemory<byte> memory)
    {
        const uint sentinel = 0xFFFFFFFF;
        var reader = new SpanReader(memory.Span);
        var count = 0;
        var skipped = 0;
        List<SaveDynamicMobileEntrySnapshot> entries = [];

        while (reader.Remaining >= 4)
        {
            if (unchecked((uint)reader.PeekInt32At(0)) == sentinel)
            {
                reader.Skip(4);
                skipped++;
                continue;
            }

            var nextVersion = reader.PeekInt32At(0);
            if (nextVersion is not (0x08 or 0x77))
                break;

            var offset = reader.Position;
            try
            {
                var mob = MobFormat.Parse(ref reader);
                count++;
                entries.Add(new SaveDynamicMobileEntrySnapshot(count, offset, mob, null));
            }
            catch (Exception ex)
            {
                entries.Add(new SaveDynamicMobileEntrySnapshot(count + 1, offset, null, ex.Message));
                break;
            }
        }

        return new SaveDynamicMobileAnalysisSnapshot(count, skipped, entries);
    }

    public static SaveCompactDifAnalysisSnapshot AnalyzeCompactDif(byte[] data)
    {
        const uint startMarker = 0x12344321u;
        const uint endMarker = 0x23455432u;
        var span = data.AsSpan();
        var magic = BinaryPrimitives.ReadInt32LittleEndian(span);

        if (magic == 0x18)
        {
            var position = 4;
            var recordIndex = 0;
            List<SaveCompactDifRecordSnapshot> records = [];
            while (position + 4 <= data.Length)
            {
                var word = BinaryPrimitives.ReadUInt32LittleEndian(span[position..]);
                if (word == startMarker)
                {
                    position += 4;
                    var dataStart = position;
                    while (position + 4 <= data.Length)
                    {
                        if (BinaryPrimitives.ReadUInt32LittleEndian(span[position..]) == endMarker)
                            break;

                        position += 4;
                    }

                    recordIndex++;
                    records.Add(new SaveCompactDifRecordSnapshot(recordIndex, null, position - dataStart));
                    position += 4;
                }
                else
                {
                    position += 4;
                }
            }

            return new SaveCompactDifAnalysisSnapshot(
                magic,
                SaveCompactDifVariant.VariantCMagic18,
                records,
                null,
                records.Count == 0
            );
        }

        var hasB = data.Length >= 16 && BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(12)) == 0x00000077u;
        var variant = hasB ? SaveCompactDifVariant.VariantAWithB : SaveCompactDifVariant.VariantBWithoutB;
        var scanPosition = 4;
        var minHeader = hasB ? 16 : 12;
        var recordCounter = 0;
        List<SaveCompactDifRecordSnapshot> parsedRecords = [];

        while (scanPosition + minHeader <= data.Length)
        {
            int? b = null;
            if (hasB)
            {
                b = BinaryPrimitives.ReadInt32LittleEndian(span[scanPosition..]);
                scanPosition += 4;
            }

            var c = BinaryPrimitives.ReadUInt32LittleEndian(span[scanPosition..]);
            scanPosition += 4;
            var d = BinaryPrimitives.ReadUInt32LittleEndian(span[scanPosition..]);
            scanPosition += 4;
            if ((c & 0x80000000u) == 0 || d != 0x00000077u)
                break;

            var start = BinaryPrimitives.ReadUInt32LittleEndian(span[scanPosition..]);
            scanPosition += 4;
            if (start != startMarker)
                break;

            var dataStart = scanPosition;
            while (scanPosition + 4 <= data.Length)
            {
                if (BinaryPrimitives.ReadUInt32LittleEndian(span[scanPosition..]) == endMarker)
                    break;

                scanPosition += 4;
            }

            recordCounter++;
            parsedRecords.Add(new SaveCompactDifRecordSnapshot(recordCounter, b, scanPosition - dataStart));
            scanPosition += 4;
        }

        var trailingValue =
            scanPosition < data.Length ? BinaryPrimitives.ReadInt32LittleEndian(span[scanPosition..]) : (int?)null;
        return new SaveCompactDifAnalysisSnapshot(magic, variant, parsedRecords, trailingValue, false);
    }

    public static SaveDestroyedObjectsAnalysisSnapshot AnalyzeDestroyedObjects(ReadOnlyMemory<byte> memory)
    {
        const int objectIdSize = 24;
        var span = memory.Span;
        if (span.Length == 0)
            return new SaveDestroyedObjectsAnalysisSnapshot(0, false, []);

        if (span.Length % objectIdSize != 0)
            return new SaveDestroyedObjectsAnalysisSnapshot(span.Length, true, []);

        var reader = new SpanReader(span);
        List<string> objectIds = [];
        for (var index = 0; index < span.Length / objectIdSize; index++)
            objectIds.Add(GameObjectGuid.Read(ref reader).ToString());

        return new SaveDestroyedObjectsAnalysisSnapshot(span.Length, false, objectIds);
    }

    public static SaveModifiedObjectsAnalysisSnapshot AnalyzeModifiedObjects(ReadOnlyMemory<byte> memory)
    {
        const int objectIdSize = 24;
        const int version8 = 0x08;
        const int version77 = 0x77;
        const uint startMarker = 0x12344321u;
        const uint endMarker = 0x23455432u;

        var span = memory.Span;
        var position = 0;
        var count = 0;
        List<SaveModifiedObjectEntrySnapshot> entries = [];
        string? terminalWarning = null;

        while (position + objectIdSize + 8 <= span.Length)
        {
            var objectIdReader = new SpanReader(span.Slice(position, objectIdSize));
            var fileObjectId = GameObjectGuid.Read(ref objectIdReader).ToString();
            position += objectIdSize;

            if (position + 4 > span.Length)
                break;

            var version = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(position, 4));
            position += 4;
            if (version is not (version8 or version77))
            {
                terminalWarning = $"  (unexpected version {version} at byte {position - 4} — stopping)";
                break;
            }

            if (position + 4 > span.Length)
                break;

            var start = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(position, 4));
            position += 4;
            if (start != startMarker)
            {
                terminalWarning = $"  (start marker mismatch 0x{start:X8} at byte {position - 4} — stopping)";
                break;
            }

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
                var mob = MobFormat.Parse(ref reader);
                var consumed = reader.Position - 4;
                string? warning = null;

                if (consumed + 4 <= remaining.Length)
                {
                    var end = BinaryPrimitives.ReadUInt32LittleEndian(remaining.Slice(consumed, 4));
                    if (end != endMarker)
                        warning = $"  WARNING: end marker missing after object {count}";

                    position += consumed + 4;
                }
                else
                {
                    position += consumed;
                }

                entries.Add(new SaveModifiedObjectEntrySnapshot(count, fileObjectId, mob, null, warning));
            }
            catch (Exception ex)
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
                {
                    entries.Add(
                        new SaveModifiedObjectEntrySnapshot(
                            count,
                            fileObjectId,
                            null,
                            ex.Message + "; end marker not found — stopping",
                            null
                        )
                    );
                    break;
                }

                entries.Add(new SaveModifiedObjectEntrySnapshot(count, fileObjectId, null, ex.Message, null));
            }
        }

        return new SaveModifiedObjectsAnalysisSnapshot(entries, terminalWarning);
    }

    public static SaveTimeEventAnalysisSnapshot AnalyzeTimeEvents(ReadOnlyMemory<byte> memory)
    {
        var span = memory.Span;
        if (span.Length < 4)
            return new SaveTimeEventAnalysisSnapshot(true, memory.Length, 0, [], false);

        var count = BinaryPrimitives.ReadInt32LittleEndian(span);
        var reader = new SpanReader(span.Slice(4));
        List<SaveTimeEventEntrySnapshot> entries = [];

        for (var index = 0; index < count && reader.Remaining >= 12; index++)
        {
            entries.Add(
                new SaveTimeEventEntrySnapshot(index + 1, reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32())
            );
            if (index == 0 && count > 1)
                break;
        }

        return new SaveTimeEventAnalysisSnapshot(false, memory.Length, count, entries, count > entries.Count);
    }

    public static SaveTownMapFogFileAnalysisSnapshot AnalyzeTownMapFog(ReadOnlyMemory<byte> memory)
    {
        var fog = TownMapFogFormat.ParseMemory(memory);
        return new SaveTownMapFogFileAnalysisSnapshot(
            memory.Length,
            fog.RevealedTiles,
            fog.TotalTiles,
            fog.CoveragePercent
        );
    }
}
