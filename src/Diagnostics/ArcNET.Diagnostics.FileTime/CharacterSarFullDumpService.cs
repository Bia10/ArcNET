using System.Buffers.Binary;
using System.Text;

namespace ArcNET.Diagnostics;

public static class CharacterSarFullDumpService
{
    public static CharacterSarFullDumpSnapshot Create(byte[] rawBytes, int? bitsetWordCountFilter = null)
    {
        ArgumentNullException.ThrowIfNull(rawBytes);

        List<CharacterSarFullDumpEntrySnapshot> entries = [];
        foreach (var sar in CharacterSarDiagnostics.Parse(rawBytes))
        {
            if (!bitsetWordCountFilter.HasValue && sar.IsFiller)
                continue;
            if (bitsetWordCountFilter.HasValue && sar.BitsetWordCount != bitsetWordCountFilter.Value)
                continue;

            var annotation = CharacterSarDiagnostics.AnnotateBsId(sar.BitsetId);
            if (string.IsNullOrEmpty(annotation))
                annotation = CharacterSarDiagnostics.AnnotateFingerprint(sar.Fingerprint);

            entries.Add(
                new CharacterSarFullDumpEntrySnapshot(
                    sar.Offset,
                    sar.BitsetId,
                    sar.ElementSize,
                    sar.ElementCount,
                    sar.BitsetWordCount,
                    sar.Fingerprint,
                    annotation,
                    sar.IsFiller,
                    sar.ElementSize == 4 ? CreateInt32Rows(rawBytes, sar) : [],
                    sar.ElementSize == 1 ? FormatHex(rawBytes.AsSpan(sar.DataOffset, sar.ElementCount)) : null,
                    sar.ElementSize == 1
                        ? FormatPrintableAscii(rawBytes.AsSpan(sar.DataOffset, sar.ElementCount))
                        : null,
                    sar.ElementSize is not 1 and not 4 ? CreateElementHexes(rawBytes, sar, 20) : [],
                    sar.ElementSize is not 1 and not 4 ? Math.Max(0, sar.ElementCount - 20) : 0,
                    sar.BitSlots
                )
            );
        }

        return new CharacterSarFullDumpSnapshot(entries);
    }

    private static IReadOnlyList<CharacterSarInt32RowSnapshot> CreateInt32Rows(
        byte[] rawBytes,
        CharacterSarEntrySnapshot sar
    )
    {
        const int perRow = 16;
        List<CharacterSarInt32RowSnapshot> rows = [];
        for (var row = 0; row < sar.ElementCount; row += perRow)
        {
            var end = Math.Min(row + perRow, sar.ElementCount);
            List<int> values = [];
            for (var index = row; index < end; index++)
                values.Add(BinaryPrimitives.ReadInt32LittleEndian(rawBytes.AsSpan(sar.DataOffset + index * 4, 4)));
            rows.Add(new CharacterSarInt32RowSnapshot(row, values));
        }

        return rows;
    }

    private static IReadOnlyList<CharacterSarElementHexSnapshot> CreateElementHexes(
        byte[] rawBytes,
        CharacterSarEntrySnapshot sar,
        int maxElements
    )
    {
        List<CharacterSarElementHexSnapshot> elements = [];
        for (var index = 0; index < Math.Min(sar.ElementCount, maxElements); index++)
        {
            var bytes = rawBytes.AsSpan(sar.DataOffset + index * sar.ElementSize, sar.ElementSize);
            elements.Add(new CharacterSarElementHexSnapshot(index, FormatHex(bytes)));
        }

        return elements;
    }

    private static string FormatHex(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
            return string.Empty;

        var builder = new StringBuilder(bytes.Length * 2);
        foreach (var value in bytes)
            builder.Append(value.ToString("X2"));
        return builder.ToString();
    }

    private static string FormatPrintableAscii(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
            return string.Empty;

        var builder = new StringBuilder(bytes.Length);
        foreach (var value in bytes)
            builder.Append(value is >= 32 and < 127 ? (char)value : '.');
        return builder.ToString();
    }
}
