using ArcNET.Formats;

namespace ArcNET.Formats.Tests;

/// <summary>Unit tests for <see cref="TownMapFogFormat"/>.</summary>
public sealed class TownMapFogFormatTests
{
    [Test]
    public async Task Parse_KnownBytes_PreservesRawBytesAndCoverage()
    {
        byte[] bytes = [0b0000_0011, 0b1000_0000];

        var fog = TownMapFogFormat.ParseMemory(bytes);

        await Assert.That(fog.RawBytes.SequenceEqual(bytes)).IsTrue();
        await Assert.That(fog.TotalTiles).IsEqualTo(16);
        await Assert.That(fog.RevealedTiles).IsEqualTo(3);
    }

    [Test]
    public async Task Parse_EmptyFile_ReturnsEmptyFog()
    {
        var fog = TownMapFogFormat.ParseMemory(Array.Empty<byte>());

        await Assert.That(fog.RawBytes.Length).IsEqualTo(0);
        await Assert.That(fog.TotalTiles).IsEqualTo(0);
        await Assert.That(fog.RevealedTiles).IsEqualTo(0);
    }

    [Test]
    public async Task RoundTrip_PreservesRawBytes()
    {
        var src = new TownMapFog { RawBytes = [0xFF, 0x00, 0x81] };

        var bytes = TownMapFogFormat.WriteToArray(in src);
        var back = TownMapFogFormat.ParseMemory(bytes);

        await Assert.That(back.RawBytes.SequenceEqual(src.RawBytes)).IsTrue();
        await Assert.That(back.RevealedTiles).IsEqualTo(src.RevealedTiles);
    }
}
