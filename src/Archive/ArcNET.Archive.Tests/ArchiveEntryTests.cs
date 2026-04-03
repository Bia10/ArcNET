using ArcNET.Archive;

namespace ArcNET.Archive.Tests;

public class ArchiveEntryTests
{
    [Test]
    public async Task IsCompressed_WhenFlagIsPlain_ReturnsFalse()
    {
        var entry = new ArchiveEntry
        {
            Path = "test.sec",
            Flags = DatEntryFlags.Plain | DatEntryFlags.InArchive,
            UncompressedSize = 100,
            CompressedSize = 100,
            Offset = 0,
        };
        await Assert.That(entry.IsCompressed).IsFalse();
    }

    [Test]
    public async Task IsCompressed_WhenFlagIsCompressed_ReturnsTrue()
    {
        var entry = new ArchiveEntry
        {
            Path = "test.sec",
            Flags = DatEntryFlags.Compressed | DatEntryFlags.InArchive,
            UncompressedSize = 100,
            CompressedSize = 50,
            Offset = 0,
        };
        await Assert.That(entry.IsCompressed).IsTrue();
    }
}
