using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace ArcNET.Formats;

/// <summary>
/// Extension methods for the sector tile-blocking bitmask (<see cref="Sector.BlockMask"/>).
/// The mask is <c>uint[128]</c> (4 096 bits), one bit per tile in a 64 × 64 grid.
/// Bit addressing: <c>tileIndex = tileY * 64 + tileX</c>;
/// word index = <c>tileIndex &gt;&gt; 5</c> (÷ 32); bit position = <c>tileIndex &amp; 31</c> (% 32).
/// </summary>
public static class BlockMaskExtensions
{
    private const int TilesPerRow = 64;
    private const int WordBits = 32; // bits per uint word
    private const int WordShift = 5; // log₂(WordBits): index = tileIndex >> WordShift
    private const int WordMask = WordBits - 1; // bit position = tileIndex & WordMask

    // ── Array overloads (primary API) ─────────────────────────────────────

    /// <inheritdoc cref="IsBlocked(ReadOnlySpan{uint}, int, int)"/>
    public static bool IsBlocked(this uint[] blockMask, int tileX, int tileY) =>
        IsBlocked((ReadOnlySpan<uint>)blockMask, tileY * TilesPerRow + tileX);

    /// <inheritdoc cref="IsBlocked(ReadOnlySpan{uint}, int)"/>
    public static bool IsBlocked(this uint[] blockMask, int tileIndex) =>
        IsBlocked((ReadOnlySpan<uint>)blockMask, tileIndex);

    /// <inheritdoc cref="SetBlocked(Span{uint}, int, int, bool)"/>
    public static void SetBlocked(this uint[] blockMask, int tileX, int tileY, bool blocked) =>
        SetBlocked((Span<uint>)blockMask, tileY * TilesPerRow + tileX, blocked);

    /// <inheritdoc cref="SetBlocked(Span{uint}, int, bool)"/>
    public static void SetBlocked(this uint[] blockMask, int tileIndex, bool blocked) =>
        SetBlocked((Span<uint>)blockMask, tileIndex, blocked);

    /// <inheritdoc cref="CountBlocked(ReadOnlySpan{uint})"/>
    public static int CountBlocked(this uint[] blockMask) => CountBlocked((ReadOnlySpan<uint>)blockMask);

    // ── Span overloads (zero-copy, no array required) ─────────────────────

    /// <summary>
    /// Returns <see langword="true"/> when tile (<paramref name="tileX"/>, <paramref name="tileY"/>)
    /// is marked as blocked.
    /// </summary>
    public static bool IsBlocked(this ReadOnlySpan<uint> blockMask, int tileX, int tileY) =>
        IsBlocked(blockMask, tileY * TilesPerRow + tileX);

    /// <summary>
    /// Returns <see langword="true"/> when tile at <paramref name="tileIndex"/> is marked as blocked.
    /// </summary>
    public static bool IsBlocked(this ReadOnlySpan<uint> blockMask, int tileIndex) =>
        (blockMask[tileIndex >> WordShift] & (1u << (tileIndex & WordMask))) != 0;

    /// <summary>
    /// Sets or clears the blocked bit for tile (<paramref name="tileX"/>, <paramref name="tileY"/>)
    /// in-place.
    /// </summary>
    public static void SetBlocked(this Span<uint> blockMask, int tileX, int tileY, bool blocked) =>
        SetBlocked(blockMask, tileY * TilesPerRow + tileX, blocked);

    /// <summary>
    /// Sets or clears the blocked bit for tile at <paramref name="tileIndex"/> in-place.
    /// </summary>
    public static void SetBlocked(this Span<uint> blockMask, int tileIndex, bool blocked)
    {
        var bit = 1u << (tileIndex & WordMask);
        if (blocked)
            blockMask[tileIndex >> WordShift] |= bit;
        else
            blockMask[tileIndex >> WordShift] &= ~bit;
    }

    /// <summary>
    /// Returns the total number of blocked tiles in the mask via scalar
    /// <see cref="BitOperations.PopCount(uint)"/> — O(words) in all cases.
    /// </summary>
    public static int CountBlocked(this ReadOnlySpan<uint> blockMask)
    {
        var count = 0;
        foreach (var word in blockMask)
            count += BitOperations.PopCount(word);
        return count;
    }
}
