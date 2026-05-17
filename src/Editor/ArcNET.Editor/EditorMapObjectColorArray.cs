namespace ArcNET.Editor;

public sealed class EditorMapObjectColorArray : IEquatable<EditorMapObjectColorArray>
{
    private readonly uint[] _colors;
    private readonly int _hashCode;

    public EditorMapObjectColorArray(ReadOnlySpan<uint> colors)
    {
        if (colors.Length == 0)
            throw new ArgumentException("Object color arrays must contain at least one color.", nameof(colors));

        _colors = colors.ToArray();
        _hashCode = ComputeHashCode(_colors);
    }

    public int Count => _colors.Length;

    public uint this[int index] => _colors[index];

    public ReadOnlySpan<uint> AsSpan() => _colors;

    public bool Equals(EditorMapObjectColorArray? other) =>
        ReferenceEquals(this, other) || other is not null && _colors.AsSpan().SequenceEqual(other._colors);

    public override bool Equals(object? obj) => obj is EditorMapObjectColorArray other && Equals(other);

    public override int GetHashCode() => _hashCode;

    private static int ComputeHashCode(uint[] colors)
    {
        var hash = new HashCode();
        for (var index = 0; index < colors.Length; index++)
            hash.Add(colors[index]);

        return hash.ToHashCode();
    }
}
