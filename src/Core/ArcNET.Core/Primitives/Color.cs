namespace ArcNET.Core.Primitives;

/// <summary>An RGB color triplet stored as bytes.</summary>
public readonly record struct Color(byte R, byte G, byte B) : IBinarySerializable<Color, SpanReader>, ISpanFormattable
{
    /// <summary>Reads an RGBA color where the 4th byte (alpha) is discarded.</summary>
    public static Color ReadRgba(ref SpanReader reader)
    {
        var b = reader.ReadByte();
        var g = reader.ReadByte();
        var r = reader.ReadByte();
        _ = reader.ReadByte(); // alpha — unused
        return new Color(r, g, b);
    }

    /// <inheritdoc/>
    public static Color Read(ref SpanReader reader) => new(reader.ReadByte(), reader.ReadByte(), reader.ReadByte());

    /// <inheritdoc/>
    public void Write(ref SpanWriter writer)
    {
        writer.WriteByte(R);
        writer.WriteByte(G);
        writer.WriteByte(B);
    }

    /// <inheritdoc/>
    public bool TryFormat(Span<char> dest, out int written, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        dest.TryWrite($"#{R:X2}{G:X2}{B:X2}", out written);

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? provider) => $"#{R:X2}{G:X2}{B:X2}";

    /// <inheritdoc/>
    public override string ToString() => $"#{R:X2}{G:X2}{B:X2}";
}
