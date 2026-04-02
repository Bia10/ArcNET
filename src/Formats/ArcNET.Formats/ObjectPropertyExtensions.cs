using System.Buffers.Binary;
using System.Text;

namespace ArcNET.Formats;

/// <summary>
/// Typed read/write accessors for <see cref="ObjectProperty.RawBytes"/>.
/// The wire representation stored in <see cref="ObjectProperty.RawBytes"/> is always
/// little-endian and matches the <c>OD_TYPE_*</c> layout from
/// <c>arcanum-ce/src/game/obj.c</c> exactly — including SAR headers for array fields.
/// </summary>
public static class ObjectPropertyExtensions
{
    // ── SAR header helpers ────────────────────────────────────────────────────
    // SAR (Sizeable Array) wire layout in RawBytes:
    //   byte    sarCount      offset 0   always 0x01
    //   uint32  elementSize   offset 1   bytes per element
    //   uint32  elementCount  offset 5   number of elements
    //   uint32  sarcIndex     offset 9   runtime pointer; always 0 in files
    //   byte[]  data          offset 13  elementSize × elementCount bytes
    //   uint32  postSize      offset 13+data  ceiling(elementCount/32)
    //   uint32[] post         postSize × 4 bytes  all-bits-1 bitmask of active elements

    private static (int ElementSize, int ElementCount, int DataOffset) ParseSarHeader(byte[] rawBytes)
    {
        if (rawBytes.Length < 13)
            throw new InvalidOperationException(
                $"SAR raw bytes too short: need at least 13 header bytes, got {rawBytes.Length}."
            );
        var elementSize = (int)BinaryPrimitives.ReadUInt32LittleEndian(rawBytes.AsSpan(1));
        var elementCount = (int)BinaryPrimitives.ReadUInt32LittleEndian(rawBytes.AsSpan(5));
        return (elementSize, elementCount, 13);
    }

    private static byte[] BuildSarBytes(int elementSize, int elementCount, ReadOnlySpan<byte> elements)
    {
        var postSize = (uint)((elementCount + 31) / 32);
        var totalSize = 13 + elements.Length + 4 + (int)(postSize * 4);
        var bytes = new byte[totalSize];
        bytes[0] = 1; // sarCount
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(1), (uint)elementSize);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(5), (uint)elementCount);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(9), 0); // sarcIndex
        elements.CopyTo(bytes.AsSpan(13));
        var postOffset = 13 + elements.Length;
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(postOffset), postSize);
        for (var i = 0; i < (int)postSize; i++)
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(postOffset + 4 + i * 4), 0xFFFFFFFF);
        return bytes;
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
    /// Valid for <c>OD_TYPE_INT64</c> and <c>OD_TYPE_HANDLE</c> fields (8 bytes).
    /// </summary>
    public static long GetInt64(this ObjectProperty property)
    {
        if (property.RawBytes.Length != 8)
            throw new InvalidOperationException(
                $"Field {property.Field} has {property.RawBytes.Length} bytes; expected 8 for Int64."
            );
        return BinaryPrimitives.ReadInt64LittleEndian(property.RawBytes);
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
    /// Only valid for <c>OD_TYPE_STRING</c> fields (int32 length prefix + ASCII bytes).
    /// </summary>
    public static string GetString(this ObjectProperty property)
    {
        if (property.RawBytes.Length < 4)
            throw new InvalidOperationException(
                $"Field {property.Field} raw bytes too short to contain a string length prefix."
            );
        var length = BinaryPrimitives.ReadInt32LittleEndian(property.RawBytes);
        if (length <= 0)
            return string.Empty;
        if (property.RawBytes.Length < 4 + length)
            throw new InvalidOperationException(
                $"Field {property.Field}: declared string length {length} exceeds available bytes."
            );
        return Encoding.ASCII.GetString(property.RawBytes, 4, length);
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
    /// a little-endian <see cref="long"/> (8 bytes).
    /// </summary>
    public static ObjectProperty WithInt64(this ObjectProperty property, long value)
    {
        var bytes = new byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(bytes, value);
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
    /// an ASCII length-prefixed string (int32 length + ASCII bytes).
    /// </summary>
    public static ObjectProperty WithString(this ObjectProperty property, string value)
    {
        var strBytes = Encoding.ASCII.GetBytes(value);
        var bytes = new byte[4 + strBytes.Length];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, strBytes.Length);
        strBytes.CopyTo(bytes, 4);
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
        return new ObjectProperty { Field = property.Field, RawBytes = BuildSarBytes(4, values.Length, elements) };
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
        return new ObjectProperty { Field = property.Field, RawBytes = BuildSarBytes(4, values.Length, elements) };
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
        return new ObjectProperty { Field = property.Field, RawBytes = BuildSarBytes(8, values.Length, elements) };
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
        return new ObjectProperty { Field = property.Field, RawBytes = BuildSarBytes(12, scripts.Length, elements) };
    }
}

/// <summary>
/// A script attachment stored in an object property SAR array (<c>OD_TYPE_SCRIPT_ARRAY</c>).
/// Wire size: 12 bytes — matches the <c>Script</c> struct from <c>arcanum-ce/src/game/script.h</c>.
/// </summary>
/// <param name="Flags">Script header flags bitmask.</param>
/// <param name="Counters">Per-slot counter bitmask (8 × uint8 packed as uint32).</param>
/// <param name="ScriptId">Script identifier (index into the compiled script table).</param>
public readonly record struct ObjectPropertyScript(uint Flags, uint Counters, int ScriptId);
