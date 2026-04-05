using System.Collections;

namespace ArcNET.GameObjects;

/// <summary>
/// Extension methods for working with <see cref="System.Collections.BitArray"/> using
/// <see cref="ObjectField"/> bit indices from a <see cref="GameObjectHeader"/>.
/// </summary>
public static class BitArrayObjectExtensions
{
    /// <summary>
    /// Returns <see langword="true"/> when the bit for <paramref name="field"/> is set.
    /// </summary>
    /// <param name="bitmap">The <see cref="GameObjectHeader.Bitmap"/> to query.</param>
    /// <param name="field">The field whose bit index is tested.</param>
    public static bool HasField(this BitArray bitmap, ObjectField field) => bitmap[(int)field];

    /// <summary>
    /// Sets or clears the bit for <paramref name="field"/> in the bitmap in-place.
    /// </summary>
    /// <param name="bitmap">The <see cref="GameObjectHeader.Bitmap"/> to mutate.</param>
    /// <param name="field">The field whose bit index is modified.</param>
    /// <param name="value"><see langword="true"/> to mark present; <see langword="false"/> to clear.</param>
    public static void SetField(this BitArray bitmap, ObjectField field, bool value) => bitmap[(int)field] = value;
}
