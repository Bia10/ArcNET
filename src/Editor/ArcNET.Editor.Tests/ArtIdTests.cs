using ArcNET.Core.Primitives;

namespace ArcNET.Editor.Tests;

public sealed class ArtIdTests
{
    [Test]
    public async Task RoofPieceIndex_DecodesCeFrameAndMirroredPieceFlag()
    {
        var northPiece = new ArtId(0xA0008000u);
        var eastPiece = new ArtId(0xA0004011u);

        await Assert.That(northPiece.RoofPieceIndex).IsEqualTo(2);
        await Assert.That(eastPiece.RoofPieceIndex).IsEqualTo(10);
    }

    [Test]
    public async Task RoofFlags_DecodeCeFillAndFadeBitsOnlyForRoofArt()
    {
        var roof = new ArtId(0xA0003000u);
        var nonRoof = new ArtId(0x40003000u);

        await Assert.That(roof.IsRoofFill).IsTrue();
        await Assert.That(roof.IsRoofFaded).IsTrue();
        await Assert.That(nonRoof.IsRoofFill).IsFalse();
        await Assert.That(nonRoof.IsRoofFaded).IsFalse();
        await Assert.That(nonRoof.RoofPieceIndex).IsEqualTo(-1);
    }

    [Test]
    public async Task TileType_DecodesTileAndFacadeBitsLikeCe()
    {
        var tileTypeZero = new ArtId(0x00000000u);
        var tileTypeOne = new ArtId(0x00000100u);
        var facadeTypeZero = new ArtId(0xB0000000u);
        var facadeTypeOne = new ArtId(0xB2000000u);
        var scenery = new ArtId(0x40000100u);

        await Assert.That(tileTypeZero.TileType).IsEqualTo(0);
        await Assert.That(tileTypeOne.TileType).IsEqualTo(1);
        await Assert.That(facadeTypeZero.TileType).IsEqualTo(0);
        await Assert.That(facadeTypeOne.TileType).IsEqualTo(1);
        await Assert.That(scenery.TileType).IsEqualTo(0);
    }

    [Test]
    public async Task IsEyeCandyTranslucent_DecodesCeTranslucencyBitOnlyForEyeCandy()
    {
        var translucentEyeCandy = new ArtId(0xE0000100u);
        var opaqueEyeCandy = new ArtId(0xE0000000u);
        var nonEyeCandyWithBit = new ArtId(0x40000100u);

        await Assert.That(translucentEyeCandy.IsEyeCandyTranslucent).IsTrue();
        await Assert.That(opaqueEyeCandy.IsEyeCandyTranslucent).IsFalse();
        await Assert.That(nonEyeCandyWithBit.IsEyeCandyTranslucent).IsFalse();
    }

    [Test]
    public async Task PaletteIndex_DecodesOnlyPaletteAwareArtIds()
    {
        var scenery = new ArtId(0x40000020u);
        var roof = new ArtId(0xA0000030u);
        var sectorTile = new ArtId(0x00000030u);
        var light = new ArtId(0x90000030u);
        var facade = new ArtId(0xB0000030u);

        await Assert.That(scenery.PaletteIndex).IsEqualTo(2);
        await Assert.That(roof.PaletteIndex).IsEqualTo(3);
        await Assert.That(sectorTile.PaletteIndex).IsEqualTo(0);
        await Assert.That(light.PaletteIndex).IsEqualTo(0);
        await Assert.That(facade.PaletteIndex).IsEqualTo(0);
    }
}
