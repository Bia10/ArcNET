namespace ArcNET.Formats;

/// <summary>
/// Extension methods for the sector tile-blocking bitmask (<see cref="Sector.BlockMask"/>).
/// The mask is <c>uint[128]</c> (4 096 bits), one bit per tile in a 64 × 64 grid.
/// Bit addressing: <c>tileIndex = tileY * 64 + tileX</c>;
/// word index = <c>tileIndex / 32</c>; bit position = <c>tileIndex % 32</c>.
/// </summary>
public static class BlockMaskExtensions
{
    private const int TilesPerRow = 64;
    private const int BitsPerWord = 32;

    /// <summary>
    /// Returns <see langword="true"/> when tile (<paramref name="tileX"/>, <paramref name="tileY"/>)
    /// is marked as blocked.
    /// </summary>
    /// <param name="blockMask">The 128-element mask from <see cref="Sector.BlockMask"/>.</param>
    /// <param name="tileX">Tile column (0–63).</param>
    /// <param name="tileY">Tile row (0–63).</param>
    public static bool IsBlocked(this uint[] blockMask, int tileX, int tileY) =>
        IsBlocked(blockMask, tileY * TilesPerRow + tileX);

    /// <summary>
    /// Returns <see langword="true"/> when tile at <paramref name="tileIndex"/> is marked as blocked.
    /// </summary>
    /// <param name="blockMask">The 128-element mask from <see cref="Sector.BlockMask"/>.</param>
    /// <param name="tileIndex">Linear tile index (0–4 095).</param>
    public static bool IsBlocked(this uint[] blockMask, int tileIndex) =>
        (blockMask[tileIndex / BitsPerWord] & (1u << (tileIndex % BitsPerWord))) != 0;

    /// <summary>
    /// Sets or clears the blocked bit for tile (<paramref name="tileX"/>, <paramref name="tileY"/>)
    /// in-place.
    /// </summary>
    /// <param name="blockMask">The 128-element mask from <see cref="Sector.BlockMask"/>.</param>
    /// <param name="tileX">Tile column (0–63).</param>
    /// <param name="tileY">Tile row (0–63).</param>
    /// <param name="blocked"><see langword="true"/> to block; <see langword="false"/> to unblock.</param>
    public static void SetBlocked(this uint[] blockMask, int tileX, int tileY, bool blocked) =>
        SetBlocked(blockMask, tileY * TilesPerRow + tileX, blocked);

    /// <summary>
    /// Sets or clears the blocked bit for tile at <paramref name="tileIndex"/> in-place.
    /// </summary>
    /// <param name="blockMask">The 128-element mask from <see cref="Sector.BlockMask"/>.</param>
    /// <param name="tileIndex">Linear tile index (0–4 095).</param>
    /// <param name="blocked"><see langword="true"/> to block; <see langword="false"/> to unblock.</param>
    public static void SetBlocked(this uint[] blockMask, int tileIndex, bool blocked)
    {
        var word = tileIndex / BitsPerWord;
        var bit = 1u << (tileIndex % BitsPerWord);
        if (blocked)
            blockMask[word] |= bit;
        else
            blockMask[word] &= ~bit;
    }
}
