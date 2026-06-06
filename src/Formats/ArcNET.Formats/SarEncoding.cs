using Bia.ValueBuffers;

namespace ArcNET.Formats;

/// <summary>
/// Low-level SAR (Sizeable Array) binary encoding shared across format classes.
/// </summary>
/// <remarks>
/// SAR wire layout:
/// <code>
/// byte    presence      offset 0    (0 = absent, non-zero = SA data follows)
/// int32   sa.size       offset 1    (element size in bytes)
/// int32   sa.count      offset 5    (number of elements)
/// int32   sa.bitset_id  offset 9    (in-memory bitset ID, preserved for round-trip)
/// byte[]  data          offset 13   (sa.size × sa.count bytes)
/// int32   bitset_cnt    after data  (number of bitset words)
/// int32[] bitset_data   bitset_cnt × 4 bytes
/// </code>
/// </remarks>
internal static class SarEncoding
{
    private const int BitsPerWord = 32;
    private const int MinimumBitsetWords = 2;

    /// <summary>
    /// Builds the full SAR wire bytes for the given <paramref name="elements"/> data.
    /// The <c>sa.bitset_id</c> field is set to zero.
    /// </summary>
    internal static byte[] BuildSarBytes(int elementSize, int elementCount, ReadOnlySpan<byte> elements) =>
        BuildSarBytes(elementSize, elementCount, bitsetId: 0, elements);

    /// <summary>
    /// Builds the full SAR wire bytes for the given <paramref name="elements"/> data,
    /// with an explicit <paramref name="bitsetId"/> written into the SAR header.
    /// </summary>
    internal static byte[] BuildSarBytes(int elementSize, int elementCount, int bitsetId, ReadOnlySpan<byte> elements)
    {
        var bitsetCnt = Math.Max(MinimumBitsetWords, (elementCount + BitsPerWord - 1) / BitsPerWord);
        Span<byte> initial = stackalloc byte[256];
        using var buf = new ValueByteBuffer(initial);
        buf.Write((byte)1); // presence
        buf.WriteUInt32LittleEndian((uint)elementSize); // sa.size
        buf.WriteUInt32LittleEndian((uint)elementCount); // sa.count
        buf.WriteUInt32LittleEndian((uint)bitsetId); // sa.bitset_id
        buf.Write(elements); // element data
        buf.WriteUInt32LittleEndian((uint)bitsetCnt); // bitset_cnt
        for (var i = 0; i < bitsetCnt; i++)
        {
            var remainingElements = elementCount - (i * BitsPerWord);
            uint word = remainingElements switch
            {
                <= 0 => 0u,
                >= BitsPerWord => 0xFFFFFFFFu,
                _ => (1u << remainingElements) - 1u,
            };
            buf.WriteUInt32LittleEndian(word);
        }
        return buf.ToArray();
    }
}
