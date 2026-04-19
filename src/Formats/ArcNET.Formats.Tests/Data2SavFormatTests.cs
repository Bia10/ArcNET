using System.Buffers.Binary;

namespace ArcNET.Formats.Tests;

public sealed class Data2SavFormatTests
{
    private static byte[] BuildData2SavBytes(int startInt = 6, int pairCount = 40)
    {
        var totalInts = startInt + pairCount * 2 + 4;
        var bytes = new byte[totalInts * 4];
        WriteInt32(bytes, 0, 25);
        WriteInt32(bytes, 1, 0);
        WriteInt32(bytes, 2, 1);
        WriteInt32(bytes, 3, 2);
        WriteInt32(bytes, 4, -1);
        WriteInt32(bytes, 5, 0);

        for (var index = 0; index < pairCount; index++)
        {
            WriteInt32(bytes, startInt + index * 2, index % 6);
            WriteInt32(bytes, startInt + index * 2 + 1, 50000 + index);
        }

        WriteInt32(bytes, startInt + pairCount * 2, unchecked((int)0xBEEFCAFE));
        WriteInt32(bytes, startInt + pairCount * 2 + 1, -1);
        WriteInt32(bytes, startInt + pairCount * 2 + 2, 169);
        WriteInt32(bytes, startInt + pairCount * 2 + 3, 186);
        return bytes;
    }

