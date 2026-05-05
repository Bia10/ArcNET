using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;

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
        EnsureAvailable(1);
        var v = _remaining[0];
        Advance(1);
        return v;
    }

    /// <summary>Reads a little-endian <see cref="short"/> and advances the position.</summary>
    public short ReadInt16()
    {
        EnsureAvailable(2);
        var v = BinaryPrimitives.ReadInt16LittleEndian(_remaining);
        Advance(2);
        return v;
    }

    /// <summary>Reads a little-endian <see cref="ushort"/> and advances the position.</summary>
    public ushort ReadUInt16()
    {
        EnsureAvailable(2);
        var v = BinaryPrimitives.ReadUInt16LittleEndian(_remaining);
        Advance(2);
        return v;
    }

    /// <summary>Reads a little-endian <see cref="uint"/> and advances the position.</summary>
    public uint ReadUInt32()
    {
        EnsureAvailable(4);
        var v = BinaryPrimitives.ReadUInt32LittleEndian(_remaining);
        Advance(4);
        return v;
    }

    /// <summary>Reads a little-endian <see cref="int"/> and advances the position.</summary>
    public int ReadInt32()
    {
        EnsureAvailable(4);
        var v = BinaryPrimitives.ReadInt32LittleEndian(_remaining);
        Advance(4);
        return v;
    }

    /// <summary>Reads a little-endian <see cref="long"/> and advances the position.</summary>
    public long ReadInt64()
    {
        EnsureAvailable(8);
        var v = BinaryPrimitives.ReadInt64LittleEndian(_remaining);
        Advance(8);
        return v;
    }

    /// <summary>Reads a little-endian <see cref="ulong"/> and advances the position.</summary>
    public ulong ReadUInt64()
    {
        EnsureAvailable(8);
        var v = BinaryPrimitives.ReadUInt64LittleEndian(_remaining);
        Advance(8);
        return v;
    }

    /// <summary>Reads a little-endian <see cref="float"/> and advances the position.</summary>
    public float ReadSingle()
    {
        EnsureAvailable(4);
        var v = BinaryPrimitives.ReadSingleLittleEndian(_remaining);
        Advance(4);
        return v;
    }

    /// <summary>Reads a little-endian <see cref="double"/> and advances the position.</summary>
    public double ReadDouble()
    {
        EnsureAvailable(8);
        var v = BinaryPrimitives.ReadDoubleLittleEndian(_remaining);
        Advance(8);
        return v;
    }

    /// <summary>Reads exactly <paramref name="count"/> bytes without copying.</summary>
    public ReadOnlySpan<byte> ReadBytes(int count)
    {
        EnsureAvailable(count);
        var s = _remaining[..count];
        Advance(count);
        return s;
    }

    /// <summary>Advances the position by <paramref name="count"/> bytes without reading.</summary>
    public void Skip(int count) => Advance(count);

    /// <summary>Slices a sub-reader of <paramref name="length"/> bytes and advances the position.</summary>
    public SpanReader Slice(int length)
    {
        EnsureAvailable(length);
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

    /// <summary>Gets the remaining unread bytes as a span without advancing the position.</summary>
    public readonly ReadOnlySpan<byte> RemainingSpan => _remaining;

    /// <summary>Peeks at the next <paramref name="count"/> bytes without advancing.</summary>
    public readonly ReadOnlySpan<byte> PeekSpan(int count)
    {
        EnsureAvailable(count);
        return _remaining[..count];
    }

    /// <summary>Reads a little-endian <see cref="int"/> at the given offset from current position without advancing.</summary>
    public readonly int PeekInt32At(int offset)
    {
        EnsureAvailableAt(offset, 4);
        return BinaryPrimitives.ReadInt32LittleEndian(_remaining[offset..]);
    }

    /// <summary>Reads a little-endian <see cref="uint"/> at the given offset from current position without advancing.</summary>
    public readonly uint PeekUInt32At(int offset)
    {
        EnsureAvailableAt(offset, 4);
        return BinaryPrimitives.ReadUInt32LittleEndian(_remaining[offset..]);
    }

    /// <summary>
    /// Bulk-reads <paramref name="dest"/><c>.Length</c> little-endian <see cref="uint"/> values
    /// using a single <see cref="MemoryMarshal"/> cast (zero-copy on little-endian hosts).
    /// </summary>
    public void ReadUInt32Array(Span<uint> dest)
    {
        var byteCount = checked(dest.Length * 4);
        EnsureAvailable(byteCount);
        MemoryMarshal.Cast<byte, uint>(_remaining[..byteCount]).CopyTo(dest);
        Advance(byteCount);
    }

    /// <summary>
    /// Bulk-reads <paramref name="dest"/><c>.Length</c> little-endian <see cref="ushort"/> values
    /// using a single <see cref="MemoryMarshal"/> cast (zero-copy on little-endian hosts).
    /// </summary>
    public void ReadUInt16Array(Span<ushort> dest)
    {
        var byteCount = checked(dest.Length * 2);
        EnsureAvailable(byteCount);
        MemoryMarshal.Cast<byte, ushort>(_remaining[..byteCount]).CopyTo(dest);
        Advance(byteCount);
    }

    /// <summary>
    /// Bulk-reads <paramref name="dest"/><c>.Length</c> little-endian <see cref="int"/> values
    /// using a single <see cref="MemoryMarshal"/> cast (zero-copy on little-endian hosts).
    /// </summary>
    public void ReadInt32Array(Span<int> dest)
    {
        var byteCount = checked(dest.Length * 4);
        EnsureAvailable(byteCount);
        MemoryMarshal.Cast<byte, int>(_remaining[..byteCount]).CopyTo(dest);
        Advance(byteCount);
    }

    /// <summary>
    /// Bulk-reads <paramref name="dest"/><c>.Length</c> unmanaged values of type
    /// <typeparamref name="T"/> via a single <see cref="MemoryMarshal"/> cast.
    /// Zero-copy on little-endian hosts; caller ensures correct field endianness.
    /// </summary>
    public void ReadUnmanaged<T>(Span<T> dest)
        where T : unmanaged
    {
        var byteCount = dest.Length * System.Runtime.CompilerServices.Unsafe.SizeOf<T>();
        EnsureAvailable(byteCount);
        MemoryMarshal.Cast<byte, T>(_remaining[..byteCount]).CopyTo(dest);
        Advance(byteCount);
    }

    private readonly void EnsureAvailable(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        if (_remaining.Length < count)
        {
            throw new InvalidDataException(
                $"Unexpected end of data at position {Position}; needed {count} byte(s) but only {_remaining.Length} remain."
            );
        }
    }

    private readonly void EnsureAvailableAt(int offset, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        if (_remaining.Length - offset < count)
        {
            throw new InvalidDataException(
                $"Unexpected end of data at position {Position + offset}; needed {count} byte(s) but only {Math.Max(_remaining.Length - offset, 0)} remain."
            );
        }
    }

    private void Advance(int count)
    {
        _remaining = _remaining[count..];
        Position += count;
    }
}
