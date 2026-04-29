using ArcNET.Core;
using ArcNET.Core.Primitives;

namespace ArcNET.GameObjects.Types;

internal static class ObjectSerializationHelpers
{
    public static int[] ReadIndexedInts(ref SpanReader reader)
    {
        var count = reader.ReadInt32();
        if (count == 0)
            return [];

        var result = new int[count];
        reader.ReadInt32Array(result);
        return result;
    }

    public static void WriteIndexedInts(ref SpanWriter writer, int[] values)
    {
        writer.WriteInt32(values.Length);
        foreach (var value in values)
            writer.WriteInt32(value);
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

    public static GameObjectGuid[] ReadGuidArray(ref SpanReader reader, int count)
    {
        if (count == 0)
            return [];

        var result = new GameObjectGuid[count];
        for (var i = 0; i < count; i++)
            result[i] = reader.ReadGameObjectGuid();

        return result;
    }

    public static void WriteGuidArray(ref SpanWriter writer, GameObjectGuid[] guids)
    {
        foreach (var guid in guids)
            guid.Write(ref writer);
    }

    public static Location[] ReadLocationArray(ref SpanReader reader)
    {
        var count = reader.ReadInt32();
        if (count == 0)
            return [];

        var result = new Location[count];
        for (var i = 0; i < count; i++)
            result[i] = reader.ReadLocation();

        return result;
    }

    public static void WriteLocationArray(ref SpanWriter writer, Location[] locations)
    {
        writer.WriteInt32(locations.Length);
        foreach (var location in locations)
            location.Write(ref writer);
    }
}
