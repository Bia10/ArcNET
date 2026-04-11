using System.Text;
using Bia.ValueBuffers;

namespace ArcNET.Core.Primitives;

/// <summary>A length-prefixed ASCII string read from binary game data.</summary>
public readonly record struct PrefixedString(string Value) : IBinarySerializable<PrefixedString, SpanReader>
{
    /// <inheritdoc/>
    public static PrefixedString Read(ref SpanReader reader)
    {
        var length = reader.ReadUInt16();
        if (length == 0)
            return new PrefixedString(string.Empty);

        var bytes = reader.ReadBytes(length);
        return new PrefixedString(Encoding.ASCII.GetString(bytes));
    }

    /// <inheritdoc/>
    public void Write(ref SpanWriter writer)
    {
        var byteCount = Encoding.ASCII.GetByteCount(Value);
        writer.WriteUInt16((ushort)byteCount);
        Span<byte> initial = stackalloc byte[Core.StackAllocPolicy.MaxStackAllocBytes];
        using var buf = new ValueByteBuffer(initial);
        buf.WriteEncoded(Value.AsSpan(), Encoding.ASCII);
        writer.WriteBytes(buf.WrittenSpan);
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
