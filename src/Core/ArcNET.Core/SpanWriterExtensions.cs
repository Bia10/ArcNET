using ArcNET.Core.Primitives;

namespace ArcNET.Core;

/// <summary>Domain-specific extension methods for <see cref="SpanWriter"/>.</summary>
public static class SpanWriterExtensions
{
    /// <summary>Writes a <see cref="Location"/> (two <see cref="short"/> values) to the writer.</summary>
    public static void WriteLocation(ref this SpanWriter writer, in Location value) => value.Write(ref writer);

    /// <summary>Writes an <see cref="ArtId"/> (one <see cref="uint"/>) to the writer.</summary>
    public static void WriteArtId(ref this SpanWriter writer, in ArtId value) => value.Write(ref writer);

    /// <summary>Writes a <see cref="GameObjectGuid"/> (four <see cref="uint"/> values) to the writer.</summary>
    public static void WriteGameObjectGuid(ref this SpanWriter writer, in GameObjectGuid value) =>
        value.Write(ref writer);

    /// <summary>
    /// Writes a length-prefixed ASCII string to the writer.
    /// Format: <c>ushort length</c> followed by <paramref name="value"/> bytes in ASCII encoding.
    /// </summary>
    public static void WritePrefixedString(ref this SpanWriter writer, in PrefixedString value) =>
        value.Write(ref writer);

    /// <summary>Writes each element of <paramref name="items"/> using the provided <paramref name="writeOne"/> delegate.</summary>
    public static void WriteArray<T>(ref this SpanWriter writer, IReadOnlyList<T> items, WriteElement<T> writeOne)
    {
        for (var i = 0; i < items.Count; i++)
            writeOne(ref writer, items[i]);
    }
}

/// <summary>Delegate used by <see cref="SpanWriterExtensions.WriteArray{T}"/>.</summary>
public delegate void WriteElement<T>(ref SpanWriter writer, T item);
