using System.Text;

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
        // Use stackalloc for short strings; fall back to ArrayPool for long ones.
        if (byteCount <= Core.StackAllocPolicy.MaxStackAllocBytes)
        {
            Span<byte> buf = stackalloc byte[byteCount];
            Encoding.ASCII.GetBytes(Value, buf);
            writer.WriteBytes(buf);
        }
        else
        {
            var buf = System.Buffers.ArrayPool<byte>.Shared.Rent(byteCount);
            try
            {
                Encoding.ASCII.GetBytes(Value, buf);
                writer.WriteBytes(buf.AsSpan(0, byteCount));
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(buf);
            }
        }
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
