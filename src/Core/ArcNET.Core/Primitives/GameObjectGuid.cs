namespace ArcNET.Core.Primitives;

/// <summary>
/// The 24-byte <c>ObjectID</c> struct as laid out in Arcanum save files and mob files.
/// Size on disk: <c>0x18</c> (24 bytes).
/// <code>
/// struct ObjectID {
///   int16_t type;      // OID_TYPE_* : -2=HANDLE, -1=BLOCKED (proto), 0=NULL, 1=A, 2=GUID, 3=P
///   int16_t padding_2;
///   int     padding_4;
///   union { int64_t h; int a; TigGuid g; ObjectID_P p; } d;  // 16 bytes
/// };
/// </code>
/// </summary>
public readonly record struct GameObjectGuid(short OidType, short Padding2, int Padding4, Guid Id)
    : IBinarySerializable<GameObjectGuid, SpanReader>,
        ISpanFormattable
{
    // OID_TYPE_BLOCKED = (int16_t)(-1) — indicates this slot IS a prototype definition.
    private const short OidTypeBlocked = -1;

    /// <summary>Returns <see langword="true"/> if this is a prototype definition (OID_TYPE_BLOCKED).</summary>
    public bool IsProto => OidType == OidTypeBlocked;

    /// <inheritdoc/>
    public static GameObjectGuid Read(ref SpanReader reader)
    {
        var oidType = reader.ReadInt16();
        var padding2 = reader.ReadInt16();
        var padding4 = reader.ReadInt32();
        // TigGuid / Windows GUID: Data1(4) + Data2(2) + Data3(2) + Data4(8) in little-endian layout.
        var guidBytes = reader.ReadBytes(16).ToArray();
        var id = new Guid(guidBytes);
        return new GameObjectGuid(oidType, padding2, padding4, id);
    }

    /// <inheritdoc/>
    public void Write(ref SpanWriter writer)
    {
        writer.WriteInt16(OidType);
        writer.WriteInt16(Padding2);
        writer.WriteInt32(Padding4);
        writer.WriteBytes(Id.ToByteArray());
    }

    /// <inheritdoc/>
    public bool TryFormat(Span<char> dest, out int written, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        dest.TryWrite($"OID({OidType}):{Id}", out written);

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? provider) => $"OID({OidType}):{Id}";

    /// <inheritdoc/>
    public override string ToString() => $"OID({OidType}):{Id}";
}
