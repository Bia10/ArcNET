using System.Buffers.Binary;
using System.Text;
using ArcNET.Core.Primitives;

namespace ArcNET.Formats;

/// <summary>
/// Typed read/write accessors for <see cref="ObjectProperty.RawBytes"/>.
/// The wire representation stored in <see cref="ObjectProperty.RawBytes"/> is always
/// little-endian and matches the <c>OD_TYPE_*</c> layout — including SAR headers for array fields.
/// </summary>
public static class ObjectPropertyExtensions
{
    // ── SAR header helpers ────────────────────────────────────────────────────
    // SAR (Sizeable Array) wire layout in RawBytes:
    //   byte    presence      offset 0    (0 = absent, non-zero = SA data follows)
    //   int32   sa.size       offset 1    (element size in bytes)
    //   int32   sa.count      offset 5    (number of elements)
    //   int32   sa.bitset_id  offset 9    (in-memory bitset ID, preserved for round-trip)
    //   byte[]  data          offset 13   (sa.size × sa.count bytes)
    //   int32   bitset_cnt    after data  (number of bitset words)
    //   int32[] bitset_data   bitset_cnt × 4 bytes

    /// <summary>Byte offset where SAR element data begins (after 1-byte presence + 12-byte SA header).</summary>
    private const int SarDataOffset = 13;

    // Returns elementSize, elementCount, and dataOffset into rawBytes.
    private static (int ElementSize, int ElementCount, int DataOffset) ParseSarHeader(byte[] rawBytes)
    {
        if (rawBytes.Length < SarDataOffset)
            throw new InvalidOperationException(
                $"SAR raw bytes too short: need at least {SarDataOffset} header bytes, got {rawBytes.Length}."
            );
        // presence at [0]; SA header { size, count, bitset_id } at [1..12]
        var elementSize = (int)BinaryPrimitives.ReadUInt32LittleEndian(rawBytes.AsSpan(1));
        var elementCount = (int)BinaryPrimitives.ReadUInt32LittleEndian(rawBytes.AsSpan(5));
        return (elementSize, elementCount, SarDataOffset);
    }

    // ── Scalar readers ────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the property value as a little-endian <see cref="int"/>.
    /// Only valid for <c>OD_TYPE_INT32</c> fields (4 bytes).
    /// </summary>
    public static int GetInt32(this ObjectProperty property)
    {
        if (property.RawBytes.Length != 4)
            throw new InvalidOperationException(
                $"Field {property.Field} has {property.RawBytes.Length} bytes; expected 4 for Int32."
            );
        return BinaryPrimitives.ReadInt32LittleEndian(property.RawBytes);
    }

    /// <summary>
    /// Returns the property value as a little-endian <see cref="long"/>.
    /// Valid for <c>OD_TYPE_INT64</c> fields (1-byte presence + 8-byte value).
    /// </summary>
    public static long GetInt64(this ObjectProperty property)
    {
        if (property.RawBytes.Length == 1 && property.RawBytes[0] == 0)
            return 0; // absent field

        if (property.RawBytes.Length != 9)
            throw new InvalidOperationException(
                $"Field {property.Field} has {property.RawBytes.Length} bytes; expected 9 for Int64 (1 presence + 8 value)."
            );
        return BinaryPrimitives.ReadInt64LittleEndian(property.RawBytes.AsSpan(1));
    }

    /// <summary>
    /// Returns the property value as an IEEE 754 <see cref="float"/>.
    /// Only valid for <c>OD_TYPE_FLOAT</c> fields (4 bytes).
    /// </summary>
    public static float GetFloat(this ObjectProperty property)
    {
        if (property.RawBytes.Length != 4)
            throw new InvalidOperationException(
                $"Field {property.Field} has {property.RawBytes.Length} bytes; expected 4 for Float."
            );
        return BinaryPrimitives.ReadSingleLittleEndian(property.RawBytes);
    }

    /// <summary>
    /// Returns the property value as an ASCII string.
    /// Only valid for <c>OD_TYPE_STRING</c> fields (1-byte presence + int32 length + (length+1) bytes).
    /// </summary>
    public static string GetString(this ObjectProperty property)
    {
        if (property.RawBytes.Length == 1 && property.RawBytes[0] == 0)
            return string.Empty; // absent field

        if (property.RawBytes.Length < 5)
            throw new InvalidOperationException(
                $"Field {property.Field} raw bytes too short to contain a string (presence + length prefix)."
            );
        // [0] = presence, [1..4] = length, [5..5+length] = string + NUL
        var length = BinaryPrimitives.ReadInt32LittleEndian(property.RawBytes.AsSpan(1));
        if (length <= 0)
            return string.Empty;
        if (property.RawBytes.Length < 5 + length)
            throw new InvalidOperationException(
                $"Field {property.Field}: declared string length {length} exceeds available bytes."
            );
        return Encoding.ASCII.GetString(property.RawBytes, 5, length);
    }

