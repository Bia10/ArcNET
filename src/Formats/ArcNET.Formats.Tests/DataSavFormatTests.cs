using System.Buffers.Binary;

namespace ArcNET.Formats.Tests;

public sealed class DataSavFormatTests
{
    private static byte[] BuildDataSavBytes()
    {
        var bytes = new byte[50];
        WriteInt32(bytes, 0, 25);
        WriteInt32(bytes, 1, 32);

        WriteInt32(bytes, 2, 7);
        WriteInt32(bytes, 3, 18);
        WriteInt32(bytes, 4, 2072);
        WriteInt32(bytes, 5, 0x02441780);

        WriteInt32(bytes, 6, 18);
        WriteInt32(bytes, 7, 25);
        WriteInt32(bytes, 8, 2072);
        WriteInt32(bytes, 9, 0x02559988);

        WriteInt32(bytes, 10, 123);
        WriteInt32(bytes, 11, 456);
        bytes[48] = 0xAA;
        bytes[49] = 0xBB;
        return bytes;
    }

    private static void WriteInt32(byte[] bytes, int intIndex, int value) =>
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(intIndex * 4, 4), value);

    [Test]
    public async Task ParseMemory_ExposesVerifiedStructuralSurface()
    {
        var bytes = BuildDataSavBytes();

        var file = DataSavFormat.ParseMemory(bytes);

        await Assert.That(file.Header0).IsEqualTo(25);
        await Assert.That(file.Header1).IsEqualTo(32);
        await Assert.That(file.TotalInts).IsEqualTo(12);
        await Assert.That(file.TrailingBytes).IsEqualTo(2);
        await Assert.That(file.QuadRowCount).IsEqualTo(2);
        await Assert.That(file.RemainderIntCount).IsEqualTo(2);
        await Assert.That(file.GetQuadRow(0)).IsEqualTo(new DataSavQuadRow(7, 18, 2072, 0x02441780));
        await Assert.That(file.GetQuadRow(1)).IsEqualTo(new DataSavQuadRow(18, 25, 2072, 0x02559988));
        await Assert.That(file.GetRemainderInt(0)).IsEqualTo(123);
        await Assert.That(file.GetRemainderInt(1)).IsEqualTo(456);
    }

    [Test]
    public async Task WriteToArray_Unchanged_PreservesBytes()
    {
        var bytes = BuildDataSavBytes();
        var file = DataSavFormat.ParseMemory(bytes);

        var written = DataSavFormat.WriteToArray(file);

        await Assert.That(written.SequenceEqual(bytes)).IsTrue();
    }

    [Test]
    public async Task StructuralPatchMethods_UpdateRequestedIntsOnly()
    {
        var bytes = BuildDataSavBytes();
        var file = DataSavFormat
            .ParseMemory(bytes)
            .WithHeader(25, 31)
            .WithQuadRow(1, new DataSavQuadRow(26, 7, 2072, 0x021CE520))
            .WithRemainderInt(1, 999);

        var written = DataSavFormat.WriteToArray(file);
        var reparsed = DataSavFormat.ParseMemory(written);

        await Assert.That(reparsed.Header1).IsEqualTo(31);
        await Assert.That(reparsed.GetQuadRow(1)).IsEqualTo(new DataSavQuadRow(26, 7, 2072, 0x021CE520));
        await Assert.That(reparsed.GetRemainderInt(1)).IsEqualTo(999);
        await Assert.That(written[48]).IsEqualTo((byte)0xAA);
        await Assert.That(written[49]).IsEqualTo((byte)0xBB);
    }

    [Test]
    public async Task RegionCopyHelpers_CopyRequestedRowsAndRemainderWindow()
    {
        var file = DataSavFormat.ParseMemory(BuildDataSavBytes());
        var rows = new DataSavQuadRow[2];
        var remainder = new int[2];

        file.CopyQuadRows(0, rows);
        file.CopyRemainderInts(0, remainder);

        await Assert.That(rows[0]).IsEqualTo(new DataSavQuadRow(7, 18, 2072, 0x02441780));
        await Assert.That(rows[1]).IsEqualTo(new DataSavQuadRow(18, 25, 2072, 0x02559988));
        await Assert.That(remainder.SequenceEqual(new[] { 123, 456 })).IsTrue();
    }

    [Test]
    public async Task RangePatchMethods_UpdateRequestedRowsAndRemainderWindow()
    {
        var bytes = BuildDataSavBytes();
        var rows = new DataSavQuadRow[2];
        rows[0] = new DataSavQuadRow(8, 19, 2072, 0x02440000);
        rows[1] = new DataSavQuadRow(26, 7, 2072, 0x021CE520);
        var file = DataSavFormat.ParseMemory(bytes).WithQuadRows(0, rows).WithRemainderInts(0, [777, 999]);

        var written = DataSavFormat.WriteToArray(file);
        var reparsed = DataSavFormat.ParseMemory(written);
        var copiedRows = new DataSavQuadRow[2];
        var copiedRemainder = new int[2];
        reparsed.CopyQuadRows(0, copiedRows);
        reparsed.CopyRemainderInts(0, copiedRemainder);

        await Assert.That(copiedRows.SequenceEqual(rows)).IsTrue();
        await Assert.That(copiedRemainder.SequenceEqual(new[] { 777, 999 })).IsTrue();
        await Assert.That(written[48]).IsEqualTo((byte)0xAA);
        await Assert.That(written[49]).IsEqualTo((byte)0xBB);
    }

    [Test]
    public async Task Builder_ComposesMultipleStructuralEdits()
    {
        var bytes = BuildDataSavBytes();
        var rows = new DataSavQuadRow[2];
        rows[0] = new DataSavQuadRow(8, 19, 2072, 0x02440000);
        rows[1] = new DataSavQuadRow(26, 7, 2072, 0x021CE520);

        var file = DataSavFormat
            .ParseMemory(bytes)
            .ToBuilder()
            .WithHeader(25, 31)
            .WithQuadRows(0, rows)
            .WithRemainderInts(0, [777, 999])
            .Build();

        var written = DataSavFormat.WriteToArray(file);
        var reparsed = DataSavFormat.ParseMemory(written);
        var copiedRows = new DataSavQuadRow[2];
        var copiedRemainder = new int[2];
        reparsed.CopyQuadRows(0, copiedRows);
        reparsed.CopyRemainderInts(0, copiedRemainder);

        await Assert.That(reparsed.Header1).IsEqualTo(31);
        await Assert.That(copiedRows.SequenceEqual(rows)).IsTrue();
        await Assert.That(copiedRemainder.SequenceEqual(new[] { 777, 999 })).IsTrue();
        await Assert.That(written[48]).IsEqualTo((byte)0xAA);
        await Assert.That(written[49]).IsEqualTo((byte)0xBB);
    }

    [Test]
    public async Task Builder_AfterBuild_UsesCopyOnWrite()
    {
        var builder = DataSavFormat.ParseMemory(BuildDataSavBytes()).ToBuilder().WithHeader(25, 31);
        var first = builder.Build();

        builder.WithRemainderInt(1, 999);
        var second = builder.Build();

        await Assert.That(first.Header1).IsEqualTo(31);
        await Assert.That(first.GetRemainderInt(1)).IsEqualTo(456);
        await Assert.That(second.Header1).IsEqualTo(31);
        await Assert.That(second.GetRemainderInt(1)).IsEqualTo(999);
    }

    [Test]
    public async Task ParseMemory_TooShort_Throws()
    {
        byte[] bytes = [0x19, 0x00, 0x00, 0x00];

        await Assert.That(() => DataSavFormat.ParseMemory(bytes)).ThrowsException();
    }
}
