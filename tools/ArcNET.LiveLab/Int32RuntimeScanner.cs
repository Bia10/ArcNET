using System.Buffers.Binary;
using System.Runtime.Versioning;

namespace ArcNET.LiveLab;

[SupportedOSPlatform("windows")]
internal static class Int32RuntimeScanner
{
    public static object[] FindSequenceMatches(byte[] buffer, nint start, int[] expected, int maxMatches = int.MaxValue)
    {
        var matches = new List<object>();
        var neededBytes = checked(expected.Length * sizeof(int));

        for (var offset = 0; offset <= buffer.Length - neededBytes && matches.Count < maxMatches; offset += sizeof(int))
        {
            if (!HasInt32Sequence(buffer, offset, expected))
                continue;

            matches.Add(
                new
                {
                    Address = ProcessMemory.FormatAddress(start + offset),
                    Offset = offset,
                    Values = expected,
                }
            );
        }

        return matches.ToArray();
    }

    public static (
        object[] Matches,
        int RegionsScanned,
        ulong BytesScanned,
        int SkippedRegions
    ) FindSequenceMatchesGlobal(ProcessMemory memory, int[] expected, int chunkSize, int maxMatches)
    {
        var overlapBytes = Math.Max(0, checked(expected.Length * sizeof(int)) - sizeof(int));
        var matches = new List<object>();
        ulong bytesScanned = 0;
        var regionsScanned = 0;
        var skippedRegions = 0;

        foreach (var region in memory.EnumerateCommittedReadableRegions())
        {
            if ((ulong)region.Size < (ulong)(expected.Length * sizeof(int)))
                continue;

            regionsScanned++;
            bytesScanned += (ulong)region.Size;

            try
            {
                ScanRegionForSequence(memory, region, expected, chunkSize, overlapBytes, matches, maxMatches);
            }
            catch
            {
                skippedRegions++;
            }

            if (matches.Count >= maxMatches)
                break;
        }

        return (matches.ToArray(), regionsScanned, bytesScanned, skippedRegions);
    }

    public static object[] ScanRecordMatches(byte[] bytes, nint start, int recordIntCount, int[] header, int maxMatches)
    {
        var matches = new List<object>();
        var recordBytes = checked(recordIntCount * sizeof(int));

        for (var offset = 0; offset <= bytes.Length - recordBytes && matches.Count < maxMatches; offset += sizeof(int))
        {
            if (!HasInt32Sequence(bytes, offset, header))
                continue;

            var values = Enumerable
                .Range(0, recordIntCount)
                .Select(index =>
                {
                    var valueOffset = index * sizeof(int);
                    var value = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset + valueOffset, sizeof(int)));

                    return new
                    {
                        Index = index,
                        Offset = valueOffset,
                        Address = ProcessMemory.FormatAddress(start + offset + valueOffset),
                        Value = value,
                        Hex = $"0x{unchecked((uint)value):X8}",
                    };
                })
                .ToArray();

