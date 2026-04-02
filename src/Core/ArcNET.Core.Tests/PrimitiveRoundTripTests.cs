using System.Buffers;
using ArcNET.Core;
using ArcNET.Core.Primitives;

namespace ArcNET.Core.Tests;

public class PrimitiveRoundTripTests
{
    [Test]
    public async Task ArtId_RoundTrip()
    {
        var original = new ArtId(0xDEADBEEF);
        var buf = new ArrayBufferWriter<byte>();
        var writer = new SpanWriter(buf);
        original.Write(ref writer);
        var reader = new SpanReader(buf.WrittenSpan);
        var restored = ArtId.Read(ref reader);
        await Assert.That(restored).IsEqualTo(original);
    }

    [Test]
    public async Task Location_RoundTrip()
    {
        var original = new Location(-123, 456);
        var buf = new ArrayBufferWriter<byte>();
        var writer = new SpanWriter(buf);
        original.Write(ref writer);
        var reader = new SpanReader(buf.WrittenSpan);
        var restored = Location.Read(ref reader);
        await Assert.That(restored).IsEqualTo(original);
    }

    [Test]
    public async Task Color_RoundTrip()
    {
        var original = new Color(0x12, 0xAB, 0xFF);
        var buf = new ArrayBufferWriter<byte>();
        var writer = new SpanWriter(buf);
        original.Write(ref writer);
        var reader = new SpanReader(buf.WrittenSpan);
        var restored = Color.Read(ref reader);
        await Assert.That(restored).IsEqualTo(original);
    }

    [Test]
    public async Task GameObjectGuid_RoundTrip()
    {
        var original = new GameObjectGuid(1, 2, 3, 0xC0FFEE);
        var buf = new ArrayBufferWriter<byte>();
        var writer = new SpanWriter(buf);
        original.Write(ref writer);
        var reader = new SpanReader(buf.WrittenSpan);
        var restored = GameObjectGuid.Read(ref reader);
        await Assert.That(restored).IsEqualTo(original);
    }

    [Test]
    public async Task PrefixedString_RoundTrip_NonEmpty()
    {
        var original = new PrefixedString("Hello, Arcanum!");
        var buf = new ArrayBufferWriter<byte>();
        var writer = new SpanWriter(buf);
        original.Write(ref writer);
        var reader = new SpanReader(buf.WrittenSpan);
        var restored = PrefixedString.Read(ref reader);
        await Assert.That(restored).IsEqualTo(original);
    }

    [Test]
    public async Task PrefixedString_RoundTrip_Empty()
    {
        var original = new PrefixedString(string.Empty);
        var buf = new ArrayBufferWriter<byte>();
        var writer = new SpanWriter(buf);
        original.Write(ref writer);
        var reader = new SpanReader(buf.WrittenSpan);
        var restored = PrefixedString.Read(ref reader);
        await Assert.That(restored).IsEqualTo(original);
    }

    [Test]
    public async Task SpanReader_Underflow_Throws()
    {
        static void ReadFromEmpty()
        {
            var r = new SpanReader([]);
            _ = r.ReadByte();
        }

        var threw = false;
        try
        {
            ReadFromEmpty();
        }
        catch (IndexOutOfRangeException)
        {
            threw = true;
        }
        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task SpanReader_Slice_IsBounded()
    {
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
        var reader = new SpanReader(data);
        var sub = reader.Slice(2);
        var v1 = sub.ReadByte();
        var v2 = sub.ReadByte();
        var outerRemaining = reader.Remaining;
        await Assert.That(v1).IsEqualTo((byte)0x01);
        await Assert.That(v2).IsEqualTo((byte)0x02);
        await Assert.That(outerRemaining).IsEqualTo(3);
    }

    [Test]
    public async Task SpanReaderExtensions_ReadLocation_MatchesDirect()
    {
        var original = new Location(10, 20);
        var buf = new ArrayBufferWriter<byte>();
        var writer = new SpanWriter(buf);
        original.Write(ref writer);
        var bytes = buf.WrittenSpan.ToArray();

        var r1 = new SpanReader(bytes);
        var direct = Location.Read(ref r1);

        var r2 = new SpanReader(bytes);
        var ext = r2.ReadLocation();

        await Assert.That(ext).IsEqualTo(direct);
    }

    [Test]
    public async Task SpanReaderExtensions_ReadArtId_MatchesDirect()
    {
        var original = new ArtId(0x12345678);
        var buf = new ArrayBufferWriter<byte>();
        var writer = new SpanWriter(buf);
        original.Write(ref writer);
        var bytes = buf.WrittenSpan.ToArray();

        var r1 = new SpanReader(bytes);
        var direct = ArtId.Read(ref r1);

        var r2 = new SpanReader(bytes);
        var ext = r2.ReadArtId();

        await Assert.That(ext).IsEqualTo(direct);
    }
}
