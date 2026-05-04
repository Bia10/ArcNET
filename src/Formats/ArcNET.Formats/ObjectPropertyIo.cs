using System.Numerics;
using ArcNET.Core;
using ArcNET.GameObjects;

namespace ArcNET.Formats;

/// <summary>
/// Stores a serialized object field: its bit-index identity and the raw bytes on disk.
/// Use the per-field typed accessor extensions once defined; for now all data is opaque.
/// </summary>
public sealed class ObjectProperty
{
    /// <summary>Field identity (bit index in the header bitmap).</summary>
    public required ObjectField Field { get; init; }

    /// <summary>
    /// Raw bytes as read from disk, in full wire representation
    /// (including SAR headers for array types).
    /// Empty when <see cref="ParseNote"/> is set (the field could not be read).
    /// </summary>
    public required byte[] RawBytes { get; init; }

    /// <summary>
    /// Non-null when this property is a sentinel indicating the parse stopped here.
    /// All subsequent bitmap bits were skipped because the wire type for this bit is unknown.
    /// </summary>
    public string? ParseNote { get; init; }
}

// ─── Dispatch tables ───────────────────────────────────────────────────────────

/// <summary>
/// Internal property I/O helpers shared by <see cref="MobFormat"/> and <see cref="ProtoFormat"/>.
/// Wire-type tables are cross-referenced from the engine's
/// <c>object_fields[]</c> and the guide's documented partial table in section 3.2.2.
/// </summary>
internal static class ObjectPropertyIo
{
    private static readonly IObjectPropertySchemaProvider s_schemaProvider = ObjectPropertySchemaProvider.Default;
    private const int ByteWireSize = 1;
    private const int Rgb24WireSize = 3;
    private const int Int32WireSize = 4;
    private const int Int64WireSize = 8;
    private const int ObjectIdWireSize = ObjectPropertyExtensions.ObjectIdWireSize;

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// Reads all object properties present in the bitmap and returns them as an ordered list.
    /// Fields are read in bitmap bit-order (bit 0 first, then bit 1, …).
    /// </summary>
    internal static IReadOnlyList<ObjectProperty> ReadProperties(ref SpanReader reader, GameObjectHeader header) =>
        ReadProperties(ref reader, header, s_schemaProvider);

    internal static IReadOnlyList<ObjectProperty> ReadProperties(
        ref SpanReader reader,
        GameObjectHeader header,
        IObjectPropertySchemaProvider schemaProvider
    )
    {
        ArgumentNullException.ThrowIfNull(schemaProvider);

        var bitmap = header.Bitmap;
        var objectType = header.GameObjectType;

        // Count set bits in one pass using hw PopCount across all bytes.
        var capacity = 0;
        foreach (var b in bitmap)
            capacity += int.PopCount(b);

        if (capacity == 0)
            return [];

        // Use List so we can do early return on unknown wire type without wasting the allocation.
        var props = new List<ObjectProperty>(capacity);

        // Iterate only set bits via TrailingZeroCount — O(set-bits), not O(all-bits).
        for (var by = 0; by < bitmap.Length; by++)
        {
            var word = (uint)bitmap[by];
            while (word != 0)
            {
                var lsb = BitOperations.TrailingZeroCount(word);
                var bit = by * 8 + lsb;

                ObjectWireType wireType;
                try
                {
                    wireType = schemaProvider.ResolveWireType(objectType, bit);
                }
                catch (NotSupportedException ex)
                {
                    // Wire type for this bit is not yet mapped.  We cannot safely advance the
                    // reader without knowing the field size, so we stop here and surface a note.
                    props.Add(
                        new ObjectProperty
                        {
                            Field = (ObjectField)bit,
                            RawBytes = [],
                            ParseNote = ex.Message,
                        }
                    );
                    return props;
                }

                byte[] raw;
                try
                {
                    raw = ReadField(ref reader, wireType);
                }
                catch (Exception ex) when (ex is NotSupportedException or ArgumentOutOfRangeException)
                {
                    // Field data is malformed or truncated — we cannot safely advance the
                    // reader, so stop here and surface a note.
                    props.Add(
                        new ObjectProperty
                        {
                            Field = (ObjectField)bit,
                            RawBytes = [],
                            ParseNote = ex.Message,
                        }
                    );
                    return props;
                }

                props.Add(new ObjectProperty { Field = (ObjectField)bit, RawBytes = raw });
                word &= word - 1; // clear lowest set bit
            }
        }

        return props;
    }

    /// <summary>
    /// Writes all object properties back in bitmap bit-order.
    /// </summary>
    internal static void WriteProperties(IReadOnlyList<ObjectProperty> properties, ref SpanWriter writer)
    {
        foreach (var prop in properties)
            writer.WriteBytes(prop.RawBytes);
    }

    // ── Field readers ─────────────────────────────────────────────────────

    private static byte[] ReadField(ref SpanReader reader, ObjectWireType wireType) =>
        wireType switch
        {
            ObjectWireType.Int32 or ObjectWireType.Float => reader.ReadBytes(Int32WireSize).ToArray(),
            ObjectWireType.Rgb24 => reader.ReadBytes(Rgb24WireSize).ToArray(),
            ObjectWireType.Int64 => ReadPresencePrefixedField(ref reader, Int64WireSize),
            ObjectWireType.ObjectId => reader.ReadBytes(ObjectIdWireSize).ToArray(),
            ObjectWireType.String => ReadStringField(ref reader),
            ObjectWireType.Int32Array
            or ObjectWireType.UInt32Array
            or ObjectWireType.Int64Array
            or ObjectWireType.HandleArray
            or ObjectWireType.ScriptArray
            or ObjectWireType.QuestArray => ReadSarField(ref reader),
            _ => throw new ArgumentOutOfRangeException(nameof(wireType), wireType, null),
        };

