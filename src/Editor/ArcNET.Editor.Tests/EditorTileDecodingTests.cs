using ArcNET.Editor;

namespace ArcNET.Editor.Tests;

public sealed class EditorTileDecodingTests
{
    [Test]
    public async Task DecodeTileArtEdge_WhenNotMirrored_ReturnsRawEdgeDirectly()
    {
        // Edge 2, raw, not mirrored (bit 0 = 0)
        // Bit 12-15 represents the raw edge value (2 << 12 = 0x2000)
        var artId = 0x00002000u;
        var decodedEdge = EditorWorkspace.DecodeTileArtEdge(artId);
        await Assert.That(decodedEdge).IsEqualTo(2);
    }

    [Test]
    public async Task DecodeTileArtEdge_WhenMirrored_MapsViaEdgeSetTable()
    {
        // Edge 2, raw, mirrored (bit 0 = 1) -> 0x2001
        var artId = 0x00002001u;
        var decodedEdge = EditorWorkspace.DecodeTileArtEdge(artId);
        // s_tileEdgeDecodeWhenFlagsSet[2] is 2
        await Assert.That(decodedEdge).IsEqualTo(2);

        // Edge 3, raw, mirrored (bit 0 = 1) -> (3 << 12) | 1 = 0x3001
        var artId3 = 0x00003001u;
        var decodedEdge3 = EditorWorkspace.DecodeTileArtEdge(artId3);
        // s_tileEdgeDecodeWhenFlagsSet[3] is 9
        await Assert.That(decodedEdge3).IsEqualTo(9);
    }

    [Test]
    public async Task DecodeTileArtFrame_WhenMirroredAndDecoderMatches_OffsetFrameByEight()
    {
        // Mirrored, frame = 1 -> (1 << 9) | 1 = 0x201
        // And rawEdge = 0 (0 << 12 = 0)
        // decodedEdge = 0.
        // s_tileEdgeDecodeWhenFlagsSet[0] = 0, s_tileEdgeDecodeWhenFlagsClear[0] = 0. They are equal!
        // So frame should be offset by 8 (1 + 8 = 9)
        var artId = 0x00000201u;
        var decodedFrame = EditorWorkspace.DecodeTileArtFrame(artId);
        await Assert.That(decodedFrame).IsEqualTo(9);
    }
}
