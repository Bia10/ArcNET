using System.Buffers;
using ArcNET.Core;

namespace ArcNET.Formats;

/// <summary>Placeholder for parsed Arcanum compiled script (.scr) data.</summary>
public sealed class ScriptData { }

/// <summary>Span-based parser and writer for Arcanum compiled script (.scr) files.</summary>
public sealed class ScriptFormat : IFormatReader<ScriptData>, IFormatWriter<ScriptData>
{
    /// <inheritdoc/>
    public static ScriptData Parse(scoped ref SpanReader reader) =>
        throw new NotImplementedException("SCR format not yet reversed.");

    /// <inheritdoc/>
    public static ScriptData ParseFile(string path) => ParseMemory(File.ReadAllBytes(path));

    /// <inheritdoc/>
    public static ScriptData ParseMemory(ReadOnlyMemory<byte> memory)
    {
        var reader = new SpanReader(memory.Span);
        return Parse(ref reader);
    }

    /// <inheritdoc/>
    public static void Write(in ScriptData value, ref SpanWriter writer) =>
        throw new NotImplementedException("SCR format not yet reversed.");

    /// <inheritdoc/>
    public static byte[] WriteToArray(in ScriptData value)
    {
        var buf = new ArrayBufferWriter<byte>();
        var writer = new SpanWriter(buf);
        Write(in value, ref writer);
        return buf.WrittenSpan.ToArray();
    }

    /// <inheritdoc/>
    public static void WriteToFile(in ScriptData value, string path) =>
        File.WriteAllBytes(path, WriteToArray(in value));
}