    private static void WriteInt32(byte[] bytes, int intIndex, int value) =>
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(intIndex * 4, 4), value);

    [Test]
    public async Task ParseMemory_DetectsIdPairTable()
    {
        var bytes = BuildData2SavBytes();

        var file = Data2SavFormat.ParseMemory(bytes);

        await Assert.That(file.Header0).IsEqualTo(25);
        await Assert.That(file.Header1).IsEqualTo(0);
        await Assert.That(file.IdPairTableStartInt).IsEqualTo(6);
        await Assert.That(file.IdPairs.Count).IsEqualTo(40);
        await Assert.That(file.IdPairTableEndInt).IsEqualTo(85);
        await Assert.That(file.PrefixIntCount).IsEqualTo(6);
        await Assert.That(file.SuffixIntCount).IsEqualTo(4);
        await Assert.That(file.GetPrefixInt(2)).IsEqualTo(1);
        await Assert.That(file.GetPrefixInt(5)).IsEqualTo(0);
        await Assert.That(file.GetSuffixInt(0)).IsEqualTo(unchecked((int)0xBEEFCAFE));
        await Assert.That(file.GetSuffixInt(3)).IsEqualTo(186);
        await Assert.That(file.TryGetIdPairValue(50000, out var firstValue)).IsTrue();
        await Assert.That(firstValue).IsEqualTo(0);
        await Assert.That(file.TryGetIdPairValue(50005, out var sixthValue)).IsTrue();
        await Assert.That(sixthValue).IsEqualTo(5);
    }

    [Test]
    public async Task WriteToArray_Unchanged_PreservesBytes()
    {
        var bytes = BuildData2SavBytes();
        var file = Data2SavFormat.ParseMemory(bytes);

        var written = Data2SavFormat.WriteToArray(file);

        await Assert.That(written.SequenceEqual(bytes)).IsTrue();
    }

    [Test]
    public async Task WriteToArray_WithUpdatedPair_PatchesOnlyValueSlot()
    {
        var bytes = BuildData2SavBytes();
        var file = Data2SavFormat.ParseMemory(bytes);
        var updated = file.WithIdPairValue(50010, 17);

        var written = Data2SavFormat.WriteToArray(updated);
        var reparsed = Data2SavFormat.ParseMemory(written);

        await Assert.That(reparsed.TryGetIdPairValue(50010, out var value)).IsTrue();
        await Assert.That(value).IsEqualTo(17);

        var changedBytes = 0;
        for (var index = 0; index < bytes.Length; index++)
        {
            if (bytes[index] != written[index])
                changedBytes++;
        }

        await Assert.That(changedBytes).IsEqualTo(1);
        await Assert.That(ReadInt32(written, 26)).IsEqualTo(17);
        await Assert.That(ReadInt32(written, 27)).IsEqualTo(50010);
        await Assert.That(ReadInt32(written, 28)).IsEqualTo(5);
    }

    [Test]
    public async Task StructuralPatchHelpers_UpdateOnlyRequestedUnresolvedInts()
    {
        var bytes = BuildData2SavBytes();
        var file = Data2SavFormat.ParseMemory(bytes).WithPrefixInt(5, 11).WithSuffixInt(1, 27);

        var written = Data2SavFormat.WriteToArray(file);
        var reparsed = Data2SavFormat.ParseMemory(written);

        await Assert.That(reparsed.GetPrefixInt(5)).IsEqualTo(11);
        await Assert.That(reparsed.GetSuffixInt(1)).IsEqualTo(27);
        await Assert.That(reparsed.TryGetIdPairValue(50010, out var unchangedValue)).IsTrue();
        await Assert.That(unchangedValue).IsEqualTo(4);
        await Assert.That(ReadInt32(written, 5)).IsEqualTo(11);
        await Assert.That(ReadInt32(written, 87)).IsEqualTo(27);
        await Assert.That(ReadInt32(written, 27)).IsEqualTo(50010);
    }

    [Test]
    public async Task RegionCopyHelpers_CopyRequestedPrefixAndSuffixWindows()
    {
        var file = Data2SavFormat.ParseMemory(BuildData2SavBytes());
        var prefix = new int[4];
        var suffix = new int[3];

        file.CopyPrefixInts(2, prefix);
        file.CopySuffixInts(1, suffix);

        await Assert.That(prefix.SequenceEqual(new[] { 1, 2, -1, 0 })).IsTrue();
        await Assert.That(suffix.SequenceEqual(new[] { -1, 169, 186 })).IsTrue();
    }

    [Test]
    public async Task RangePatchHelpers_UpdateContiguousWindowsWithSingleCopy()
    {
        var bytes = BuildData2SavBytes();
        var file = Data2SavFormat.ParseMemory(bytes).WithPrefixInts(2, [11, 12, 13]).WithSuffixInts(1, [27, 28]);

        var written = Data2SavFormat.WriteToArray(file);
        var reparsed = Data2SavFormat.ParseMemory(written);
        var prefix = new int[3];
        var suffix = new int[2];
        reparsed.CopyPrefixInts(2, prefix);
        reparsed.CopySuffixInts(1, suffix);

        await Assert.That(prefix.SequenceEqual(new[] { 11, 12, 13 })).IsTrue();
        await Assert.That(suffix.SequenceEqual(new[] { 27, 28 })).IsTrue();
        await Assert.That(reparsed.TryGetIdPairValue(50010, out var unchangedValue)).IsTrue();
        await Assert.That(unchangedValue).IsEqualTo(4);
        await Assert.That(ReadInt32(written, 2)).IsEqualTo(11);
        await Assert.That(ReadInt32(written, 3)).IsEqualTo(12);
        await Assert.That(ReadInt32(written, 4)).IsEqualTo(13);
        await Assert.That(ReadInt32(written, 87)).IsEqualTo(27);
        await Assert.That(ReadInt32(written, 88)).IsEqualTo(28);
    }

    [Test]
    public async Task Builder_ComposesPairTableAndStructuralRegionEdits()
    {
        var bytes = BuildData2SavBytes();

        var file = Data2SavFormat
            .ParseMemory(bytes)
            .ToBuilder()
            .WithIdPairValue(50010, 17)
            .WithPrefixInts(2, [11, 12, 13])
            .WithSuffixInts(1, [27, 28])
            .Build();

        var written = Data2SavFormat.WriteToArray(file);
        var reparsed = Data2SavFormat.ParseMemory(written);
        var prefix = new int[3];
        var suffix = new int[2];
        reparsed.CopyPrefixInts(2, prefix);
        reparsed.CopySuffixInts(1, suffix);

        await Assert.That(reparsed.TryGetIdPairValue(50010, out var value)).IsTrue();
        await Assert.That(value).IsEqualTo(17);
        await Assert.That(prefix.SequenceEqual(new[] { 11, 12, 13 })).IsTrue();
        await Assert.That(suffix.SequenceEqual(new[] { 27, 28 })).IsTrue();
    }

    [Test]
    public async Task Builder_AfterBuild_UsesCopyOnWrite()
    {
        var builder = Data2SavFormat.ParseMemory(BuildData2SavBytes()).ToBuilder().WithIdPairValue(50010, 17);
        var first = builder.Build();

        builder.WithSuffixInt(1, 27);
        var second = builder.Build();

        await Assert.That(first.TryGetIdPairValue(50010, out var firstValue)).IsTrue();
        await Assert.That(firstValue).IsEqualTo(17);
        await Assert.That(first.GetSuffixInt(1)).IsEqualTo(-1);
        await Assert.That(second.TryGetIdPairValue(50010, out var secondValue)).IsTrue();
        await Assert.That(secondValue).IsEqualTo(17);
        await Assert.That(second.GetSuffixInt(1)).IsEqualTo(27);
    }

    [Test]
    public async Task ParseMemory_MissingRecognizedTable_Throws()
    {
        byte[] bytes = [0x19, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];

        await Assert.That(() => Data2SavFormat.ParseMemory(bytes)).ThrowsException();
    }

    private static int ReadInt32(byte[] bytes, int intIndex) =>
        BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(intIndex * 4, 4));
}