    /// <summary>
    /// Returns the tile X and Y coordinates for an <c>OD_TYPE_INT64</c> location field.
    /// Uses the Arcanum LOCATION_MAKE encoding: lower 32 bits = X tile, upper 32 bits = Y tile.
    /// </summary>
    public static (int X, int Y) GetLocation(this ObjectProperty property)
    {
        var packed = property.GetInt64();
        return ((int)(packed & 0xFFFFFFFF), (int)((packed >> 32) & 0xFFFFFFFF));
    }

    // ── Scalar writers ────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a new <see cref="ObjectProperty"/> with <paramref name="value"/> encoded as
    /// a little-endian <see cref="int"/> (4 bytes).
    /// </summary>
    public static ObjectProperty WithInt32(this ObjectProperty property, int value)
    {
        var bytes = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
        return new ObjectProperty { Field = property.Field, RawBytes = bytes };
    }

    /// <summary>
    /// Returns a new <see cref="ObjectProperty"/> with <paramref name="value"/> encoded as
    /// a little-endian <see cref="long"/> (1-byte presence + 8-byte value).
    /// </summary>
    public static ObjectProperty WithInt64(this ObjectProperty property, long value)
    {
        var bytes = new byte[9];
        bytes[0] = 1; // presence
        BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(1), value);
        return new ObjectProperty { Field = property.Field, RawBytes = bytes };
    }

    /// <summary>
    /// Returns a new <see cref="ObjectProperty"/> with <paramref name="value"/> encoded as
    /// an IEEE 754 little-endian <see cref="float"/> (4 bytes).
    /// </summary>
    public static ObjectProperty WithFloat(this ObjectProperty property, float value)
    {
        var bytes = new byte[4];
        BinaryPrimitives.WriteSingleLittleEndian(bytes, value);
        return new ObjectProperty { Field = property.Field, RawBytes = bytes };
    }

    /// <summary>
    /// Returns a new <see cref="ObjectProperty"/> with <paramref name="value"/> encoded as
    /// an ASCII string (1-byte presence + int32 length + (length+1) bytes including NUL).
    /// </summary>
    public static ObjectProperty WithString(this ObjectProperty property, string value)
    {
        var strBytes = Encoding.ASCII.GetBytes(value);
        // presence(1) + length(4) + string + NUL(1)
        var bytes = new byte[1 + 4 + strBytes.Length + 1];
        bytes[0] = 1; // presence
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(1), strBytes.Length);
        strBytes.CopyTo(bytes, 5);
        bytes[5 + strBytes.Length] = 0; // NUL terminator
        return new ObjectProperty { Field = property.Field, RawBytes = bytes };
    }

    /// <summary>
    /// Returns a new <see cref="ObjectProperty"/> with the tile coordinates packed into an
    /// Arcanum location int64 (lower 32 bits = X, upper 32 bits = Y).
    /// </summary>
    public static ObjectProperty WithLocation(this ObjectProperty property, int x, int y)
    {
        var packed = (long)(uint)x | ((long)(uint)y << 32);
        return property.WithInt64(packed);
    }

    // ── SAR array readers ─────────────────────────────────────────────────────

    /// <summary>
    /// Decodes the property as a SAR <c>int32[]</c> array (<c>OD_TYPE_INT32_ARRAY</c>).
    /// Returns the element values without the SAR header or post-bitmask.
    /// </summary>
    public static int[] GetInt32Array(this ObjectProperty property)
    {
        var (elementSize, elementCount, dataOffset) = ParseSarHeader(property.RawBytes);
        if (elementSize != 4)
            throw new InvalidOperationException(
                $"Field {property.Field}: expected elementSize=4 for Int32Array, got {elementSize}."
            );
        var result = new int[elementCount];
        for (var i = 0; i < elementCount; i++)
            result[i] = BinaryPrimitives.ReadInt32LittleEndian(property.RawBytes.AsSpan(dataOffset + i * 4));
        return result;
    }

    /// <summary>
    /// Decodes the property as a SAR <c>uint32[]</c> array (<c>OD_TYPE_UINT32_ARRAY</c>).
    /// Returns the element values without the SAR header or post-bitmask.
    /// </summary>
    public static uint[] GetUInt32Array(this ObjectProperty property)
    {
        var (elementSize, elementCount, dataOffset) = ParseSarHeader(property.RawBytes);
        if (elementSize != 4)
            throw new InvalidOperationException(
                $"Field {property.Field}: expected elementSize=4 for UInt32Array, got {elementSize}."
            );
        var result = new uint[elementCount];
        for (var i = 0; i < elementCount; i++)
            result[i] = BinaryPrimitives.ReadUInt32LittleEndian(property.RawBytes.AsSpan(dataOffset + i * 4));
        return result;
    }

    /// <summary>
    /// Decodes the property as a SAR <c>int64[]</c> (location or handle) array
    /// (<c>OD_TYPE_INT64_ARRAY</c> or <c>OD_TYPE_HANDLE_ARRAY</c>).
    /// Returns the element values without the SAR header or post-bitmask.
    /// </summary>
    public static long[] GetInt64Array(this ObjectProperty property)
    {
        var (elementSize, elementCount, dataOffset) = ParseSarHeader(property.RawBytes);
        if (elementSize != 8)
            throw new InvalidOperationException(
                $"Field {property.Field}: expected elementSize=8 for Int64/HandleArray, got {elementSize}."
            );
        var result = new long[elementCount];
        for (var i = 0; i < elementCount; i++)
            result[i] = BinaryPrimitives.ReadInt64LittleEndian(property.RawBytes.AsSpan(dataOffset + i * 8));
        return result;
    }

    /// <summary>
    /// Decodes the property as a SAR Script array (<c>OD_TYPE_SCRIPT_ARRAY</c>).
    /// Each element is 12 bytes: <c>uint32 flags + uint32 counters + int32 scriptId</c>.
    /// </summary>
    public static ObjectPropertyScript[] GetScriptArray(this ObjectProperty property)
    {
        var (elementSize, elementCount, dataOffset) = ParseSarHeader(property.RawBytes);
        if (elementSize != 12)
            throw new InvalidOperationException(
                $"Field {property.Field}: expected elementSize=12 for ScriptArray, got {elementSize}."
            );
        var result = new ObjectPropertyScript[elementCount];
        for (var i = 0; i < elementCount; i++)
        {
            var o = dataOffset + i * 12;
            result[i] = new ObjectPropertyScript(
                BinaryPrimitives.ReadUInt32LittleEndian(property.RawBytes.AsSpan(o)),
                BinaryPrimitives.ReadUInt32LittleEndian(property.RawBytes.AsSpan(o + 4)),
                BinaryPrimitives.ReadInt32LittleEndian(property.RawBytes.AsSpan(o + 8))
            );
        }
        return result;
    }

    // ── SAR array writers ─────────────────────────────────────────────────────

    /// <summary>
    /// Returns a new <see cref="ObjectProperty"/> with <paramref name="values"/> encoded as
    /// a SAR <c>int32[]</c> array (4-byte elements, full SAR header + post-bitmask).
    /// </summary>
    public static ObjectProperty WithInt32Array(this ObjectProperty property, ReadOnlySpan<int> values)
    {
        var elements = new byte[values.Length * 4];
        for (var i = 0; i < values.Length; i++)
            BinaryPrimitives.WriteInt32LittleEndian(elements.AsSpan(i * 4), values[i]);
        return new ObjectProperty
        {
            Field = property.Field,
            RawBytes = SarEncoding.BuildSarBytes(4, values.Length, elements),
        };
    }

    /// <summary>
    /// Returns a new <see cref="ObjectProperty"/> with <paramref name="values"/> encoded as
    /// a SAR <c>uint32[]</c> array (4-byte elements, full SAR header + post-bitmask).
    /// </summary>
    public static ObjectProperty WithUInt32Array(this ObjectProperty property, ReadOnlySpan<uint> values)
    {
        var elements = new byte[values.Length * 4];
        for (var i = 0; i < values.Length; i++)
            BinaryPrimitives.WriteUInt32LittleEndian(elements.AsSpan(i * 4), values[i]);
        return new ObjectProperty
        {
            Field = property.Field,
            RawBytes = SarEncoding.BuildSarBytes(4, values.Length, elements),
        };
    }

    /// <summary>
    /// Returns a new <see cref="ObjectProperty"/> with <paramref name="values"/> encoded as
    /// a SAR <c>int64[]</c> (location or handle) array (8-byte elements, full SAR header + post-bitmask).
    /// </summary>
    public static ObjectProperty WithInt64Array(this ObjectProperty property, ReadOnlySpan<long> values)
    {
        var elements = new byte[values.Length * 8];
        for (var i = 0; i < values.Length; i++)
            BinaryPrimitives.WriteInt64LittleEndian(elements.AsSpan(i * 8), values[i]);
        return new ObjectProperty
        {
            Field = property.Field,
            RawBytes = SarEncoding.BuildSarBytes(8, values.Length, elements),
        };
    }

    /// <summary>
    /// Returns a new <see cref="ObjectProperty"/> with <paramref name="scripts"/> encoded as
    /// a SAR Script array (12-byte elements, full SAR header + post-bitmask).
    /// </summary>
    public static ObjectProperty WithScriptArray(
        this ObjectProperty property,
        ReadOnlySpan<ObjectPropertyScript> scripts
    )
    {
        var elements = new byte[scripts.Length * 12];
        for (var i = 0; i < scripts.Length; i++)
        {
            var o = i * 12;
            BinaryPrimitives.WriteUInt32LittleEndian(elements.AsSpan(o), scripts[i].Flags);
            BinaryPrimitives.WriteUInt32LittleEndian(elements.AsSpan(o + 4), scripts[i].Counters);
            BinaryPrimitives.WriteInt32LittleEndian(elements.AsSpan(o + 8), scripts[i].ScriptId);
        }
        return new ObjectProperty
        {
            Field = property.Field,
            RawBytes = SarEncoding.BuildSarBytes(12, scripts.Length, elements),
        };
    }

    // ── ObjectID (Handle) array readers / writers ─────────────────────────────
    // OD_TYPE_HANDLE_ARRAY fields (e.g. ContainerInventoryListIdx, CritterInventoryListIdx)
    // store a SAR block where each element is a 24-byte ObjectID:
    //   int16_t  type      (OID_TYPE_* : -1=BLOCKED, 0=NULL, 1=A, 2=GUID, 3=P)
    //   int16_t  padding_2
    //   int32    padding_4
    //   byte[16] TigGuid   (Windows GUID in little-endian layout)

    /// <summary>ObjectID size on disk — <c>sizeof(ObjectID) == 0x18</c>.</summary>
    public const int ObjectIdWireSize = 24;

    /// <summary>
    /// Decodes a <c>OD_TYPE_HANDLE_ARRAY</c> SAR property into an array of <see cref="Guid"/> values.
    /// Each element is a 24-byte <c>ObjectID</c>; only the 16-byte GUID portion (bytes 8–23) is returned.
    /// </summary>
    public static Guid[] GetObjectIdArray(this ObjectProperty property)
    {
        var (elementSize, elementCount, dataOffset) = ParseSarHeader(property.RawBytes);
        if (elementSize != ObjectIdWireSize)
            throw new InvalidOperationException(
                $"Field {property.Field}: expected elementSize={ObjectIdWireSize} for HandleArray (ObjectID), got {elementSize}."
            );
        var result = new Guid[elementCount];
        for (var i = 0; i < elementCount; i++)
        {
            var offset = dataOffset + i * ObjectIdWireSize;
            // Skip the 8-byte header (type + padding_2 + padding_4); read the 16-byte GUID.
            result[i] = new Guid(property.RawBytes.AsSpan(offset + 8, 16));
        }
        return result;
    }

    /// <summary>
    /// Decodes a <c>OD_TYPE_HANDLE_ARRAY</c> SAR property into full ObjectID tuples:
    /// (OidType, ProtoOrData1, GUID).  <c>ProtoOrData1</c> is meaningful as a prototype index
    /// when <c>OidType == 1</c> (OID_TYPE_A); for GUID-type items it is the first 4 bytes of the TigGuid.
    /// </summary>
    public static (short OidType, int ProtoOrData1, Guid Id)[] GetObjectIdArrayFull(this ObjectProperty property)
    {
        var (elementSize, elementCount, dataOffset) = ParseSarHeader(property.RawBytes);
        if (elementSize != ObjectIdWireSize)
            throw new InvalidOperationException(
                $"Field {property.Field}: expected elementSize={ObjectIdWireSize} for HandleArray (ObjectID), got {elementSize}."
            );
        var result = new (short OidType, int ProtoOrData1, Guid Id)[elementCount];
        for (var i = 0; i < elementCount; i++)
        {
            var offset = dataOffset + i * ObjectIdWireSize;
            var oidType = BinaryPrimitives.ReadInt16LittleEndian(property.RawBytes.AsSpan(offset));
            // d.a is the first int of the union at bytes 8-11; it is the proto index for OID_TYPE_A.
            var protoOrData1 = BinaryPrimitives.ReadInt32LittleEndian(property.RawBytes.AsSpan(offset + 8));
            var guid = new Guid(property.RawBytes.AsSpan(offset + 8, 16));
            result[i] = (oidType, protoOrData1, guid);
        }
        return result;
    }

    /// <summary>
    /// Returns a new <see cref="ObjectProperty"/> encoding <paramref name="ids"/> as a
    /// <c>OD_TYPE_HANDLE_ARRAY</c> SAR block. Each <see cref="Guid"/> is written as a full
    /// 24-byte <c>ObjectID</c> with <c>OID_TYPE_GUID = 2</c> in the type field.
    /// </summary>
    public static ObjectProperty WithObjectIdArray(this ObjectProperty property, ReadOnlySpan<Guid> ids)
    {
        var elements = new byte[ids.Length * ObjectIdWireSize];
        for (var i = 0; i < ids.Length; i++)
        {
            var o = i * ObjectIdWireSize;
            BinaryPrimitives.WriteInt16LittleEndian(elements.AsSpan(o), GameObjectGuid.OidTypeGuid);
            BinaryPrimitives.WriteInt16LittleEndian(elements.AsSpan(o + 2), 0); // padding_2
            BinaryPrimitives.WriteInt32LittleEndian(elements.AsSpan(o + 4), 0); // padding_4
            ids[i].ToByteArray().CopyTo(elements, o + 8);
        }
        return new ObjectProperty
        {
            Field = property.Field,
            RawBytes = SarEncoding.BuildSarBytes(ObjectIdWireSize, ids.Length, elements),
        };
    }

    /// <summary>
    /// Returns a new <see cref="ObjectProperty"/> holding an empty <c>OD_TYPE_HANDLE_ARRAY</c>
    /// SAR block (zero elements, element size = <see cref="ObjectIdWireSize"/>).
    /// Use this to clear container or critter inventory lists in mob overrides.
    /// </summary>
    public static ObjectProperty WithEmptyObjectIdArray(this ObjectProperty property) => property.WithObjectIdArray([]);

    /// <summary>
    /// Returns a new <see cref="ObjectProperty"/> encoding <paramref name="ids"/> as a
    /// <c>OD_TYPE_HANDLE_ARRAY</c> SAR block preserving each entry's full ObjectID:
    /// type field, proto-or-data field, and GUID.  Use this instead of
    /// <see cref="WithObjectIdArray"/> when the list contains <c>OID_TYPE_A</c> (type=1) entries
    /// that store a prototype index in <c>ProtoOrData1</c> — writing those via
    /// <see cref="WithObjectIdArray"/> would corrupt the OID type to <c>OID_TYPE_GUID</c>.
    /// </summary>
    public static ObjectProperty WithObjectIdArrayFull(
        this ObjectProperty property,
        ReadOnlySpan<(short OidType, int ProtoOrData1, Guid Id)> ids
    )
    {
        var elements = new byte[ids.Length * ObjectIdWireSize];
        for (var i = 0; i < ids.Length; i++)
        {
            var o = i * ObjectIdWireSize;
            var (oidType, protoOrData1, id) = ids[i];
            BinaryPrimitives.WriteInt16LittleEndian(elements.AsSpan(o), oidType);
            BinaryPrimitives.WriteInt16LittleEndian(elements.AsSpan(o + 2), 0); // padding_2
            BinaryPrimitives.WriteInt32LittleEndian(elements.AsSpan(o + 4), 0); // padding_4
            // The 16-byte TigGuid starts at byte 8.  For OID_TYPE_A the first 4 bytes of
            // TigGuid overlap d.a (the proto index), so we must write protoOrData1 there.
            BinaryPrimitives.WriteInt32LittleEndian(elements.AsSpan(o + 8), protoOrData1);
            id.ToByteArray().AsSpan(4).CopyTo(elements.AsSpan(o + 12)); // remaining 12 GUID bytes
        }
        return new ObjectProperty
        {
            Field = property.Field,
            RawBytes = SarEncoding.BuildSarBytes(ObjectIdWireSize, ids.Length, elements),
        };
    }
}

/// <summary>
/// A script attachment stored in an object property SAR array (<c>OD_TYPE_SCRIPT_ARRAY</c>).
/// Wire size: 12 bytes.
/// </summary>
/// <param name="Flags">Script header flags bitmask.</param>
/// <param name="Counters">Per-slot counter bitmask (8 × uint8 packed as uint32).</param>
/// <param name="ScriptId">Script identifier (index into the compiled script table).</param>
public readonly record struct ObjectPropertyScript(uint Flags, uint Counters, int ScriptId);
