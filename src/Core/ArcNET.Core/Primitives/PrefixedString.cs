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
        var bytes = Encoding.ASCII.GetBytes(Value);
        writer.WriteUInt16((ushort)bytes.Length);
        writer.WriteBytes(bytes);
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
