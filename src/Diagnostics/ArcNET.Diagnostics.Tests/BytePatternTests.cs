namespace ArcNET.Diagnostics.Tests;

public sealed class BytePatternTests
{
    [Test]
    public async Task ParseAndFindMatches_WhenPatternUsesWildcards_ReturnsExpectedOffsets()
    {
        var pattern = BytePattern.Parse("8B ?? FF");
        var haystack = new byte[] { 0x8B, 0x00, 0xFF, 0x8B, 0x11, 0xFF, 0x90 };

        var matches = pattern.FindMatches(haystack);

        await Assert.That(pattern.NormalizedText).IsEqualTo("8B ?? FF");
        await Assert.That(matches).IsEquivalentTo([0, 3]);
    }

    [Test]
    public async Task FindMatches_WhenPatternIsOnlyWildcards_ReturnsEveryValidOffset()
    {
        var pattern = BytePattern.Parse("?? ??");
        var haystack = new byte[] { 0x10, 0x20, 0x30, 0x40 };

        var matches = pattern.FindMatches(haystack);

        await Assert.That(matches).IsEquivalentTo([0, 1, 2]);
    }

    [Test]
    public async Task FindMatches_WhenAnchorIsNotFirstByte_DoesNotReadPastLastValidCandidate()
    {
        var pattern = BytePattern.Parse("?? FE 10");
        var haystack = new byte[] { 0x00, 0xFE, 0x10, 0x11, 0x22, 0xFE };

        var matches = pattern.FindMatches(haystack);

        await Assert.That(matches).IsEquivalentTo([0]);
    }
}
