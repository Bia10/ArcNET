using System.Buffers;
using ArcNET.Core;

namespace ArcNET.Formats;

/// <summary>
/// Generic format I/O helpers that implement file and in-memory boilerplate
/// in terms of the core span-based parse and write methods.
/// </summary>
public static class FormatIo
{
    /// <summary>
    /// Parses <typeparamref name="T"/> from <paramref name="memory"/> using <typeparamref name="TFormat"/>.
    /// </summary>
    public static T ParseMemory<TFormat, T>(ReadOnlyMemory<byte> memory)
        where TFormat : IFormatReader<T>
    {
        var reader = new SpanReader(memory.Span);
        return TFormat.Parse(ref reader);
    }

    /// <summary>
    /// Parses <typeparamref name="T"/> from the file at <paramref name="path"/> using <typeparamref name="TFormat"/>.
    /// </summary>
    public static T ParseFile<TFormat, T>(string path)
        where TFormat : IFormatFileReader<T> => ParseMemory<TFormat, T>(File.ReadAllBytes(path));

    /// <summary>
    /// Serializes <paramref name="value"/> to a newly allocated byte array using <typeparamref name="TFormat"/>.
    /// </summary>
    public static byte[] WriteToArray<TFormat, T>(in T value)
        where TFormat : IFormatWriter<T>
    {
        var buf = new ArrayBufferWriter<byte>();
        var writer = new SpanWriter(buf);
        TFormat.Write(in value, ref writer);
        return buf.WrittenSpan.ToArray();
    }

    /// <summary>
    /// Serializes <paramref name="value"/> and writes it to the file at <paramref name="path"/> using <typeparamref name="TFormat"/>.
    /// </summary>
    public static void WriteToFile<TFormat, T>(in T value, string path)
        where TFormat : IFormatFileWriter<T> => File.WriteAllBytes(path, WriteToArray<TFormat, T>(in value));
}
