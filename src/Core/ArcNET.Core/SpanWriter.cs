using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ArcNET.Core;

/// <summary>Zero-allocation sequential binary writer backed by an <see cref="IBufferWriter{T}"/>.</summary>
public ref struct SpanWriter(IBufferWriter<byte> output)
{
    private readonly IBufferWriter<byte> _output = output;

    /// <summary>Writes a single byte.</summary>
    public void WriteByte(byte v)
    {
        var s = _output.GetSpan(1);
        s[0] = v;
        _output.Advance(1);
    }

    /// <summary>Writes a little-endian <see cref="short"/>.</summary>
    public void WriteInt16(short v)
    {
        var s = _output.GetSpan(2);
        BinaryPrimitives.WriteInt16LittleEndian(s, v);
        _output.Advance(2);
    }

    /// <summary>Writes a little-endian <see cref="ushort"/>.</summary>
    public void WriteUInt16(ushort v)
    {
        var s = _output.GetSpan(2);
        BinaryPrimitives.WriteUInt16LittleEndian(s, v);
        _output.Advance(2);
    }

    /// <summary>Writes a little-endian <see cref="uint"/>.</summary>
    public void WriteUInt32(uint v)
    {
        var s = _output.GetSpan(4);
        BinaryPrimitives.WriteUInt32LittleEndian(s, v);
        _output.Advance(4);
    }

    /// <summary>Writes a little-endian <see cref="int"/>.</summary>
    public void WriteInt32(int v)
    {
        var s = _output.GetSpan(4);
        BinaryPrimitives.WriteInt32LittleEndian(s, v);
        _output.Advance(4);
    }

    /// <summary>Writes a little-endian <see cref="long"/>.</summary>
    public void WriteInt64(long v)
    {
        var s = _output.GetSpan(8);
        BinaryPrimitives.WriteInt64LittleEndian(s, v);
        _output.Advance(8);
    }

    /// <summary>Writes a little-endian <see cref="ulong"/>.</summary>
    public void WriteUInt64(ulong v)
    {
        var s = _output.GetSpan(8);
        BinaryPrimitives.WriteUInt64LittleEndian(s, v);
        _output.Advance(8);
    }

    /// <summary>Writes a little-endian <see cref="float"/>.</summary>
    public void WriteSingle(float v)
    {
        var s = _output.GetSpan(4);
        BinaryPrimitives.WriteSingleLittleEndian(s, v);
        _output.Advance(4);
    }

    /// <summary>Writes a little-endian <see cref="double"/>.</summary>
    public void WriteDouble(double v)
    {
        var s = _output.GetSpan(8);
        BinaryPrimitives.WriteDoubleLittleEndian(s, v);
        _output.Advance(8);
    }

    /// <summary>Writes all bytes from <paramref name="data"/>.</summary>
    public void WriteBytes(scoped ReadOnlySpan<byte> data)
    {
        data.CopyTo(_output.GetSpan(data.Length));
        _output.Advance(data.Length);
    }

    /// <summary>
    /// Writes all unmanaged values from <paramref name="data"/> as their raw little-endian byte representation.
    /// Zero-copy on all little-endian hosts (x86/x64/ARM) via <see cref="MemoryMarshal.Cast{TFrom,TTo}(ReadOnlySpan{TFrom})"/>.
    /// </summary>
    public void WriteUnmanaged<T>(ReadOnlySpan<T> data)
        where T : unmanaged
    {
        var bytes = MemoryMarshal.Cast<T, byte>(data);
        bytes.CopyTo(_output.GetSpan(bytes.Length));
        _output.Advance(bytes.Length);
    }
}
