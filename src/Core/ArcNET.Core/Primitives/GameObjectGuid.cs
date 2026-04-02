namespace ArcNET.Core.Primitives;

/// <summary>A composite 16-byte identifier for a game object instance.</summary>
public readonly record struct GameObjectGuid(uint Type, uint Foo0, uint Foo2, uint Guid)
    : IBinarySerializable<GameObjectGuid, SpanReader>,
        ISpanFormattable
{
    /// <summary>Returns <see langword="true"/> if this is a prototype reference (Type == 0xFFFFFFFF).</summary>
    public bool IsProto => Type == 0xFFFFFFFF;

    /// <inheritdoc/>
    public static GameObjectGuid Read(ref SpanReader reader) =>
        new(reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32());

    /// <inheritdoc/>
    public void Write(ref SpanWriter writer)
    {
        writer.WriteUInt32(Type);
        writer.WriteUInt32(Foo0);
        writer.WriteUInt32(Foo2);
        writer.WriteUInt32(Guid);
    }

    /// <inheritdoc/>
    public bool TryFormat(Span<char> dest, out int written, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        dest.TryWrite($"{Type:X8}-{Guid:X8}", out written);

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? provider) => $"{Type:X8}-{Guid:X8}";

    /// <inheritdoc/>
    public override string ToString() => $"{Type:X8}-{Guid:X8}";
}