    /// <summary>
    /// Reads a presence-prefixed fixed-size field (used for OD_TYPE_INT64).
    /// Wire format: uint8 presence + <paramref name="dataSize"/> bytes if present.
    /// </summary>
    private static byte[] ReadPresencePrefixedField(ref SpanReader reader, int dataSize)
    {
        var presence = reader.ReadByte();
        if (presence == 0)
            return [0];

        EnsureRemainingBytes(reader.Remaining, dataSize, $"fixed-size field data ({dataSize}B)");

        var raw = new byte[ByteWireSize + dataSize];
        raw[0] = presence;
        reader.ReadBytes(dataSize).CopyTo(raw.AsSpan(ByteWireSize));
        return raw;
    }

    /// <summary>
    /// Reads a presence-prefixed string field (OD_TYPE_STRING).
    /// Wire format: uint8 presence + int32 length + (length+1) bytes (including NUL terminator).
    /// </summary>
    private static byte[] ReadStringField(ref SpanReader reader)
    {
        var presence = reader.ReadByte();
        if (presence == 0)
            return [0];

        EnsureRemainingBytes(reader.Remaining, Int32WireSize, "string length prefix (4B)");

        // Read length directly off the span — no intermediate ToArray().
        var length = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(reader.ReadBytes(Int32WireSize));
        if (length < 0)
            throw new NotSupportedException($"String field length was negative ({length}).");

        // The game writes strlen() as the length, then writes strlen()+1 bytes (including NUL).
        var strDataSize = (long)length + 1;
        EnsureRemainingBytes(reader.Remaining, strDataSize, $"string payload ({strDataSize}B)");

        // Allocate the single final buffer and fill in one pass.
        var total = ByteWireSize + Int32WireSize + (int)strDataSize;
        var raw = new byte[total];
        raw[0] = presence;
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(raw.AsSpan(ByteWireSize), length);
        reader.ReadBytes((int)strDataSize).CopyTo(raw.AsSpan(ByteWireSize + Int32WireSize));
        return raw;
    }

    /// <summary>
    /// Reads a presence-prefixed SizeableArray (SAR) field.
    /// Wire format:
    ///   uint8   presence        — 0 = absent, non-zero = SA data follows
    ///   int32   sa.size         — element size in bytes  (part of SizeableArray struct)
    ///   int32   sa.count        — number of elements     (part of SizeableArray struct)
    ///   int32   sa.bitset_id    — in-memory bitset ID    (part of SizeableArray struct, ignored on read)
    ///   byte[]  data            — sa.size × sa.count bytes of element data
    ///   int32   bitset_cnt      — number of 32-bit bitset storage words
    ///   int32[] bitset_data     — bitset_cnt × 4 bytes of bitmask data
    /// </summary>
    private static byte[] ReadSarField(ref SpanReader reader)
    {
        var presence = reader.ReadByte();
        if (presence == 0)
            return [0];

        EnsureRemainingBytes(reader.Remaining, 12, "SAR header (12B)");

        // SizeableArray header: { int32 size, int32 count, int32 bitset_id } = 12 bytes
        var elementSize = reader.ReadUInt32();
        var elementCount = reader.ReadUInt32();
        var bitsetId = reader.ReadUInt32(); // in-memory reference, preserved for round-trip

        // Use long arithmetic to avoid uint overflow when multiplying large values.
        var dataLen = (long)elementSize * elementCount;
        var bytesRequiredBeforeBitset = dataLen + Int32WireSize;
        if (bytesRequiredBeforeBitset > reader.Remaining)
            throw new NotSupportedException(
                $"SAR element data plus bitset count ({bytesRequiredBeforeBitset}B) exceeds available bytes ({reader.Remaining}B). "
                    + $"elementSize={elementSize}, elementCount={elementCount}"
            );

        // Read both variable-length regions as zero-copy spans before allocating the output buffer.
        var dataSpan = reader.ReadBytes((int)dataLen);
        EnsureRemainingBytes(reader.Remaining, Int32WireSize, "SAR bitset count (4B)");
        var bitsetCnt = reader.ReadUInt32();
        var bitsetLen = (long)bitsetCnt * 4;
        if (bitsetLen > reader.Remaining)
            throw new NotSupportedException(
                $"SAR bitset data ({bitsetLen}B) exceeds available bytes ({reader.Remaining}B). "
                    + $"bitsetCnt={bitsetCnt}"
            );
        var bitsetSpan = reader.ReadBytes((int)bitsetLen);

        // Both dataLen and bitsetLen are known to fit in int (already checked against Remaining).
        var dataLenI = (int)dataLen;
        var bitsetLenI = (int)bitsetLen;

        // Single allocation for the full wire representation.
        // presence(1) + SA header(12) + data + bitsetCnt(4) + bitset
        var total = 1 + 12 + dataLenI + 4 + bitsetLenI;
        var raw = new byte[total];
        var p = 0;
        raw[p++] = presence;
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(raw.AsSpan(p), elementSize);
        p += 4;
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(raw.AsSpan(p), elementCount);
        p += 4;
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(raw.AsSpan(p), bitsetId);
        p += 4;
        dataSpan.CopyTo(raw.AsSpan(p));
        p += dataLenI;
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(raw.AsSpan(p), bitsetCnt);
        p += 4;
        bitsetSpan.CopyTo(raw.AsSpan(p));

        return raw;
    }

    private static void EnsureRemainingBytes(int remaining, long requiredBytes, string payloadLabel)
    {
        if (requiredBytes > remaining)
        {
            throw new NotSupportedException(
                $"{payloadLabel} exceeds available bytes ({remaining}B remaining, needed {requiredBytes}B)."
            );
        }
    }
}
