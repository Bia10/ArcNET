using System.Buffers.Binary;

namespace ArcNET.Diagnostics;

public static class SaveGlobalInt32Reader
{
    public static int ReadInt32(byte[] bytes, int intIndex) =>
        intIndex >= 0 && (intIndex + 1) * 4 <= bytes.Length
            ? BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(intIndex * 4, 4))
            : 0;

    public static int CountValue(byte[] bytes, int totalInts, int match)
    {
        var count = 0;
        for (var index = 0; index < totalInts; index++)
        {
            if (ReadInt32(bytes, index) == match)
                count++;
        }

        return count;
    }
}
