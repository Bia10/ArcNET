using System.Buffers.Binary;
using System.Text;
using ArcNET.GameObjects;

namespace ArcNET.Formats;

/// <summary>
/// Static factory for constructing <see cref="ObjectProperty"/> instances from typed values.
/// Use these when creating a new property from scratch; use the <c>With*</c> extension methods
/// on <see cref="ObjectPropertyExtensions"/> to transform an existing property.
/// </summary>
public static class ObjectPropertyFactory
{
    // ── SAR helpers (duplicated from ObjectPropertyExtensions to stay in this file) ──────

    private static byte[] BuildSarBytes(int elementSize, int elementCount, ReadOnlySpan<byte> elements)
    {
        var bitsetCnt = (uint)((elementCount + 31) / 32);
        var totalSize = 1 + 12 + elements.Length + 4 + (int)(bitsetCnt * 4);
        var bytes = new byte[totalSize];
        bytes[0] = 1; // presence
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(1), (uint)elementSize);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(5), (uint)elementCount);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(9), 0); // sa.bitset_id
        elements.CopyTo(bytes.AsSpan(13));
        var postOffset = 13 + elements.Length;
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(postOffset), bitsetCnt);
        for (var i = 0; i < (int)bitsetCnt; i++)
        {
            // Last (possibly partial) word: only bits 0..(elementCount%32 - 1) set.
            // All preceding words are fully occupied (0xFFFFFFFF).
            uint word;
            if (i < (int)bitsetCnt - 1)
                word = 0xFFFFFFFF;
            else
            {
                var rem = elementCount % 32;
                word = rem == 0 ? 0xFFFFFFFF : (1u << rem) - 1u;
            }
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(postOffset + 4 + i * 4), word);
        }
        return bytes;
    }

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
        var strBytes = Encoding.ASCII.GetBytes(value);
        var bytes = new byte[1 + 4 + strBytes.Length + 1];
        bytes[0] = 1; // presence
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(1), strBytes.Length);
        strBytes.CopyTo(bytes, 5);
        bytes[5 + strBytes.Length] = 0; // NUL terminator
        return new ObjectProperty { Field = field, RawBytes = bytes };
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
        var elements = new byte[values.Length * 4];
        for (var i = 0; i < values.Length; i++)
            BinaryPrimitives.WriteInt32LittleEndian(elements.AsSpan(i * 4), values[i]);
        return new ObjectProperty { Field = field, RawBytes = BuildSarBytes(4, values.Length, elements) };
    }

    /// <summary>
    /// Creates a property with <paramref name="values"/> encoded as a SAR
    /// <c>int64[]</c> array (<c>OD_TYPE_INT64_ARRAY</c>).
    /// </summary>
    public static ObjectProperty ForInt64Array(ObjectField field, ReadOnlySpan<long> values)
    {
        var elements = new byte[values.Length * 8];
        for (var i = 0; i < values.Length; i++)
            BinaryPrimitives.WriteInt64LittleEndian(elements.AsSpan(i * 8), values[i]);
        return new ObjectProperty { Field = field, RawBytes = BuildSarBytes(8, values.Length, elements) };
    }

    /// <summary>
    /// Creates a property with <paramref name="ids"/> encoded as a <c>OD_TYPE_HANDLE_ARRAY</c>
    /// SAR block. Each <see cref="Guid"/> is written as a 24-byte <c>ObjectID</c>
    /// with <c>OID_TYPE_GUID = 2</c> in the type field.
    /// </summary>
    public static ObjectProperty ForObjectIdArray(ObjectField field, ReadOnlySpan<Guid> ids)
    {
        const int wireSize = ObjectPropertyExtensions.ObjectIdWireSize;
        const short oidTypeGuid = 2;
        var elements = new byte[ids.Length * wireSize];
        for (var i = 0; i < ids.Length; i++)
        {
            var o = i * wireSize;
            BinaryPrimitives.WriteInt16LittleEndian(elements.AsSpan(o), oidTypeGuid);
            BinaryPrimitives.WriteInt16LittleEndian(elements.AsSpan(o + 2), 0);
            BinaryPrimitives.WriteInt32LittleEndian(elements.AsSpan(o + 4), 0);
            ids[i].ToByteArray().CopyTo(elements, o + 8);
        }
        return new ObjectProperty { Field = field, RawBytes = BuildSarBytes(wireSize, ids.Length, elements) };
    }

    /// <summary>
    /// Creates an empty <c>OD_TYPE_HANDLE_ARRAY</c> SAR property (zero elements).
    /// Use this to clear container or critter inventory lists.
    /// </summary>
    public static ObjectProperty ForEmptyObjectIdArray(ObjectField field) => ForObjectIdArray(field, []);
}
