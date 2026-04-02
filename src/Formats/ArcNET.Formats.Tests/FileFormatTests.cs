using ArcNET.Formats;

namespace ArcNET.Formats.Tests;

public class FileFormatTests
{
    [Test]
    public async Task FromExtension_KnownExtensions_ReturnCorrectFormat()
    {
        await Assert.That(FileFormatExtensions.FromExtension(".sec")).IsEqualTo(FileFormat.Sector);
        await Assert.That(FileFormatExtensions.FromExtension(".mes")).IsEqualTo(FileFormat.Message);
        await Assert.That(FileFormatExtensions.FromExtension(".pro")).IsEqualTo(FileFormat.Proto);
    }

    [Test]
    public async Task FromExtension_CaseInsensitive()
    {
        await Assert.That(FileFormatExtensions.FromExtension(".SEC")).IsEqualTo(FileFormat.Sector);
        await Assert.That(FileFormatExtensions.FromExtension(".ART")).IsEqualTo(FileFormat.Art);
    }

    [Test]
    public async Task FromExtension_Unknown_ReturnsUnknown()
    {
        await Assert.That(FileFormatExtensions.FromExtension(".xyz")).IsEqualTo(FileFormat.Unknown);
    }

    [Test]
    public async Task MessageFormat_ParsesValidLines()
    {
        var lines = new[] { "{100}{Test message}", "{200}{Another}" };
        var entries = MessageFormat.Parse(lines);
        await Assert.That(entries.Count).IsEqualTo(2);
        await Assert.That(entries[0].Index).IsEqualTo(100);
        await Assert.That(entries[0].Text).IsEqualTo("Test message");
    }

    [Test]
    public async Task MessageFormat_SkipsInvalidLines()
    {
        var lines = new[] { "// comment", "", "{42}{valid}" };
        var entries = MessageFormat.Parse(lines);
        await Assert.That(entries.Count).IsEqualTo(1);
        await Assert.That(entries[0].Index).IsEqualTo(42);
    }
}
