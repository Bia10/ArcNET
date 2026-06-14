using ArcanumDebugger.App.ViewModels;
using ArcNET.Diagnostics;
using ArcNET.GameData.Workspace;

namespace ArcanumDebugger.App.Tests;

public sealed class SpellTechLiveCatalogTests
{
    [Test]
    public async Task CreateProgressionEntries_WhenCollegeHasRank_IncludesKnownSpellPreview()
    {
        var snapshot = new SheetDataSnapshot(
            [],
            [],
            [],
            [],
            [],
            [new SheetSkillSnapshot(0, "Repair", 6, 2, "Expert", 134)],
            [new SheetScalarSnapshot(4, "Fire", 3)],
            new SheetScalarSnapshot(16, "Spell Mastery", -1),
            [new SheetScalarSnapshot(5, "Mechanical", 4)]
        );

        var entries = SpellTechLiveCatalog.CreateProgressionEntries(snapshot);

        await Assert.That(entries.Count).IsEqualTo(3);
        await Assert.That(entries.Any(entry => entry.Kind == SpellTechLiveEntryKind.SpellCollege)).IsTrue();
        await Assert
            .That(entries.First(entry => entry.Kind == SpellTechLiveEntryKind.SpellCollege).DetailText)
            .Contains("Fireflash");
        await Assert
            .That(entries.First(entry => entry.Kind == SpellTechLiveEntryKind.TechSkill).DetailText)
            .Contains("Expert training");
    }

    [Test]
    public async Task CreateSchematicEntries_WhenPrototypeCatalogMatches_UsesDisplayNameAndSlot()
    {
        PrototypePaletteEntry[] prototypes =
        [
            new(
                10079,
                "Generic",
                "proto/items/pyrotechnic-axe.pro",
                "Pyrotechnic Axe",
                "A learned schematic result.",
                "Items",
                "art/items/axe.art"
            ),
        ];

        var entries = SpellTechLiveCatalog.CreateSchematicEntries([10079], prototypes);

        await Assert.That(entries.Count).IsEqualTo(1);
        await Assert.That(entries[0].TitleText).IsEqualTo("Pyrotechnic Axe");
        await Assert.That(entries[0].SubtitleText).Contains("Slot 0");
        await Assert.That(entries[0].ValueText).IsEqualTo("10079");
    }

    [Test]
    public async Task CreateKnownSpellEntries_WhenCollegeRankExists_ExpandsConcreteSpellList()
    {
        var snapshot = new SheetDataSnapshot(
            [],
            [],
            [],
            [],
            [],
            [],
            [new SheetScalarSnapshot(0, "Conveyance", 2), new SheetScalarSnapshot(4, "Fire", 1)],
            new SheetScalarSnapshot(16, "Spell Mastery", -1),
            []
        );

        var entries = SpellTechLiveCatalog.CreateKnownSpellEntries(snapshot);

        await Assert.That(entries.Select(static entry => entry.TitleText)).Contains("Disarm");
        await Assert.That(entries.Select(static entry => entry.TitleText)).Contains("Unlocking Cantrip");
        await Assert.That(entries.Select(static entry => entry.TitleText)).Contains("Agility Of Fire");
        await Assert.That(entries.Any(entry => entry.TitleText == "Teleportation")).IsFalse();
    }
}
