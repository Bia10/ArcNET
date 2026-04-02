using ArcNET.Core.Primitives;

namespace ArcNET.Core;

/// <summary>Domain-specific extension methods for <see cref="SpanReader"/>.</summary>
public static class SpanReaderExtensions
{
    /// <summary>Reads a <see cref="Location"/> (two <see cref="short"/> values) from the reader.</summary>
    public static Location ReadLocation(ref this SpanReader reader) => Location.Read(ref reader);

    /// <summary>Reads an <see cref="ArtId"/> (one <see cref="uint"/>) from the reader.</summary>
    public static ArtId ReadArtId(ref this SpanReader reader) => ArtId.Read(ref reader);

    /// <summary>Reads a <see cref="GameObjectGuid"/> (four <see cref="uint"/> values) from the reader.</summary>
    public static GameObjectGuid ReadGameObjectGuid(ref this SpanReader reader) => GameObjectGuid.Read(ref reader);

    /// <summary>
    /// Reads a length-prefixed ASCII string from the reader.
    /// Format: <c>ushort length</c> followed by that many ASCII bytes.
    /// </summary>
    public static PrefixedString ReadPrefixedString(ref this SpanReader reader) => PrefixedString.Read(ref reader);

    /// <summary>
    /// Reads an array of <paramref name="count"/> elements using the provided <paramref name="readOne"/> delegate.
    /// </summary>
    public static T[] ReadArray<T>(ref this SpanReader reader, int count, ReadElement<T> readOne)
    {
        var arr = new T[count];
        for (var i = 0; i < count; i++)
            arr[i] = readOne(ref reader);
        return arr;
    }
}

/// <summary>Delegate used by <see cref="SpanReaderExtensions.ReadArray{T}"/>.</summary>
public delegate T ReadElement<T>(ref SpanReader reader);