            matches.Add(
                new
                {
                    Address = ProcessMemory.FormatAddress(start + offset),
                    Offset = offset,
                    Values = values,
                }
            );
        }

        return matches.ToArray();
    }

    public static object[] PatchRecordFields(
        ProcessMemory memory,
        nint start,
        byte[] bytes,
        int recordIntCount,
        int fieldIndex,
        int expectedValue,
        int newValue,
        int[] header,
        int maxWrites
    ) =>
        PatchRecordFieldsCore(
            memory,
            start,
            bytes,
            recordIntCount,
            fieldIndex,
            expectedValue,
            newValue,
            header,
            maxWrites,
            false
        );

    public static object[] PatchAddresses(ProcessMemory memory, int newValue, IReadOnlyList<nint> addresses)
    {
        return addresses
            .Select(address =>
            {
                var before = memory.ReadInt32(address);
                memory.WriteInt32(address, newValue);
                var after = memory.ReadInt32(address);

                return (object)
                    new
                    {
                        Kind = "address",
                        Address = ProcessMemory.FormatAddress(address),
                        Before = before,
                        After = after,
                    };
            })
            .ToArray();
    }

    public static object[] PatchRecordFieldInRange(
        ProcessMemory memory,
        nint start,
        int byteCount,
        int recordIntCount,
        int fieldIndex,
        int expectedValue,
        int newValue,
        int[] header
    )
    {
        const int maxWrites = 256;

        var bytes = memory.ReadBytes(start, byteCount);
        return PatchRecordFieldsCore(
            memory,
            start,
            bytes,
            recordIntCount,
            fieldIndex,
            expectedValue,
            newValue,
            header,
            maxWrites,
            true
        );
    }

    private static object[] PatchRecordFieldsCore(
        ProcessMemory memory,
        nint start,
        byte[] bytes,
        int recordIntCount,
        int fieldIndex,
        int expectedValue,
        int newValue,
        int[] header,
        int maxWrites,
        bool includeKind
    )
    {
        var writes = new List<object>();
        var recordBytes = checked(recordIntCount * sizeof(int));
        var fieldOffset = checked(fieldIndex * sizeof(int));

        for (var offset = 0; offset <= bytes.Length - recordBytes && writes.Count < maxWrites; offset += sizeof(int))
        {
            if (!HasInt32Sequence(bytes, offset, header))
                continue;

            var before = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset + fieldOffset, sizeof(int)));
            if (before != expectedValue)
                continue;

            var fieldAddress = start + offset + fieldOffset;
            memory.WriteInt32(fieldAddress, newValue);
            var after = memory.ReadInt32(fieldAddress);

            writes.Add(
                includeKind
                    ? new
                    {
                        Kind = "record",
                        RecordAddress = ProcessMemory.FormatAddress(start + offset),
                        FieldIndex = fieldIndex,
                        FieldAddress = ProcessMemory.FormatAddress(fieldAddress),
                        Before = before,
                        After = after,
                    }
                    : new
                    {
                        RecordAddress = ProcessMemory.FormatAddress(start + offset),
                        FieldIndex = fieldIndex,
                        FieldAddress = ProcessMemory.FormatAddress(fieldAddress),
                        Before = before,
                        After = after,
                    }
            );
        }

        return writes.ToArray();
    }

    private static void ScanRegionForSequence(
        ProcessMemory memory,
        ProcessMemory.MemoryRegion region,
        int[] expected,
        int chunkSize,
        int overlapBytes,
        List<object> matches,
        int maxMatches
    )
    {
        var regionBase = (ulong)memory.ToUInt32Address(region.BaseAddress);
        var regionSize = (ulong)region.Size;
        var cursor = 0UL;
        byte[] carry = [];

        while (cursor < regionSize && matches.Count < maxMatches)
        {
            var remaining = regionSize - cursor;
            var readSize = (int)Math.Min((ulong)chunkSize, remaining);
            var address = (nint)(long)(regionBase + cursor);
            var bytes = memory.ReadBytes(address, readSize);

            var combined = new byte[carry.Length + bytes.Length];
            carry.CopyTo(combined, 0);
            bytes.CopyTo(combined, carry.Length);

            var combinedBase = regionBase + cursor - (ulong)carry.Length;
            AppendSequenceMatches(combined, combinedBase, expected, matches, maxMatches);

            if (overlapBytes == 0)
            {
                carry = [];
            }
            else
            {
                var preserved = Math.Min(overlapBytes, combined.Length);
                carry = combined[^preserved..];
            }

            cursor += (ulong)readSize;
        }
    }

    private static void AppendSequenceMatches(
        byte[] buffer,
        ulong bufferBase,
        int[] expected,
        List<object> matches,
        int maxMatches
    )
    {
        var neededBytes = checked(expected.Length * sizeof(int));
        for (var offset = 0; offset <= buffer.Length - neededBytes && matches.Count < maxMatches; offset += sizeof(int))
        {
            if (!HasInt32Sequence(buffer, offset, expected))
                continue;

            matches.Add(
                new
                {
                    Address = ProcessMemory.FormatAddress((nint)(long)(bufferBase + (ulong)offset)),
                    Offset = offset,
                    Values = expected,
                }
            );
        }
    }

    private static bool HasInt32Sequence(byte[] buffer, int offset, int[] expected)
    {
        for (var index = 0; index < expected.Length; index++)
        {
            var current = BinaryPrimitives.ReadInt32LittleEndian(
                buffer.AsSpan(offset + index * sizeof(int), sizeof(int))
            );
            if (current != expected[index])
                return false;
        }

        return true;
    }
}
