using System.Text.Unicode;

namespace ArcNET.Core.Primitives;

/// <summary>An opaque 32-bit identifier for an art resource.</summary>
public readonly record struct ArtId(uint Value)
    : IBinarySerializable<ArtId, SpanReader>,
        ISpanFormattable,
        IUtf8SpanFormattable
{
    /// <inheritdoc/>
    public static ArtId Read(ref SpanReader reader) => new(reader.ReadUInt32());

    /// <inheritdoc/>
    public void Write(ref SpanWriter writer) => writer.WriteUInt32(Value);

    /// <inheritdoc/>
    public bool TryFormat(Span<char> dest, out int written, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        dest.TryWrite($"0x{Value:X8}", out written);

    /// <inheritdoc/>
    public bool TryFormat(Span<byte> utf8Dest, out int written, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        Utf8.TryWrite(utf8Dest, $"0x{Value:X8}", out written);

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? provider) => $"0x{Value:X8}";

    /// <inheritdoc/>
    public override string ToString() => $"0x{Value:X8}";
}
