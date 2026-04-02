using ArcNET.Archive;

namespace ArcNET.Archive.Tests;

public class ArchiveEntryTests
{
    [Test]
    public async Task IsCompressed_WhenCompressedSizeIsZero_ReturnsFalse()
    {
        var entry = new ArchiveEntry
        {
            Path = "test.sec",
            UncompressedSize = 100,
            CompressedSize = 0,
            Offset = 0,
        };
        await Assert.That(entry.IsCompressed).IsFalse();
    }

    [Test]
    public async Task IsCompressed_WhenCompressedSizeIsPositive_ReturnsTrue()
    {
        var entry = new ArchiveEntry
        {
            Path = "test.sec",
            UncompressedSize = 100,
            CompressedSize = 50,
            Offset = 0,
        };
        await Assert.That(entry.IsCompressed).IsTrue();
    }
}
