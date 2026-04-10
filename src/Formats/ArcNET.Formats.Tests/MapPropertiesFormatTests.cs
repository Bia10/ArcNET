using ArcNET.Formats;
using static ArcNET.Formats.Tests.SpanWriterTestHelpers;

namespace ArcNET.Formats.Tests;

/// <summary>Unit tests for <see cref="MapPropertiesFormat"/>.</summary>
public sealed class MapPropertiesFormatTests
{
    [Test]
    public async Task Parse_KnownBytes_AllFieldsCorrect()
    {
        var bytes = BuildBytes(w =>
        {
            w.WriteInt32(5); // ArtId
            w.WriteInt32(0); // Unused
            w.WriteUInt64(960UL); // LimitX
            w.WriteUInt64(960UL); // LimitY
        });

        var result = MapPropertiesFormat.ParseMemory(bytes);

        await Assert.That(result.ArtId).IsEqualTo(5);
        await Assert.That(result.Unused).IsEqualTo(0);
        await Assert.That(result.LimitX).IsEqualTo(960UL);
        await Assert.That(result.LimitY).IsEqualTo(960UL);
    }

    [Test]
    public async Task RoundTrip_PreservesAllFields()
    {
        var src = new MapProperties
        {
            ArtId = 99,
            Unused = 12345,
            LimitX = 960,
            LimitY = 960,
        };

        var bytes = MapPropertiesFormat.WriteToArray(in src);
        var back = MapPropertiesFormat.ParseMemory(bytes);

        await Assert.That(back.ArtId).IsEqualTo(src.ArtId);
        await Assert.That(back.Unused).IsEqualTo(src.Unused);
        await Assert.That(back.LimitX).IsEqualTo(src.LimitX);
        await Assert.That(back.LimitY).IsEqualTo(src.LimitY);
    }

    [Test]
    public async Task Write_ProducesExactly24Bytes()
    {
        var src = new MapProperties
        {
            ArtId = 0,
            Unused = 0,
            LimitX = 960,
            LimitY = 960,
        };
        var bytes = MapPropertiesFormat.WriteToArray(in src);
        await Assert.That(bytes.Length).IsEqualTo(24);
    }

    [Test]
    public void Parse_TooShort_ThrowsInvalidDataException()
    {
        Assert.Throws<InvalidDataException>(() => MapPropertiesFormat.ParseMemory(new byte[4]));
    }

    [Test]
    public async Task Parse_AllZeroFields_DoesNotThrow()
    {
        var bytes = BuildBytes(w =>
        {
            w.WriteInt32(0); // ArtId
            w.WriteInt32(0); // Unused
            w.WriteUInt64(0); // LimitX
            w.WriteUInt64(0); // LimitY
        });

        var result = MapPropertiesFormat.ParseMemory(bytes);

        await Assert.That(result.ArtId).IsEqualTo(0);
        await Assert.That(result.LimitX).IsEqualTo(0UL);
        await Assert.That(result.LimitY).IsEqualTo(0UL);
    }

    [Test]
    public async Task RoundTrip_MaxFieldValues_Preserved()
    {
        var src = new MapProperties
        {
            ArtId = int.MaxValue,
            Unused = int.MinValue,
            LimitX = ulong.MaxValue,
            LimitY = ulong.MaxValue,
        };

        var bytes = MapPropertiesFormat.WriteToArray(in src);
        var back = MapPropertiesFormat.ParseMemory(bytes);

        await Assert.That(back.ArtId).IsEqualTo(int.MaxValue);
        await Assert.That(back.Unused).IsEqualTo(int.MinValue);
        await Assert.That(back.LimitX).IsEqualTo(ulong.MaxValue);
        await Assert.That(back.LimitY).IsEqualTo(ulong.MaxValue);
    }
}
