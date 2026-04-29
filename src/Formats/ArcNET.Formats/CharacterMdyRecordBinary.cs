using System.Buffers.Binary;

namespace ArcNET.Formats;

internal static class CharacterMdyRecordBinary
{
    public static int[] ReadInts(ReadOnlySpan<byte> data, int off, int count)
    {
        var arr = new int[count];
        for (var i = 0; i < count && off + i * 4 + 4 <= data.Length; i++)
            arr[i] = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(off + i * 4, 4));

        return arr;
    }

    public static byte[] PatchInts(byte[] source, int off, ReadOnlySpan<int> values)
    {
        var raw = (byte[])source.Clone();
        for (var i = 0; i < values.Length && off + i * 4 + 4 <= raw.Length; i++)
            BinaryPrimitives.WriteInt32LittleEndian(raw.AsSpan(off + i * 4, 4), values[i]);

        return raw;
    }

    public static byte[] CloneAndWriteInt32(byte[] source, int off, int value)
    {
        var raw = (byte[])source.Clone();
        BinaryPrimitives.WriteInt32LittleEndian(raw.AsSpan(off, 4), value);
        return raw;
    }
}
