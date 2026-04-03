namespace ArcNET.Core;

/// <summary>
/// Contract for types that can be deserialized from a <see cref="SpanReader"/> and
/// serialized back to a <see cref="SpanWriter"/>.
/// </summary>
/// <typeparam name="TSelf">The implementing type.</typeparam>
/// <typeparam name="TReader">The reader type (typically <see cref="SpanReader"/>).</typeparam>
public interface IBinarySerializable<TSelf, TReader>
    where TSelf : IBinarySerializable<TSelf, TReader>
    where TReader : allows ref struct
{
    /// <summary>Reads an instance from <paramref name="reader"/>.</summary>
    static abstract TSelf Read(ref TReader reader);

    /// <summary>Writes this instance to <paramref name="writer"/>.</summary>
    void Write(ref SpanWriter writer);
}
