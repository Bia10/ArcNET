using ArcNET.Core;

namespace ArcNET.Formats;

/// <summary>
/// Span-based parser and writer for the currently verified structural surface of <c>data.sav</c>.
/// The implementation is intentionally conservative: it validates the 8-byte header exists,
/// exposes the aligned INT32[4] row framing through <see cref="DataSavFile"/>, and preserves
/// every byte verbatim on write.
/// </summary>
public sealed class DataSavFormat : IFormatFileReader<DataSavFile>, IFormatFileWriter<DataSavFile>
{
    private const int MinimumByteCount = 8;

    /// <inheritdoc/>
    public static DataSavFile Parse(scoped ref SpanReader reader)
    {
        var rawBytes = reader.ReadBytes(reader.Remaining).ToArray();
        return ParseBytes(rawBytes);
    }

    /// <inheritdoc/>
    public static DataSavFile ParseMemory(ReadOnlyMemory<byte> memory) =>
        FormatIo.ParseMemory<DataSavFormat, DataSavFile>(memory);

    /// <inheritdoc/>
    public static DataSavFile ParseFile(string path) => FormatIo.ParseFile<DataSavFormat, DataSavFile>(path);

    /// <inheritdoc/>
    public static void Write(in DataSavFile value, ref SpanWriter writer)
    {
        Validate(value.RawBytes);
        writer.WriteBytes(value.RawBytes);
    }

    /// <inheritdoc/>
    public static byte[] WriteToArray(in DataSavFile value) =>
        FormatIo.WriteToArray<DataSavFormat, DataSavFile>(in value);

    /// <inheritdoc/>
    public static void WriteToFile(in DataSavFile value, string path) =>
        FormatIo.WriteToFile<DataSavFormat, DataSavFile>(in value, path);

    private static DataSavFile ParseBytes(byte[] rawBytes)
    {
        Validate(rawBytes);
        return new DataSavFile { RawBytes = rawBytes };
    }

    private static void Validate(byte[] rawBytes)
    {
        ArgumentNullException.ThrowIfNull(rawBytes);
        if (rawBytes.Length < MinimumByteCount)
            throw new InvalidDataException("data.sav must contain at least the verified 8-byte header.");
    }
}
