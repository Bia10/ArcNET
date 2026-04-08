using System.Buffers;
using ArcNET.Core;
using ArcNET.GameObjects;

namespace ArcNET.Formats;

/// <summary>
/// Parsed contents of an Arcanum object prototype (.pro) file.
/// File path pattern: <c>data/proto/&lt;type&gt;/*.pro</c>.
/// </summary>
public sealed class ProtoData
{
    /// <summary>OFF file header (version + prototype ID + object ID + type + bitmap).</summary>
    public required GameObjectHeader Header { get; init; }

    /// <summary>
    /// Property values present in the bitmap, in bit-index order.
    /// Each property's raw bytes include the full wire representation (including SAR headers).
    /// For prototypes all bitmap bits are considered present.
    /// </summary>
    public required IReadOnlyList<ObjectProperty> Properties { get; init; }
}

/// <summary>
/// Span-based parser and writer for Arcanum object prototype (.pro) files.
/// The PRO format is structurally identical to MOB; the differences are:
/// <list type="bullet">
///   <item><see cref="GameObjectHeader.IsPrototype"/> is <see langword="true"/>.</item>
///   <item>The <c>PropCollectionItems</c> int16 is absent from the header.</item>
///   <item>All bitmap bits are treated as present — prototypes define every field.</item>
/// </list>
/// </summary>
public sealed class ProtoFormat : IFormatFileReader<ProtoData>, IFormatFileWriter<ProtoData>
{
    /// <inheritdoc/>
    public static ProtoData Parse(scoped ref SpanReader reader)
    {
        var header = GameObjectHeader.Read(ref reader);
        var properties = ObjectPropertyIo.ReadProperties(ref reader, header);

        return new ProtoData { Header = header, Properties = properties };
    }

    /// <inheritdoc/>
    public static ProtoData ParseMemory(ReadOnlyMemory<byte> memory)
    {
        var reader = new SpanReader(memory.Span);
        return Parse(ref reader);
    }

    /// <inheritdoc/>
    public static ProtoData ParseFile(string path) => ParseMemory(File.ReadAllBytes(path));

    /// <inheritdoc/>
    public static void Write(in ProtoData value, ref SpanWriter writer)
    {
        value.Header.Write(ref writer);
        ObjectPropertyIo.WriteProperties(value.Properties, ref writer);
    }

    /// <inheritdoc/>
    public static byte[] WriteToArray(in ProtoData value)
    {
        var buf = new ArrayBufferWriter<byte>();
        var writer = new SpanWriter(buf);
        Write(in value, ref writer);
        return buf.WrittenSpan.ToArray();
    }

    /// <inheritdoc/>
    public static void WriteToFile(in ProtoData value, string path) => File.WriteAllBytes(path, WriteToArray(in value));
}
