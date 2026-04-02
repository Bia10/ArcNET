namespace ArcNET.Formats;

/// <summary>Contract for a stateless span-based binary format reader.</summary>
/// <typeparam name="T">The model type produced by parsing.</typeparam>
public interface IFormatReader<T>
{
    /// <summary>Parses a <typeparamref name="T"/> from the given <paramref name="reader"/>.</summary>
    static abstract T Parse(scoped ref ArcNET.Core.SpanReader reader);

    /// <summary>Parses a <typeparamref name="T"/> from the given byte memory.</summary>
    static abstract T ParseMemory(ReadOnlyMemory<byte> memory);

    /// <summary>Parses a <typeparamref name="T"/> from the file at <paramref name="path"/>.</summary>
    static abstract T ParseFile(string path);
}

/// <summary>Contract for a stateless span-based binary format writer.</summary>
/// <typeparam name="T">The model type written to bytes.</typeparam>
public interface IFormatWriter<T>
{
    /// <summary>Writes <paramref name="value"/> to the given <paramref name="writer"/>.</summary>
    static abstract void Write(in T value, ref ArcNET.Core.SpanWriter writer);

    /// <summary>Serializes <paramref name="value"/> to a newly-allocated byte array.</summary>
    static abstract byte[] WriteToArray(in T value);

    /// <summary>Serializes <paramref name="value"/> and writes the result to the file at <paramref name="path"/>.</summary>
    static abstract void WriteToFile(in T value, string path);
}
