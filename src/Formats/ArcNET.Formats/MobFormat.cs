using System.Buffers;
using ArcNET.Core;

namespace ArcNET.Formats;

/// <summary>Placeholder for parsed Arcanum mobile (.mob) save-state data.</summary>
public sealed class MobData { }

/// <summary>Span-based parser and writer for Arcanum mobile (.mob) files.</summary>
public sealed class MobFormat : IFormatReader<MobData>, IFormatWriter<MobData>
{
    /// <inheritdoc/>
    public static MobData Parse(scoped ref SpanReader reader) =>
        throw new NotImplementedException("MOB format not yet reversed.");

    /// <inheritdoc/>
    public static MobData ParseFile(string path) => ParseMemory(File.ReadAllBytes(path));

    /// <inheritdoc/>
    public static MobData ParseMemory(ReadOnlyMemory<byte> memory)
    {
        var reader = new SpanReader(memory.Span);
        return Parse(ref reader);
    }

    /// <inheritdoc/>
    public static void Write(in MobData value, ref SpanWriter writer) =>
        throw new NotImplementedException("MOB format not yet reversed.");

    /// <inheritdoc/>
    public static byte[] WriteToArray(in MobData value)
    {
        var buf = new ArrayBufferWriter<byte>();
        var writer = new SpanWriter(buf);
        Write(in value, ref writer);
        return buf.WrittenSpan.ToArray();
    }

    /// <inheritdoc/>
    public static void WriteToFile(in MobData value, string path) => File.WriteAllBytes(path, WriteToArray(in value));
}
