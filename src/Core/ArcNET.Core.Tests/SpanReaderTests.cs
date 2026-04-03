using ArcNET.Core;

namespace ArcNET.Core.Tests;

public class SpanReaderTests
{
    [Test]
    public async Task ReadByte_ReturnsCorrectValue()
    {
        var data = new byte[] { 0xAB };
        var reader = new SpanReader(data);
        var value = reader.ReadByte();
        await Assert.That(value).IsEqualTo((byte)0xAB);
    }

    [Test]
    public async Task ReadInt32_LittleEndian()
    {
        var data = new byte[] { 0x01, 0x00, 0x00, 0x00 };
        var reader = new SpanReader(data);
        var value = reader.ReadInt32();
        await Assert.That(value).IsEqualTo(1);
    }

    [Test]
    public async Task Position_AdvancesCorrectly()
    {
        var data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var reader = new SpanReader(data);
        _ = reader.ReadInt32();
        // Extract values before await (ref struct cannot cross await boundary)
        var position = reader.Position;
        var remaining = reader.Remaining;
        await Assert.That(position).IsEqualTo(4);
        await Assert.That(remaining).IsEqualTo(4);
    }
}
