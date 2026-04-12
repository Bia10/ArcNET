using ArcNET.Formats;

namespace ArcNET.Formats.Tests;

public class BlockMaskExtensionsTests
{
    private static uint[] EmptyMask() => new uint[128];

    [Test]
    public async Task IsBlocked_EmptyMask_ReturnsFalse()
    {
        var mask = EmptyMask();
        await Assert.That(mask.IsBlocked(0, 0)).IsFalse();
        await Assert.That(mask.IsBlocked(63, 63)).IsFalse();
    }

    [Test]
    public async Task SetBlocked_True_MakesTileBlocked()
    {
        var mask = EmptyMask();
        mask.SetBlocked(0, 0, true);
        await Assert.That(mask.IsBlocked(0, 0)).IsTrue();
    }

    [Test]
    public async Task SetBlocked_False_ClearsPreviouslyBlockedTile()
    {
        var mask = EmptyMask();
        mask.SetBlocked(5, 3, true);
        mask.SetBlocked(5, 3, false);
        await Assert.That(mask.IsBlocked(5, 3)).IsFalse();
    }

    [Test]
    public async Task SetBlocked_DoesNotAffectNeighbouringTiles()
    {
        var mask = EmptyMask();
        mask.SetBlocked(1, 0, true);

        await Assert.That(mask.IsBlocked(0, 0)).IsFalse();
        await Assert.That(mask.IsBlocked(2, 0)).IsFalse();
        await Assert.That(mask.IsBlocked(1, 1)).IsFalse();
    }

    [Test]
    public async Task IsBlocked_ByTileIndex_MatchesXYOverload()
    {
        var mask = EmptyMask();
        mask.SetBlocked(7, 3, true);

        var tileIndex = 3 * 64 + 7;
        await Assert.That(mask.IsBlocked(tileIndex)).IsTrue();
        await Assert.That(mask.IsBlocked(7, 3)).IsTrue();
    }

    [Test]
    public async Task SetBlocked_ByTileIndex_MatchesXYOverload()
    {
        var mask = EmptyMask();
        mask.SetBlocked(3 * 64 + 7, true);
        await Assert.That(mask.IsBlocked(7, 3)).IsTrue();
    }

    [Test]
    public async Task SetBlocked_LastTile_WorksAtWordBoundary()
    {
        var mask = EmptyMask();
        mask.SetBlocked(63, 63, true); // tileIndex = 4095 — last bit of last word
        await Assert.That(mask.IsBlocked(63, 63)).IsTrue();
        await Assert.That(mask.IsBlocked(62, 63)).IsFalse();
    }

    [Test]
    public async Task SetBlocked_MultipleIndependentTiles()
    {
        var mask = EmptyMask();
        mask.SetBlocked(0, 0, true);
        mask.SetBlocked(32, 0, true); // first bit of second word
        mask.SetBlocked(0, 1, true);

        await Assert.That(mask.IsBlocked(0, 0)).IsTrue();
        await Assert.That(mask.IsBlocked(32, 0)).IsTrue();
        await Assert.That(mask.IsBlocked(0, 1)).IsTrue();
        await Assert.That(mask.IsBlocked(1, 0)).IsFalse();
    }

    [Test]
    public async Task RoundTrip_AllTilesBlockedThenCleared()
    {
        var mask = EmptyMask();
        for (var y = 0; y < 64; y++)
            for (var x = 0; x < 64; x++)
                mask.SetBlocked(x, y, true);

        for (var i = 0; i < 128; i++)
            await Assert.That(mask[i]).IsEqualTo(uint.MaxValue);

        for (var y = 0; y < 64; y++)
            for (var x = 0; x < 64; x++)
                mask.SetBlocked(x, y, false);

        for (var i = 0; i < 128; i++)
            await Assert.That(mask[i]).IsEqualTo(0u);
    }
}
