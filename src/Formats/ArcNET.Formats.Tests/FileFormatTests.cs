using System.Text;
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

    [Test]
    public async Task MessageFormat_ParseMemory_RoundTrip()
    {
        // Build a two-entry MES in UTF-8 bytes and parse via ParseMemory.
        var raw = "{10}{Hello}\n{20}{World}\n";
        var bytes = Encoding.UTF8.GetBytes(raw).AsMemory();
        var entries = MessageFormat.ParseMemory(bytes);
        await Assert.That(entries.Count).IsEqualTo(2);
        await Assert.That(entries[0].Index).IsEqualTo(10);
        await Assert.That(entries[1].Text).IsEqualTo("World");
    }

    [Test]
    public async Task MessageFormat_WriteToArray_RoundTrip()
    {
        // Parse → write → parse again; all entries survive the round-trip.
        var original = new[] { "{1}{Alpha}", "{2}{Beta}", "{3}{Gamma}" };
        var entries = MessageFormat.Parse(original);
        var bytes = MessageFormat.WriteToArray(entries).AsMemory();
        var reparsed = MessageFormat.ParseMemory(bytes);

        await Assert.That(reparsed.Count).IsEqualTo(3);
        await Assert.That(reparsed[0].Text).IsEqualTo("Alpha");
        await Assert.That(reparsed[1].Text).IsEqualTo("Beta");
        await Assert.That(reparsed[2].Text).IsEqualTo("Gamma");
    }

    [Test]
    public async Task TextDataFormat_ParsesKeyValueLines()
    {
        var lines = new[] { "Level:5", "Hit Points:100", "Faction:3 // inline comment" };
        var entries = TextDataFormat.Parse(lines);
        await Assert.That(entries.Count).IsEqualTo(3);
        await Assert.That(entries[0].Key).IsEqualTo("Level");
        await Assert.That(entries[0].Value).IsEqualTo("5");
        await Assert.That(entries[2].Value).IsEqualTo("3");
    }

    [Test]
    public async Task TextDataFormat_SkipsBlankAndCommentLines()
    {
        var lines = new[] { "", "// full comment", "Key:Value" };
        var entries = TextDataFormat.Parse(lines);
        await Assert.That(entries.Count).IsEqualTo(1);
    }

    // ── Stub tests for not-yet-reversed formats ──────────────────────────

    [Test]
    public void ProtoFormat_Parse_ThrowsNotImplementedException() =>
        Assert.Throws<NotImplementedException>(() => ProtoFormat.ParseMemory(new byte[4]));

    [Test]
    public void MobFormat_Parse_ThrowsNotImplementedException() =>
        Assert.Throws<NotImplementedException>(() => MobFormat.ParseMemory(new byte[4]));

    [Test]
    public void ArtFormat_Parse_ThrowsNotImplementedException() =>
        Assert.Throws<NotImplementedException>(() => ArtFormat.ParseMemory(new byte[4]));

    [Test]
    public void JmpFormat_Parse_ThrowsNotImplementedException() =>
        Assert.Throws<NotImplementedException>(() => JmpFormat.ParseMemory(new byte[4]));

    [Test]
    public void ScriptFormat_Parse_ThrowsNotImplementedException() =>
        Assert.Throws<NotImplementedException>(() => ScriptFormat.ParseMemory(new byte[4]));

    [Test]
    public void DialogFormat_Parse_ThrowsNotImplementedException() =>
        Assert.Throws<NotImplementedException>(() => DialogFormat.ParseMemory(new byte[4]));

    [Test]
    public void TerrainFormat_Parse_ThrowsNotImplementedException() =>
        Assert.Throws<NotImplementedException>(() => TerrainFormat.ParseMemory(new byte[4]));

    [Test]
    public void MapPropertiesFormat_Parse_ThrowsNotImplementedException() =>
        Assert.Throws<NotImplementedException>(() => MapPropertiesFormat.ParseMemory(new byte[4]));
}
