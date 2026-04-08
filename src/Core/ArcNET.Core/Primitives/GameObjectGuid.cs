using System.Buffers.Binary;

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
    // OID_TYPE_* constants — match the int16_t values used in Arcanum's binary format.
    /// <summary>OID_TYPE_HANDLE (-2) — placeholder/handle reference.</summary>
    public const short OidTypeHandle = -2;

    /// <summary>OID_TYPE_BLOCKED (-1) — indicates this slot IS a prototype definition.</summary>
    public const short OidTypeBlocked = -1;

    /// <summary>OID_TYPE_NULL (0) — null/empty reference.</summary>
    public const short OidTypeNull = 0;

    /// <summary>OID_TYPE_A (1) — A-type OID; the union encodes a proto index (<c>d.a</c>).</summary>
    public const short OidTypeA = 1;

    /// <summary>OID_TYPE_GUID (2) — instance identified by a GUID (<c>d.g</c>).</summary>
    public const short OidTypeGuid = 2;

    /// <summary>OID_TYPE_P (3) — P-type packed OID (<c>d.p</c>).</summary>
    public const short OidTypeP = 3;

    /// <summary>Returns <see langword="true"/> if this is a prototype definition (OID_TYPE_BLOCKED).</summary>
    public bool IsProto => OidType == OidTypeBlocked;

    /// <summary>
    /// For A-type OIDs (<c>OidType == OidTypeA</c>), returns the embedded proto number stored in
    /// the first 4 bytes of the union (<c>d.a</c> = little-endian int32).  Returns
    /// <see langword="null"/> for all other OID types.
    /// </summary>
    public int? GetProtoNumber()
    {
        if (OidType != OidTypeA)
            return null;
        Span<byte> bytes = stackalloc byte[16];
        Id.TryWriteBytes(bytes);
        return BinaryPrimitives.ReadInt32LittleEndian(bytes);
    }

    /// <summary>
    /// Returns a short human-readable identifier:
    /// <list type="bullet">
    ///   <item><c>OidType == OidTypeHandle</c> → <c>"handle"</c></item>
    ///   <item><c>OidType == OidTypeBlocked</c> → <c>"proto:self"</c></item>
    ///   <item><c>OidType == OidTypeNull</c> → <c>"null"</c></item>
    ///   <item><c>OidType == OidTypeA</c> → <c>"proto#NNNN"</c> (proto number extracted from d.a)</item>
    ///   <item><c>OidType == OidTypeGuid</c> → <c>"mob:{short-guid}"</c></item>
    ///   <item>other → <c>"oid{type}:{guid}"</c></item>
    /// </list>
    /// </summary>
    public string ToLabel()
    {
        return OidType switch
        {
            OidTypeHandle => "handle",
            OidTypeBlocked => "proto:self",
            OidTypeNull => "null",
            OidTypeA => $"proto#{GetProtoNumber()}",
            OidTypeGuid => $"mob:{Id.ToString()[..8]}…",
            _ => $"oid{OidType}:{Id.ToString()[..8]}…",
        };
    }

    /// <inheritdoc/>
    public static GameObjectGuid Read(ref SpanReader reader)
    {
        var oidType = reader.ReadInt16();
        var padding2 = reader.ReadInt16();
        var padding4 = reader.ReadInt32();
        // TigGuid / Windows GUID: Data1(4) + Data2(2) + Data3(2) + Data4(8) in little-endian layout.
        var id = new Guid(reader.ReadBytes(16));
        return new GameObjectGuid(oidType, padding2, padding4, id);
    }

    /// <inheritdoc/>
    public void Write(ref SpanWriter writer)
    {
        writer.WriteInt16(OidType);
        writer.WriteInt16(Padding2);
        writer.WriteInt32(Padding4);
        Span<byte> guidBuf = stackalloc byte[16];
        Id.TryWriteBytes(guidBuf);
        writer.WriteBytes(guidBuf);
    }

    /// <inheritdoc/>
    public bool TryFormat(Span<char> dest, out int written, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        dest.TryWrite($"{ToLabel()} [{Id}]", out written);

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? provider) => $"{ToLabel()} [{Id}]";

    /// <inheritdoc/>
    public override string ToString() => $"{ToLabel()} [{Id}]";
}
