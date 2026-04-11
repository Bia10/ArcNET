using System.Buffers.Binary;
using System.Text;
using ArcNET.Core.Primitives;
using ArcNET.GameObjects;
using Bia.ValueBuffers;

namespace ArcNET.Formats;

/// <summary>
/// Static factory for constructing <see cref="ObjectProperty"/> instances from typed values.
/// Use these when creating a new property from scratch; use the <c>With*</c> extension methods
/// on <see cref="ObjectPropertyExtensions"/> to transform an existing property.
/// </summary>
public static class ObjectPropertyFactory
{
    // ── Scalars ───────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a property with <paramref name="value"/> encoded as a little-endian
    /// <see cref="int"/> (4 bytes, <c>OD_TYPE_INT32</c>).
    /// </summary>
    public static ObjectProperty ForInt32(ObjectField field, int value)
    {
        var bytes = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
        return new ObjectProperty { Field = field, RawBytes = bytes };
    }

    /// <summary>
    /// Creates a property with <paramref name="value"/> encoded as a little-endian
    /// <see cref="long"/> (1-byte presence + 8-byte value, <c>OD_TYPE_INT64</c>).
    /// </summary>
    public static ObjectProperty ForInt64(ObjectField field, long value)
    {
        var bytes = new byte[9];
        bytes[0] = 1; // presence
        BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(1), value);
        return new ObjectProperty { Field = field, RawBytes = bytes };
    }

    /// <summary>
    /// Creates a property with <paramref name="value"/> encoded as an IEEE 754
    /// little-endian <see cref="float"/> (4 bytes, <c>OD_TYPE_FLOAT</c>).
    /// </summary>
    public static ObjectProperty ForFloat(ObjectField field, float value)
    {
        var bytes = new byte[4];
        BinaryPrimitives.WriteSingleLittleEndian(bytes, value);
        return new ObjectProperty { Field = field, RawBytes = bytes };
    }

    /// <summary>
    /// Creates a property with <paramref name="value"/> encoded as an ASCII string
    /// (1-byte presence + int32 length + (length+1) bytes including NUL, <c>OD_TYPE_STRING</c>).
    /// </summary>
    public static ObjectProperty ForString(ObjectField field, string value)
    {
        Span<byte> initial = stackalloc byte[256];
        using var buf = new ValueByteBuffer(initial);
        buf.Write(1); // presence
        buf.WriteInt32LittleEndian(value.Length); // ASCII: 1 byte per char
        buf.WriteAsciiEncoded(value.AsSpan());
        buf.Write(0); // NUL terminator
        return new ObjectProperty { Field = field, RawBytes = buf.ToArray() };
    }

    /// <summary>
    /// Creates a location property from tile coordinates.
    /// The value is packed as <c>LOCATION_MAKE(x, y)</c>: lower 32 bits = X, upper 32 bits = Y.
    /// Wire type is <c>OD_TYPE_INT64</c>.
    /// </summary>
    public static ObjectProperty ForLocation(ObjectField field, int tileX, int tileY)
    {
        var packed = (long)(uint)tileX | ((long)(uint)tileY << 32);
        return ForInt64(field, packed);
    }

    // ── SAR arrays ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a property with <paramref name="values"/> encoded as a SAR
    /// <c>int32[]</c> array (<c>OD_TYPE_INT32_ARRAY</c>).
    /// </summary>
    public static ObjectProperty ForInt32Array(ObjectField field, ReadOnlySpan<int> values)
    {
        Span<byte> initial = stackalloc byte[256];
        using var buf = new ValueByteBuffer(initial);
        buf.WriteInt32LittleEndianAll(values);
        return new ObjectProperty
        {
            Field = field,
            RawBytes = SarEncoding.BuildSarBytes(4, values.Length, buf.WrittenSpan),
        };
    }

    /// <summary>
    /// Creates a property with <paramref name="values"/> encoded as a SAR
    /// <c>int64[]</c> array (<c>OD_TYPE_INT64_ARRAY</c>).
    /// </summary>
    public static ObjectProperty ForInt64Array(ObjectField field, ReadOnlySpan<long> values)
    {
        Span<byte> initial = stackalloc byte[256];
        using var buf = new ValueByteBuffer(initial);
        buf.WriteInt64LittleEndianAll(values);
        return new ObjectProperty
        {
            Field = field,
            RawBytes = SarEncoding.BuildSarBytes(8, values.Length, buf.WrittenSpan),
        };
    }

    /// <summary>
    /// Creates a property with <paramref name="ids"/> encoded as a <c>OD_TYPE_HANDLE_ARRAY</c>
    /// SAR block. Each <see cref="Guid"/> is written as a 24-byte <c>ObjectID</c>
    /// with <c>OID_TYPE_GUID = 2</c> in the type field.
    /// </summary>
    public static ObjectProperty ForObjectIdArray(ObjectField field, ReadOnlySpan<Guid> ids)
    {
        const int wireSize = ObjectPropertyExtensions.ObjectIdWireSize;
        Span<byte> initial = stackalloc byte[256];
        using var buf = new ValueByteBuffer(initial);
        for (var i = 0; i < ids.Length; i++)
        {
            buf.WriteInt16LittleEndian(GameObjectGuid.OidTypeGuid);
            buf.WriteInt16LittleEndian(0); // padding_2
            buf.WriteInt32LittleEndian(0); // padding_4
            var guidSlot = buf.GetWritableSpan(16);
            ids[i].TryWriteBytes(guidSlot);
            buf.AdvanceLength(16);
        }
        return new ObjectProperty
        {
            Field = field,
            RawBytes = SarEncoding.BuildSarBytes(wireSize, ids.Length, buf.WrittenSpan),
        };
    }

    /// <summary>
    /// Creates an empty <c>OD_TYPE_HANDLE_ARRAY</c> SAR property (zero elements).
    /// Use this to clear container or critter inventory lists.
    /// </summary>
    public static ObjectProperty ForEmptyObjectIdArray(ObjectField field) => ForObjectIdArray(field, []);
}
