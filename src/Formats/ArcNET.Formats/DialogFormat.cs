using System.Buffers;
using ArcNET.Core;

namespace ArcNET.Formats;

/// <summary>Placeholder for parsed Arcanum dialog conversation-tree (.dlg) data.</summary>
public sealed class DialogData { }

/// <summary>Span-based parser and writer for Arcanum dialog (.dlg) files.</summary>
public sealed class DialogFormat : IFormatReader<DialogData>, IFormatWriter<DialogData>
{
    /// <inheritdoc/>
    public static DialogData Parse(scoped ref SpanReader reader) =>
        throw new NotImplementedException("DLG format not yet reversed.");

    /// <inheritdoc/>
    public static DialogData ParseFile(string path) => ParseMemory(File.ReadAllBytes(path));

    /// <inheritdoc/>
    public static DialogData ParseMemory(ReadOnlyMemory<byte> memory)
    {
        var reader = new SpanReader(memory.Span);
        return Parse(ref reader);
    }

    /// <inheritdoc/>
    public static void Write(in DialogData value, ref SpanWriter writer) =>
        throw new NotImplementedException("DLG format not yet reversed.");

    /// <inheritdoc/>
    public static byte[] WriteToArray(in DialogData value)
    {
        var buf = new ArrayBufferWriter<byte>();
        var writer = new SpanWriter(buf);
        Write(in value, ref writer);
        return buf.WrittenSpan.ToArray();
    }

    /// <inheritdoc/>
    public static void WriteToFile(in DialogData value, string path) =>
        File.WriteAllBytes(path, WriteToArray(in value));
}
