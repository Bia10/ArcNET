using System.Text;
using ArcNET.Core;
using ArcNET.Core.Primitives;

namespace ArcNET.GameObjects.Types;

internal static class ObjectSerializationHelpers
{
    private const int BitsPerWord = 32;
    private const int ObjectIdWireSize = 24;

    public static int[] ReadIndexedInts(ref SpanReader reader)
    {
        var (isPresent, count) = ReadSarHeader(ref reader, expectedElementSize: 4);
        if (!isPresent)
            return [];

        var result = new int[count];
        if (count != 0)
            reader.ReadInt32Array(result);

        SkipSarFooter(ref reader);
        return result;
    }

    public static void WriteIndexedInts(ref SpanWriter writer, int[] values)
    {
        WriteSarHeader(ref writer, elementSize: 4, elementCount: values.Length);
        foreach (var value in values)
            writer.WriteInt32(value);
        WriteSarFooter(ref writer, values.Length);
    }

    public static GameObjectScript[] ReadScripts(ref SpanReader reader)
    {
        var count = reader.ReadInt32();
        if (count == 0)
            return [];

        var result = new GameObjectScript[count];
        for (var i = 0; i < count; i++)
            result[i] = GameObjectScript.Read(ref reader);

        return result;
    }

    public static void WriteScripts(ref SpanWriter writer, GameObjectScript[] scripts)
    {
        writer.WriteInt32(scripts.Length);
        foreach (var script in scripts)
            script.Write(ref writer);
    }

    public static GameObjectGuid[] ReadGuidArray(ref SpanReader reader)
    {
        var (isPresent, count) = ReadSarHeader(ref reader, expectedElementSize: ObjectIdWireSize);
        if (!isPresent)
            return [];

        var result = new GameObjectGuid[count];
        for (var i = 0; i < count; i++)
            result[i] = reader.ReadGameObjectGuid();

        SkipSarFooter(ref reader);
        return result;
    }

    public static void WriteGuidArray(ref SpanWriter writer, GameObjectGuid[] guids)
    {
        WriteSarHeader(ref writer, elementSize: ObjectIdWireSize, elementCount: guids.Length);
        foreach (var guid in guids)
            guid.Write(ref writer);
        WriteSarFooter(ref writer, guids.Length);
    }

    public static Location[] ReadLocationArray(ref SpanReader reader)
    {
        var (isPresent, count) = ReadSarHeader(ref reader, expectedElementSize: 8);
        if (!isPresent)
            return [];

        var result = new Location[count];
        for (var i = 0; i < count; i++)
            result[i] = UnpackLocation(reader.ReadInt64());

        SkipSarFooter(ref reader);
        return result;
    }

    public static void WriteLocationArray(ref SpanWriter writer, Location[] locations)
    {
        WriteSarHeader(ref writer, elementSize: 8, elementCount: locations.Length);
        foreach (var location in locations)
            writer.WriteInt64(PackLocation(location));
        WriteSarFooter(ref writer, locations.Length);
    }

    public static long ReadPresencePrefixedInt64(ref SpanReader reader)
    {
        var presence = reader.ReadByte();
        return presence == 0 ? 0 : reader.ReadInt64();
    }

    public static void WritePresencePrefixedInt64(ref SpanWriter writer, long value)
    {
        writer.WriteByte(1);
        writer.WriteInt64(value);
    }

    public static Location ReadLocation(ref SpanReader reader) => UnpackLocation(ReadPresencePrefixedInt64(ref reader));

    public static void WriteLocation(ref SpanWriter writer, Location location) =>
        WritePresencePrefixedInt64(ref writer, PackLocation(location));

    public static PrefixedString ReadRawString(ref SpanReader reader)
    {
        var presence = reader.ReadByte();
        if (presence == 0)
            return new PrefixedString(string.Empty);

        var length = reader.ReadInt32();
        if (length < 0)
            throw new InvalidDataException($"String length cannot be negative ({length}).");

        var bytes = reader.ReadBytes(checked(length + 1));
        return new PrefixedString(Encoding.ASCII.GetString(bytes[..length]));
    }

    public static void WriteRawString(ref SpanWriter writer, PrefixedString value)
    {
        var bytes = Encoding.ASCII.GetBytes(value.Value);
        writer.WriteByte(1);
        writer.WriteInt32(bytes.Length);
        writer.WriteBytes(bytes);
        writer.WriteByte(0);
    }

    private static (bool IsPresent, int ElementCount) ReadSarHeader(ref SpanReader reader, int expectedElementSize)
    {
        var presence = reader.ReadByte();
        if (presence == 0)
            return (false, 0);

        var elementSize = reader.ReadInt32();
        var elementCount = reader.ReadInt32();
        _ = reader.ReadInt32();

        if (elementSize != expectedElementSize)
        {
            throw new InvalidDataException(
                $"Expected SAR element size {expectedElementSize}, but found {elementSize}."
            );
        }

        if (elementCount < 0)
            throw new InvalidDataException($"SAR element count cannot be negative ({elementCount}).");

        return (true, elementCount);
    }

    private static void SkipSarFooter(ref SpanReader reader)
    {
        var bitsetCount = reader.ReadInt32();
        if (bitsetCount < 0)
            throw new InvalidDataException($"SAR bitset word count cannot be negative ({bitsetCount}).");

        reader.Skip(checked(bitsetCount * 4));
    }

    private static void WriteSarHeader(ref SpanWriter writer, int elementSize, int elementCount)
    {
        writer.WriteByte(1);
        writer.WriteInt32(elementSize);
        writer.WriteInt32(elementCount);
        writer.WriteInt32(0);
    }

    private static void WriteSarFooter(ref SpanWriter writer, int elementCount)
    {
        var bitsetCount = (elementCount + BitsPerWord - 1) / BitsPerWord;
        writer.WriteInt32(bitsetCount);

        for (var i = 0; i < bitsetCount; i++)
        {
            int word;
            if (i < bitsetCount - 1)
                word = unchecked((int)0xFFFFFFFFu);
            else
            {
                var rem = elementCount % BitsPerWord;
                word = rem == 0 ? unchecked((int)0xFFFFFFFFu) : checked((1 << rem) - 1);
            }

            writer.WriteInt32(word);
        }
    }

    private static long PackLocation(Location location) =>
        (long)(uint)(ushort)location.X | ((long)(uint)(ushort)location.Y << 32);

    private static Location UnpackLocation(long packed)
    {
        var x = (int)(packed & 0xFFFFFFFF);
        var y = (int)((packed >> 32) & 0xFFFFFFFF);
        return new Location(checked((short)x), checked((short)y));
    }
}
