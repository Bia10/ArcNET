namespace ArcNET.Core.Primitives;

/// <summary>A 2D integer coordinate pair used for map tile positions.</summary>
public readonly record struct Location(short X, short Y) : IBinarySerializable<Location, SpanReader>, ISpanFormattable
{
    /// <inheritdoc/>
    public static Location Read(ref SpanReader reader) => new(reader.ReadInt16(), reader.ReadInt16());

    /// <inheritdoc/>
    public void Write(ref SpanWriter writer)
    {
        writer.WriteInt16(X);
        writer.WriteInt16(Y);
    }

    /// <inheritdoc/>
    public bool TryFormat(Span<char> dest, out int written, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        dest.TryWrite($"({X}, {Y})", out written);

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? provider) => $"({X}, {Y})";

    /// <inheritdoc/>
    public override string ToString() => $"({X}, {Y})";
}
