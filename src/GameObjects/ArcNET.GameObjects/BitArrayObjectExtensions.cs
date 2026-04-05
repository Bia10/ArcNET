namespace ArcNET.GameObjects;

/// <summary>
/// Extension methods for working with a <see cref="GameObjectHeader.Bitmap"/> byte array using
/// <see cref="ObjectField"/> bit indices.  Each byte holds 8 consecutive bits, LSB-first.
/// </summary>
public static class BitArrayObjectExtensions
{
    /// <summary>
    /// Returns <see langword="true"/> when the bit for <paramref name="field"/> is set.
    /// </summary>
    /// <param name="bitmap">The <see cref="GameObjectHeader.Bitmap"/> to query.</param>
    /// <param name="field">The field whose bit index is tested.</param>
    public static bool HasField(this byte[] bitmap, ObjectField field)
    {
        var i = (int)field;
        return (bitmap[i >> 3] & (1 << (i & 7))) != 0;
    }

    /// <summary>
    /// Sets or clears the bit for <paramref name="field"/> in the bitmap in-place.
    /// </summary>
    /// <param name="bitmap">The <see cref="GameObjectHeader.Bitmap"/> to mutate.</param>
    /// <param name="field">The field whose bit index is modified.</param>
    /// <param name="value"><see langword="true"/> to mark present; <see langword="false"/> to clear.</param>
    public static void SetField(this byte[] bitmap, ObjectField field, bool value)
    {
        var i = (int)field;
        if (value)
            bitmap[i >> 3] |= (byte)(1 << (i & 7));
        else
            bitmap[i >> 3] &= (byte)~(1 << (i & 7));
    }
}
