using System.Buffers;
using ArcNET.Core;
using ArcNET.GameObjects;

namespace ArcNET.Formats;

/// <summary>
/// Parsed contents of an Arcanum mobile (.mob) save-state file.
/// File path pattern: <c>modules/&lt;mod&gt;/maps/&lt;map&gt;/mobile/G_*.mob</c>.
/// Source: <c>arcanum-ce/src/game/obj.c</c>, <c>obj_file.c</c>, <c>obj.h</c>.
/// </summary>
public sealed class MobData
{
    /// <summary>OFF file header (version + prototype ID + object ID + type + bitmap).</summary>
    public required GameObjectHeader Header { get; init; }

    /// <summary>
    /// Property values present in the bitmap, in bit-index order.
    /// Each property's raw bytes include the full wire representation (including SAR headers).
    /// </summary>
    public required IReadOnlyList<ObjectProperty> Properties { get; init; }
}

/// <summary>
/// Span-based parser and writer for Arcanum mobile (.mob) files.
/// The MOB format begins with a <see cref="GameObjectHeader"/> (OFF header) followed by
/// a sequential property collection — one value per set bit in the header bitmap.
/// </summary>
public sealed class MobFormat : IFormatReader<MobData>, IFormatWriter<MobData>
{
    /// <inheritdoc/>
    public static MobData Parse(scoped ref SpanReader reader)
    {
        var header = GameObjectHeader.Read(ref reader);
        var properties = ObjectPropertyIo.ReadProperties(ref reader, header);

        return new MobData { Header = header, Properties = properties };
    }

    /// <inheritdoc/>
    public static MobData ParseMemory(ReadOnlyMemory<byte> memory)
    {
        var reader = new SpanReader(memory.Span);
        return Parse(ref reader);
    }

    /// <inheritdoc/>
    public static MobData ParseFile(string path) => ParseMemory(File.ReadAllBytes(path));

    /// <inheritdoc/>
    public static void Write(in MobData value, ref SpanWriter writer)
    {
        value.Header.Write(ref writer);
        ObjectPropertyIo.WriteProperties(value.Properties, ref writer);
    }

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
