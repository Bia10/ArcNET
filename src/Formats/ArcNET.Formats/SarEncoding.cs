using System.Buffers.Binary;

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
    private const int PresenceOffset = 0;
    private const int SizeFieldOffset = 1;
    private const int CountFieldOffset = 5;
    private const int BitsetIdOffset = 9;
    private const int DataOffset = 13;
    private const int SaHeaderSize = DataOffset - 1; // 12 bytes: size(4) + count(4) + bitset_id(4)
    private const int BitsPerWord = 32;

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
        var bitsetCnt = (uint)((elementCount + BitsPerWord - 1) / BitsPerWord);
        // presence(1) + SA header(12) + data + bitsetCnt(4) + bitsetData
        var totalSize = 1 + SaHeaderSize + elements.Length + 4 + (int)(bitsetCnt * 4);
        var bytes = new byte[totalSize];
        bytes[PresenceOffset] = 1; // presence
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(SizeFieldOffset), (uint)elementSize); // sa.size
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(CountFieldOffset), (uint)elementCount); // sa.count
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(BitsetIdOffset), (uint)bitsetId); // sa.bitset_id
        elements.CopyTo(bytes.AsSpan(DataOffset));
        var postOffset = DataOffset + elements.Length;
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(postOffset), bitsetCnt);
        for (var i = 0; i < (int)bitsetCnt; i++)
        {
            // Fully-occupied words are 0xFFFFFFFF.
            // The last (potentially partial) word must only set the bits that correspond
            // to actual elements; otherwise the engine sees phantom present-element flags.
            uint word;
            if (i < (int)bitsetCnt - 1)
                word = 0xFFFFFFFF;
            else
            {
                var rem = elementCount % BitsPerWord;
                word = rem == 0 ? 0xFFFFFFFF : (1u << rem) - 1u;
            }
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(postOffset + 4 + i * 4), word);
        }
        return bytes;
    }
}
