using ArcNET.Formats;
using static ArcNET.Formats.Tests.SpanWriterTestHelpers;

namespace ArcNET.Formats.Tests;

/// <summary>Unit tests for <see cref="JmpFormat"/>.</summary>
public sealed class JmpFormatTests
{
    [Test]
    public async Task Parse_Empty_ReturnsZeroJumps()
    {
        var bytes = BuildBytes(w => w.WriteInt32(0));
        var result = JmpFormat.ParseMemory(bytes);
        await Assert.That(result.Jumps.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Parse_SingleEntry_AllFieldsCorrect()
    {
        const uint flags = 0u;
        const long srcLoc = 0x0000_0002_0000_0001L; // X=1, Y=2
        const int dstMap = 42;
        const long dstLoc = 0x0000_0004_0000_0003L; // X=3, Y=4

        var bytes = BuildBytes(w =>
        {
            w.WriteInt32(1);
            w.WriteUInt32(flags);
            w.WriteInt32(0); // padding_4
            w.WriteInt64(srcLoc);
            w.WriteInt32(dstMap);
            w.WriteInt32(0); // padding_14
            w.WriteInt64(dstLoc);
        });

        var result = JmpFormat.ParseMemory(bytes);

        await Assert.That(result.Jumps.Count).IsEqualTo(1);
        var j = result.Jumps[0];
        await Assert.That(j.Flags).IsEqualTo(flags);
        await Assert.That(j.SourceLoc).IsEqualTo(srcLoc);
        await Assert.That(j.SourceX).IsEqualTo(1);
        await Assert.That(j.SourceY).IsEqualTo(2);
        await Assert.That(j.DestinationMapId).IsEqualTo(dstMap);
        await Assert.That(j.DestinationLoc).IsEqualTo(dstLoc);
        await Assert.That(j.DestX).IsEqualTo(3);
        await Assert.That(j.DestY).IsEqualTo(4);
    }

    [Test]
    public async Task RoundTrip_MultipleEntries_Preserved()
    {
        var src = new JmpFile
        {
            Jumps =
            [
                new JumpEntry
                {
                    Flags = 0,
                    SourceLoc = 0x0000_0005_0000_000AL,
                    DestinationMapId = 3,
                    DestinationLoc = 0,
                },
                new JumpEntry
                {
                    Flags = 1,
                    SourceLoc = 0x0000_000A_0000_0014L,
                    DestinationMapId = 7,
                    DestinationLoc = 0x0000_0001_0000_0002L,
                },
            ],
        };

        var bytes = JmpFormat.WriteToArray(in src);
        var back = JmpFormat.ParseMemory(bytes);

        await Assert.That(back.Jumps.Count).IsEqualTo(2);
        await Assert.That(back.Jumps[0].Flags).IsEqualTo(src.Jumps[0].Flags);
        await Assert.That(back.Jumps[0].SourceLoc).IsEqualTo(src.Jumps[0].SourceLoc);
        await Assert.That(back.Jumps[0].DestinationMapId).IsEqualTo(src.Jumps[0].DestinationMapId);
        await Assert.That(back.Jumps[1].Flags).IsEqualTo(src.Jumps[1].Flags);
        await Assert.That(back.Jumps[1].DestinationMapId).IsEqualTo(src.Jumps[1].DestinationMapId);
        await Assert.That(back.Jumps[1].DestX).IsEqualTo(2);
        await Assert.That(back.Jumps[1].DestY).IsEqualTo(1);
    }

    [Test]
    public async Task Write_PaddingIsZero()
    {
        // Verify the two padding fields are zero in the output.
        var src = new JmpFile
        {
            Jumps =
            [
                new JumpEntry
                {
                    Flags = 0,
                    SourceLoc = 0,
                    DestinationMapId = 1,
                    DestinationLoc = 0,
                },
            ],
        };

        var bytes = JmpFormat.WriteToArray(in src);
        // Structure: int32 count + (uint32 flags + int32 pad4 + int64 srcLoc + int32 dstMap + int32 pad14 + int64 dstLoc)
        // Pad4 is at offset 4+4=8
        var pad4 = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(8));
        // Pad14 is at offset 4+4+4+8+4=24
        var pad14 = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(24));

        await Assert.That(pad4).IsEqualTo(0);
        await Assert.That(pad14).IsEqualTo(0);
    }

    [Test]
    public async Task Parse_TruncatedEntry_ThrowsException()
    {
        // Count says 1 entry but only 4 bytes follow (not the full 32-byte struct).
        var bytes = BuildBytes(w =>
        {
            w.WriteInt32(1);
            w.WriteUInt32(0); // only 4 bytes of the 32-byte entry
        });
        await Assert.That(() => JmpFormat.ParseMemory(bytes)).ThrowsException();
    }

    [Test]
    public async Task Parse_MaxPackedLocation_UnpacksCorrectly()
    {
        // Tile X = int.MaxValue, Tile Y = int.MaxValue packed into int64
        const int maxTile = int.MaxValue;
        var packed = (long)(uint)maxTile | ((long)(uint)maxTile << 32);

        var entry = new JumpEntry
        {
            Flags = 0xFFFFFFFF,
            SourceLoc = packed,
            DestinationMapId = int.MaxValue,
            DestinationLoc = 0,
        };
        var src = new JmpFile { Jumps = [entry] };
        var bytes = JmpFormat.WriteToArray(in src);
        var back = JmpFormat.ParseMemory(bytes);

        await Assert.That(back.Jumps[0].SourceX).IsEqualTo(maxTile);
        await Assert.That(back.Jumps[0].SourceY).IsEqualTo(maxTile);
        await Assert.That(back.Jumps[0].DestinationMapId).IsEqualTo(int.MaxValue);
    }
}
