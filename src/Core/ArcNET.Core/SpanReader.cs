using System.Buffers.Binary;

namespace ArcNET.Core;

/// <summary>Zero-allocation sequential binary reader over a <see cref="ReadOnlySpan{T}"/>.</summary>
public ref struct SpanReader(ReadOnlySpan<byte> data)
{
    private ReadOnlySpan<byte> _remaining = data;

    /// <summary>Gets the number of bytes consumed so far.</summary>
    public int Position { get; private set; }

    /// <summary>Gets the number of bytes still available.</summary>
    public int Remaining => _remaining.Length;

    /// <summary>Reads one byte and advances the position.</summary>
    public byte ReadByte()
    {
        var v = _remaining[0];
        Advance(1);
        return v;
    }

    /// <summary>Reads a little-endian <see cref="short"/> and advances the position.</summary>
    public short ReadInt16()
    {
        var v = BinaryPrimitives.ReadInt16LittleEndian(_remaining);
        Advance(2);
        return v;
    }

    /// <summary>Reads a little-endian <see cref="ushort"/> and advances the position.</summary>
    public ushort ReadUInt16()
    {
        var v = BinaryPrimitives.ReadUInt16LittleEndian(_remaining);
        Advance(2);
        return v;
    }

    /// <summary>Reads a little-endian <see cref="uint"/> and advances the position.</summary>
    public uint ReadUInt32()
    {
        var v = BinaryPrimitives.ReadUInt32LittleEndian(_remaining);
        Advance(4);
        return v;
    }

    /// <summary>Reads a little-endian <see cref="int"/> and advances the position.</summary>
    public int ReadInt32()
    {
        var v = BinaryPrimitives.ReadInt32LittleEndian(_remaining);
        Advance(4);
        return v;
    }

    /// <summary>Reads a little-endian <see cref="long"/> and advances the position.</summary>
    public long ReadInt64()
    {
        var v = BinaryPrimitives.ReadInt64LittleEndian(_remaining);
        Advance(8);
        return v;
    }

    /// <summary>Reads a little-endian <see cref="ulong"/> and advances the position.</summary>
    public ulong ReadUInt64()
    {
        var v = BinaryPrimitives.ReadUInt64LittleEndian(_remaining);
        Advance(8);
        return v;
    }

    /// <summary>Reads a little-endian <see cref="float"/> and advances the position.</summary>
    public float ReadSingle()
    {
        var v = BinaryPrimitives.ReadSingleLittleEndian(_remaining);
        Advance(4);
        return v;
    }

    /// <summary>Reads a little-endian <see cref="double"/> and advances the position.</summary>
    public double ReadDouble()
    {
        var v = BinaryPrimitives.ReadDoubleLittleEndian(_remaining);
        Advance(8);
        return v;
    }

    /// <summary>Reads exactly <paramref name="count"/> bytes without copying.</summary>
    public ReadOnlySpan<byte> ReadBytes(int count)
    {
        var s = _remaining[..count];
        Advance(count);
        return s;
    }

    /// <summary>Advances the position by <paramref name="count"/> bytes without reading.</summary>
    public void Skip(int count) => Advance(count);

    /// <summary>Slices a sub-reader of <paramref name="length"/> bytes and advances the position.</summary>
    public SpanReader Slice(int length)
    {
        var s = new SpanReader(_remaining[..length]);
        Advance(length);
        return s;
    }

    /// <summary>Peeks at the next byte without advancing.</summary>
    public bool TryPeek(out byte value)
    {
        if (_remaining.IsEmpty)
        {
            value = 0;
            return false;
        }

        value = _remaining[0];
        return true;
    }

    /// <summary>Reads a little-endian <see cref="int"/> at the given offset from current position without advancing.</summary>
    public int PeekInt32At(int offset) => BinaryPrimitives.ReadInt32LittleEndian(_remaining[offset..]);

    private void Advance(int count)
    {
        _remaining = _remaining[count..];
        Position += count;
    }
}
