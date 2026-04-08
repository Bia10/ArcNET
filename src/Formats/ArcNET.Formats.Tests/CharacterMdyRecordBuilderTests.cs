namespace ArcNET.Formats.Tests;

/// <summary>Unit tests for <see cref="CharacterMdyRecordBuilder"/>.</summary>
public sealed class CharacterMdyRecordBuilderTests
{
    // ── Shared helpers ────────────────────────────────────────────────────────

    private static int[] MakeStats(int fill = 8) => Enumerable.Repeat(fill, 28).ToArray();

    private static int[] MakeBasicSkills(int fill = 0) => new int[12];

    private static int[] MakeTechSkills(int fill = 0) => new int[4];

    private static int[] MakeSpellTech(int fill = 0) => new int[25];

    // ── Basic creation ────────────────────────────────────────────────────────

    [Test]
    public async Task Create_WithValidArgs_ReturnsRecord()
    {
        var rec = CharacterMdyRecordBuilder.Create(MakeStats(), MakeBasicSkills(), MakeTechSkills(), MakeSpellTech());

        await Assert.That(rec).IsNotNull();
    }

    [Test]
    public async Task Create_HasCompleteData_IsTrue()
    {
        var rec = CharacterMdyRecordBuilder.Create(MakeStats(), MakeBasicSkills(), MakeTechSkills(), MakeSpellTech());

        await Assert.That(rec.HasCompleteData).IsTrue();
    }

    [Test]
    public async Task Create_RawBytesNotEmpty()
    {
        var rec = CharacterMdyRecordBuilder.Create(MakeStats(), MakeBasicSkills(), MakeTechSkills(), MakeSpellTech());

        await Assert.That(rec.RawBytes.Length).IsGreaterThan(0);
    }

    // ── Field preservation ────────────────────────────────────────────────────

    [Test]
    public async Task Create_PreservesStats()
    {
        var stats = MakeStats();
        stats[0] = 12; // STR
        stats[4] = 15; // INT
        stats[6] = 13; // WIS

        var rec = CharacterMdyRecordBuilder.Create(stats, MakeBasicSkills(), MakeTechSkills(), MakeSpellTech());

        await Assert.That(rec.Stats[0]).IsEqualTo(12);
        await Assert.That(rec.Stats[4]).IsEqualTo(15);
        await Assert.That(rec.Stats[6]).IsEqualTo(13);
    }

    [Test]
    public async Task Create_PreservesBasicSkills()
    {
        var basic = MakeBasicSkills();
        basic[2] = 5; // MELEE
        basic[10] = 3; // HEAL

        var rec = CharacterMdyRecordBuilder.Create(MakeStats(), basic, MakeTechSkills(), MakeSpellTech());

        await Assert.That(rec.BasicSkills[2]).IsEqualTo(5);
        await Assert.That(rec.BasicSkills[10]).IsEqualTo(3);
    }

    [Test]
    public async Task Create_PreservesTechSkills()
    {
        var tech = MakeTechSkills();
        tech[1] = 7; // FIREARMS

        var rec = CharacterMdyRecordBuilder.Create(MakeStats(), MakeBasicSkills(), tech, MakeSpellTech());

        await Assert.That(rec.TechSkills[1]).IsEqualTo(7);
    }

    [Test]
    public async Task Create_PreservesSpellTech()
    {
        var spell = MakeSpellTech();
        spell[2] = 3; // AIR college

        var rec = CharacterMdyRecordBuilder.Create(MakeStats(), MakeBasicSkills(), MakeTechSkills(), spell);

        await Assert.That(rec.SpellTech[2]).IsEqualTo(3);
    }

    [Test]
    public async Task Create_PreservesGold()
    {
        var rec = CharacterMdyRecordBuilder.Create(
            MakeStats(),
            MakeBasicSkills(),
            MakeTechSkills(),
            MakeSpellTech(),
            gold: 999
        );

        await Assert.That(rec.Gold).IsEqualTo(999);
    }

    [Test]
    public async Task Create_DefaultGoldIsZero()
    {
        var rec = CharacterMdyRecordBuilder.Create(MakeStats(), MakeBasicSkills(), MakeTechSkills(), MakeSpellTech());

        await Assert.That(rec.Gold).IsEqualTo(0);
    }

