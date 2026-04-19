using System.Buffers.Binary;
using ArcNET.Core;

namespace ArcNET.Formats;

/// <summary>
/// Span-based parser and writer for the currently verified <c>data2.sav</c> structure.
/// The implementation is intentionally narrow: it decodes the stable alternating
/// <c>[state, 50000+ id]</c> table and preserves every unresolved byte verbatim.
/// </summary>
public sealed class Data2SavFormat : IFormatFileReader<Data2SavFile>, IFormatFileWriter<Data2SavFile>
{
    private const int MinimumPairCount = 32;
    private const int MinValue = -1;
    private const int MaxValue = 32;
    private const int MinId = 50000;
    private const int MaxId = 60000;

    /// <inheritdoc/>
    public static Data2SavFile Parse(scoped ref SpanReader reader)
    {
        var rawBytes = reader.ReadBytes(reader.Remaining).ToArray();
        return ParseBytes(rawBytes);
    }

    /// <inheritdoc/>
    public static Data2SavFile ParseMemory(ReadOnlyMemory<byte> memory) =>
        FormatIo.ParseMemory<Data2SavFormat, Data2SavFile>(memory);

    /// <inheritdoc/>
    public static Data2SavFile ParseFile(string path) => FormatIo.ParseFile<Data2SavFormat, Data2SavFile>(path);

    /// <inheritdoc/>
    public static void Write(in Data2SavFile value, ref SpanWriter writer)
    {
        var patched = BuildPatchedBytes(in value);
        writer.WriteBytes(patched);
    }

    /// <inheritdoc/>
    public static byte[] WriteToArray(in Data2SavFile value) =>
        FormatIo.WriteToArray<Data2SavFormat, Data2SavFile>(in value);

    /// <inheritdoc/>
    public static void WriteToFile(in Data2SavFile value, string path) =>
        FormatIo.WriteToFile<Data2SavFormat, Data2SavFile>(in value, path);

    private static Data2SavFile ParseBytes(byte[] rawBytes)
    {
        if (!TryDetectIdPairTable(rawBytes, out var startInt, out var pairCount))
            throw new InvalidDataException("data2.sav does not contain a recognized 50000+ ID pair table.");

        var idPairs = new Data2SavIdPairEntry[pairCount];
        for (var index = 0; index < pairCount; index++)
        {
            var value = ReadInt32(rawBytes, startInt + index * 2);
            var id = ReadInt32(rawBytes, startInt + index * 2 + 1);
            idPairs[index] = new Data2SavIdPairEntry(id, value);
        }

        return new Data2SavFile
        {
            RawBytes = rawBytes,
            IdPairTableStartInt = startInt,
            IdPairs = idPairs,
        };
    }

    private static byte[] BuildPatchedBytes(in Data2SavFile value)
    {
        ArgumentNullException.ThrowIfNull(value.RawBytes);

        var pairCount = value.IdPairs.Count;
        if (pairCount == 0)
            throw new InvalidDataException("data2.sav write requires at least one decoded ID pair entry.");

        var totalInts = value.RawBytes.Length / 4;
        if (value.IdPairTableStartInt < 0 || value.IdPairTableStartInt + pairCount * 2 > totalInts)
            throw new InvalidDataException("data2.sav ID pair table bounds are outside the preserved raw payload.");

        var patched = value.RawBytes.ToArray();
        for (var index = 0; index < pairCount; index++)
        {
            var intIndex = value.IdPairTableStartInt + index * 2;
            var idIndex = intIndex + 1;
            var expectedId = ReadInt32(patched, idIndex);
            var entry = value.IdPairs[index];
            if (expectedId != entry.Id)
            {
                throw new InvalidDataException(
                    $"data2.sav ID pair layout mismatch at pair {index}: raw id {expectedId}, entry id {entry.Id}."
                );
            }

            BinaryPrimitives.WriteInt32LittleEndian(patched.AsSpan(intIndex * 4, 4), entry.Value);
        }

        return patched;
    }

    internal static bool TryDetectIdPairTable(ReadOnlySpan<byte> bytes, out int startInt, out int pairCount)
    {
        var totalInts = bytes.Length / 4;
        var bestStart = -1;
        var bestPairCount = 0;

        for (var candidateStart = 0; candidateStart + 1 < totalInts; candidateStart++)
        {
            var candidateCount = 0;
            var previousId = int.MinValue;
            while (candidateStart + candidateCount * 2 + 1 < totalInts)
            {
                var value = ReadInt32(bytes, candidateStart + candidateCount * 2);
                var id = ReadInt32(bytes, candidateStart + candidateCount * 2 + 1);
                if (value < MinValue || value > MaxValue || id < MinId || id > MaxId || id <= previousId)
                    break;

                previousId = id;
                candidateCount++;
            }

            if (candidateCount <= bestPairCount)
                continue;

            bestStart = candidateStart;
            bestPairCount = candidateCount;
        }

        if (bestPairCount < MinimumPairCount)
        {
            startInt = -1;
            pairCount = 0;
            return false;
        }

        startInt = bestStart;
        pairCount = bestPairCount;
        return true;
    }

    private static int ReadInt32(ReadOnlySpan<byte> bytes, int intIndex)
    {
        var start = intIndex * 4;
        return start >= 0 && start + 4 <= bytes.Length
            ? BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(start, 4))
            : 0;
    }
}
