using ArcNET.Core;
using ArcNET.Core.Primitives;

namespace ArcNET.GameObjects.Types;

internal static class ObjectInventoryGuidListCodec
{
    public readonly record struct ReadResult(int ReservedCount, GameObjectGuid[] Values);

    public static ReadResult Read(
        ref SpanReader reader,
        bool hasCount,
        bool hasList,
        int reservedCount,
        GameObjectGuid[] values,
        string ownerName
    )
    {
        if (hasCount)
            reservedCount = reader.ReadInt32();

        if (!hasList)
            return new ReadResult(reservedCount, values);

        if (!hasCount)
            throw new InvalidOperationException($"{ownerName} inventory list requires an inventory count field.");

        values = ObjectSerializationHelpers.ReadGuidArray(ref reader);
        reservedCount = values.Length;

        return new ReadResult(reservedCount, values);
    }

    public static void Write(
        ref SpanWriter writer,
        bool hasCount,
        bool hasList,
        int reservedCount,
        GameObjectGuid[] values,
        string ownerName
    )
    {
        if (hasList && !hasCount)
            throw new InvalidOperationException($"{ownerName} inventory list requires an inventory count field.");

        if (hasCount)
            writer.WriteInt32(hasList ? values.Length : reservedCount);

        if (hasList)
            ObjectSerializationHelpers.WriteGuidArray(ref writer, values);
    }
}