    [Test]
    public async Task Create_PreservesPortraitIndex()
    {
        var rec = CharacterMdyRecordBuilder.Create(
            MakeStats(),
            MakeBasicSkills(),
            MakeTechSkills(),
            MakeSpellTech(),
            portraitIndex: 7
        );

        await Assert.That(rec.PortraitIndex).IsEqualTo(7);
    }

    [Test]
    public async Task Create_PreservesMaxFollowers()
    {
        var rec = CharacterMdyRecordBuilder.Create(
            MakeStats(),
            MakeBasicSkills(),
            MakeTechSkills(),
            MakeSpellTech(),
            maxFollowers: 3
        );

        await Assert.That(rec.MaxFollowers).IsEqualTo(3);
    }

    [Test]
    public async Task Create_PreservesName()
    {
        var rec = CharacterMdyRecordBuilder.Create(
            MakeStats(),
            MakeBasicSkills(),
            MakeTechSkills(),
            MakeSpellTech(),
            name: "Percival"
        );

        await Assert.That(rec.Name).IsEqualTo("Percival");
    }

    // ── Round-trip ────────────────────────────────────────────────────────────

    [Test]
    public async Task Create_RawBytes_ReparseProducesIdenticalFields()
    {
        var stats = MakeStats();
        stats[0] = 10;
        var rec = CharacterMdyRecordBuilder.Create(
            stats,
            MakeBasicSkills(),
            MakeTechSkills(),
            MakeSpellTech(),
            gold: 500,
            name: "Rondino",
            portraitIndex: 2,
            maxFollowers: 4
        );

        var reparsed = CharacterMdyRecord.Parse(rec.RawBytes, out var consumed);

        await Assert.That(consumed).IsEqualTo(rec.RawBytes.Length);
        await Assert.That(reparsed.Stats[0]).IsEqualTo(10);
        await Assert.That(reparsed.Gold).IsEqualTo(500);
        await Assert.That(reparsed.Name).IsEqualTo("Rondino");
        await Assert.That(reparsed.PortraitIndex).IsEqualTo(2);
        await Assert.That(reparsed.MaxFollowers).IsEqualTo(4);
    }

    [Test]
    public async Task Create_WithPatched_GoldRoundTrips()
    {
        var rec = CharacterMdyRecordBuilder.Create(
            MakeStats(),
            MakeBasicSkills(),
            MakeTechSkills(),
            MakeSpellTech(),
            gold: 100
        );

        var patched = rec.WithGold(99999);
        var reparsed = CharacterMdyRecord.Parse(patched.RawBytes, out _);

        await Assert.That(reparsed.Gold).IsEqualTo(99999);
    }

    [Test]
    public async Task Create_WithPatchedName_RoundTrips()
    {
        var rec = CharacterMdyRecordBuilder.Create(
            MakeStats(),
            MakeBasicSkills(),
            MakeTechSkills(),
            MakeSpellTech(),
            name: "Original"
        );

        var patched = rec.WithName("Renamed");
        await Assert.That(patched.Name).IsEqualTo("Renamed");
    }

    // ── Argument validation ───────────────────────────────────────────────────

    [Test]
    public async Task Create_WrongStatsLength_Throws()
    {
        await Assert
            .That(() =>
                CharacterMdyRecordBuilder.Create(
                    new int[27], // wrong length
                    MakeBasicSkills(),
                    MakeTechSkills(),
                    MakeSpellTech()
                )
            )
            .ThrowsException()
            .WithMessageContaining("28");
    }

    [Test]
    public async Task Create_WrongBasicSkillsLength_Throws()
    {
        await Assert
            .That(() =>
                CharacterMdyRecordBuilder.Create(
                    MakeStats(),
                    new int[11], // wrong length
                    MakeTechSkills(),
                    MakeSpellTech()
                )
            )
            .ThrowsException()
            .WithMessageContaining("12");
    }

    [Test]
    public async Task Create_EmptyName_Throws()
    {
        await Assert
            .That(() =>
                CharacterMdyRecordBuilder.Create(
                    MakeStats(),
                    MakeBasicSkills(),
                    MakeTechSkills(),
                    MakeSpellTech(),
                    name: ""
                )
            )
            .ThrowsException();
    }

    [Test]
    public async Task Create_NonAsciiName_Throws()
    {
        await Assert
            .That(() =>
                CharacterMdyRecordBuilder.Create(
                    MakeStats(),
                    MakeBasicSkills(),
                    MakeTechSkills(),
                    MakeSpellTech(),
                    name: "Héro" // é is non-ASCII
                )
            )
            .ThrowsException();
    }
}
