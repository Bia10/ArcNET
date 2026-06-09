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
}
