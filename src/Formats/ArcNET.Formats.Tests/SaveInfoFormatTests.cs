using System.Buffers;
using System.Text;
using ArcNET.Core;
using ArcNET.Formats;

namespace ArcNET.Formats.Tests;

/// <summary>Unit tests for <see cref="SaveInfoFormat"/>.</summary>
public sealed class SaveInfoFormatTests
{
    private static byte[] BuildGsi(
        string moduleName = "module",
        string leaderName = "Leader",
        string displayName = "Slot 1",
        int mapId = 5,
        int days = 10,
        int ms = 500,
        int portraitId = 3,
        int level = 7,
        int tileX = 100,
        int tileY = 200,
        int storyState = 0,
        int version = 0
    )
    {
        var src = new SaveInfo
        {
            Version = version,
            ModuleName = moduleName,
            LeaderName = leaderName,
            DisplayName = displayName,
            MapId = mapId,
            GameTimeDays = days,
            GameTimeMs = ms,
            LeaderPortraitId = portraitId,
            LeaderLevel = level,
            LeaderTileX = tileX,
            LeaderTileY = tileY,
            StoryState = storyState,
        };
        return SaveInfoFormat.WriteToArray(in src);
    }

    [Test]
    public async Task Parse_KnownBytes_AllFieldsCorrect()
    {
        var bytes = BuildGsi(
            moduleName: "test",
            leaderName: "Hero",
            displayName: "Save01",
            mapId: 7,
            days: 3,
            ms: 1000,
            portraitId: 2,
            level: 10,
            tileX: 50,
            tileY: 75,
            storyState: 0
        );

        var result = SaveInfoFormat.ParseMemory(bytes);

        await Assert.That(result.ModuleName).IsEqualTo("test");
        await Assert.That(result.LeaderName).IsEqualTo("Hero");
        await Assert.That(result.DisplayName).IsEqualTo("Save01");
        await Assert.That(result.MapId).IsEqualTo(7);
        await Assert.That(result.GameTimeDays).IsEqualTo(3);
        await Assert.That(result.GameTimeMs).IsEqualTo(1000);
        await Assert.That(result.LeaderPortraitId).IsEqualTo(2);
        await Assert.That(result.LeaderLevel).IsEqualTo(10);
        await Assert.That(result.LeaderTileX).IsEqualTo(50);
        await Assert.That(result.LeaderTileY).IsEqualTo(75);
        await Assert.That(result.StoryState).IsEqualTo(0);
    }

    [Test]
    public async Task RoundTrip_AllFieldsPreserved()
    {
        var bytes = BuildGsi(
            moduleName: "arcanum",
            leaderName: "Virgil",
            displayName: "Tarrant",
            tileX: 123,
            tileY: 456
        );
        var back = SaveInfoFormat.ParseMemory(bytes);

        await Assert.That(back.ModuleName).IsEqualTo("arcanum");
        await Assert.That(back.LeaderName).IsEqualTo("Virgil");
        await Assert.That(back.DisplayName).IsEqualTo("Tarrant");
        await Assert.That(back.LeaderTileX).IsEqualTo(123);
        await Assert.That(back.LeaderTileY).IsEqualTo(456);
    }

    [Test]
    public void Parse_BadVersion_ThrowsInvalidDataException()
    {
        var buf = new ArrayBufferWriter<byte>();
        var w = new SpanWriter(buf);
        w.WriteInt32(999); // bad version
        Assert.Throws<InvalidDataException>(() => SaveInfoFormat.ParseMemory(buf.WrittenSpan.ToArray().AsMemory()));
    }

    [Test]
    public async Task Parse_EmptyStrings_DoNotThrow()
    {
        var bytes = BuildGsi(moduleName: "", leaderName: "", displayName: "");
        var result = SaveInfoFormat.ParseMemory(bytes);
        await Assert.That(result.ModuleName).IsEqualTo(string.Empty);
        await Assert.That(result.LeaderName).IsEqualTo(string.Empty);
        await Assert.That(result.DisplayName).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task RoundTrip_MaxFieldValues_Preserved()
    {
        var src = new SaveInfo
        {
            ModuleName = "arcanum",
            LeaderName = "Hero",
            DisplayName = "Slot",
            MapId = int.MaxValue,
            GameTimeDays = int.MaxValue,
            GameTimeMs = int.MaxValue,
            LeaderPortraitId = int.MaxValue,
            LeaderLevel = int.MaxValue,
            LeaderTileX = int.MaxValue,
            LeaderTileY = int.MaxValue,
            StoryState = 0,
        };

        var bytes = SaveInfoFormat.WriteToArray(in src);
        var back = SaveInfoFormat.ParseMemory(bytes);

        await Assert.That(back.MapId).IsEqualTo(int.MaxValue);
        await Assert.That(back.GameTimeDays).IsEqualTo(int.MaxValue);
        await Assert.That(back.LeaderLevel).IsEqualTo(int.MaxValue);
        await Assert.That(back.LeaderTileX).IsEqualTo(int.MaxValue);
        await Assert.That(back.LeaderTileY).IsEqualTo(int.MaxValue);
    }

    [Test]
    public async Task Parse_HardcodedBytes_VersionZeroAndKnownFields()
    {
        // Build the binary layout by hand to test parsing of a raw byte stream.
        var buf = new ArrayBufferWriter<byte>();
        var w = new SpanWriter(buf);

        w.WriteInt32(0); // version = 0

        static void WriteStr(ref SpanWriter writer, string s)
        {
            var b = System.Text.Encoding.ASCII.GetBytes(s);
            writer.WriteInt32(b.Length);
            writer.WriteBytes(b);
        }

        WriteStr(ref w, "module1");
        WriteStr(ref w, "Oskar");
        w.WriteInt32(3); // MapId
        w.WriteInt32(1); // GameTimeDays
        w.WriteInt32(500); // GameTimeMs
        w.WriteInt32(4); // LeaderPortraitId
        w.WriteInt32(9); // LeaderLevel
        // LeaderLoc: X=10, Y=20
        var loc = (long)10u | ((long)20u << 32);
        w.WriteInt64(loc);
        w.WriteInt32(0); // StoryState
        WriteStr(ref w, "Test Save");

        var result = SaveInfoFormat.ParseMemory(buf.WrittenSpan.ToArray().AsMemory());

        await Assert.That(result.Version).IsEqualTo(0);
        await Assert.That(result.ModuleName).IsEqualTo("module1");
        await Assert.That(result.LeaderName).IsEqualTo("Oskar");
        await Assert.That(result.MapId).IsEqualTo(3);
        await Assert.That(result.LeaderLevel).IsEqualTo(9);
        await Assert.That(result.LeaderTileX).IsEqualTo(10);
        await Assert.That(result.LeaderTileY).IsEqualTo(20);
        await Assert.That(result.DisplayName).IsEqualTo("Test Save");
    }

    [Test]
    public async Task RoundTrip_Version25_Preserved()
    {
        // UAP/patched Arcanum uses version 25; it must survive a parse → write round-trip.
        var bytes = BuildGsi(version: 25, moduleName: "arcanum", leaderName: "Hero", displayName: "UAP Save");
        var back = SaveInfoFormat.ParseMemory(bytes);

        await Assert.That(back.Version).IsEqualTo(25);
        // Verify the first 4 bytes written are 0x19 00 00 00 (25 LE).
        await Assert.That(bytes[0]).IsEqualTo((byte)25);
        await Assert.That(bytes[1]).IsEqualTo((byte)0);
        await Assert.That(bytes[2]).IsEqualTo((byte)0);
        await Assert.That(bytes[3]).IsEqualTo((byte)0);
    }
}
