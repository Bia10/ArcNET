using System.Linq;
using ArcanumDebugger.App.ViewModels;
using ArcNET.Diagnostics;
using ArcNET.GameData.Workspace;

namespace ArcanumDebugger.App.Tests;

public sealed class GameDataQuickPickCatalogTests
{
    [Test]
    public async Task BuildInventoryPrototypeEntries_FiltersToInventoryTypesAndFormatsProtoTokens()
    {
        PrototypePaletteEntry[] entries =
        [
            new PrototypePaletteEntry(
                14001,
                "Weapon",
                "proto/items/dagger.pro",
                "Rusty Dagger",
                "A chipped blade.",
                "Items",
                "art/items/dagger.art"
            ),
            new PrototypePaletteEntry(
                24001,
                "Npc",
                "proto/critters/guard.pro",
                "City Guard",
                "A stern watchman.",
                "Critters",
                "art/critters/guard.art"
            ),
        ];

        var quickPickEntries = GameDataQuickPickCatalog.BuildInventoryPrototypeEntries(entries, "dagger");

        await Assert.That(quickPickEntries.Count).IsEqualTo(1);
        await Assert.That(quickPickEntries[0].BadgeText).IsEqualTo("Weapon");
        await Assert.That(quickPickEntries[0].ApplyValueText).IsEqualTo("proto:14001");
    }

    [Test]
    public async Task BuildSpawnPrototypeEntries_MatchesWorldObjectsAndMobiles()
    {
        PrototypePaletteEntry[] entries =
        [
            new PrototypePaletteEntry(
                2001,
                "Scenery",
                "proto/scenery/lamp.pro",
                "Lamp Post",
                "Town street light.",
                "Scenery",
                "art/scenery/lamp.art"
            ),
            new PrototypePaletteEntry(
                24001,
                "Npc",
                "proto/critters/guard.pro",
                "City Guard",
                "A stern watchman.",
                "Critters",
                "art/critters/guard.art"
            ),
        ];

        var quickPickEntries = GameDataQuickPickCatalog.BuildSpawnPrototypeEntries(entries, "guard");

        await Assert.That(quickPickEntries.Count).IsEqualTo(1);
        await Assert.That(quickPickEntries[0].TitleText).IsEqualTo("City Guard");
        await Assert.That(quickPickEntries[0].ApplyValueText).IsEqualTo("proto:24001");
    }

    [Test]
    public async Task BuildLookupTokenEntries_CombinesPrototypeAndPlacedObjectSources()
    {
        PrototypePaletteEntry[] prototypeEntries =
        [
            new PrototypePaletteEntry(
                20011,
                "Scenery",
                "proto/scenery/factory-gate.pro",
                "Factory Gate",
                "A heavy industrial gate.",
                "Scenery",
                "art/scenery/factory-gate.art"
            ),
        ];
        StaticObjectCatalogEntry[] staticObjectEntries =
        [
            new StaticObjectCatalogEntry(
                "Sector object",
                "Factory Gate",
                "Scenery",
                "guid:factory-gate",
                "3f61a56b-2818-4f89-a297-8c94504f90a2",
                20011,
                "Factory Gate [20011]",
                "maps/tarant/factory/sec_01.sector",
                "Tile (412, 588)",
                "Sector object - maps/tarant/factory/sec_01.sector"
            ),
        ];

        var quickPickEntries = GameDataQuickPickCatalog.BuildLookupTokenEntries(
            prototypeEntries,
            staticObjectEntries,
            "factory"
        );

        await Assert.That(quickPickEntries.Count).IsEqualTo(2);
        await Assert
            .That(
                quickPickEntries.Any(entry =>
                    entry.BadgeText == "Proto · Scenery" && entry.ApplyValueText == "proto:20011"
                )
            )
            .IsTrue();
        await Assert
            .That(
                quickPickEntries.Any(entry =>
                    entry.BadgeText == "Placed · Scenery"
                    && entry.ApplyValueText == "3f61a56b-2818-4f89-a297-8c94504f90a2"
                )
            )
            .IsTrue();
    }

    [Test]
    public async Task BuildLookupStaticObjectPrototypeEntries_MatchesObjectGuidAndUsesPreferredPlacedObjectToken()
    {
        StaticObjectCatalogEntry[] entries =
        [
            new StaticObjectCatalogEntry(
                "Sector object",
                "Factory Gate",
                "Scenery",
                "guid:factory-gate",
                "3f61a56b-2818-4f89-a297-8c94504f90a2",
                20011,
                "Factory Gate [20011]",
                "maps/tarant/factory/sec_01.sector",
                "Tile (412, 588)",
                "Sector object - maps/tarant/factory/sec_01.sector"
            ),
        ];

        var quickPickEntries = GameDataQuickPickCatalog.BuildLookupStaticObjectPrototypeEntries(
            entries,
            "3f61a56b-2818-4f89-a297-8c94504f90a2"
        );

        await Assert.That(quickPickEntries.Count).IsEqualTo(1);
        await Assert.That(quickPickEntries[0].TitleText).IsEqualTo("Factory Gate");
        await Assert.That(quickPickEntries[0].ApplyValueText).IsEqualTo("3f61a56b-2818-4f89-a297-8c94504f90a2");
        await Assert.That(quickPickEntries[0].HasMultipleValueOptions).IsTrue();
        await Assert
            .That(quickPickEntries[0].RecommendedValueChoice.ValueText)
            .IsEqualTo("3f61a56b-2818-4f89-a297-8c94504f90a2");
        await Assert.That(quickPickEntries[0].ValueSummaryText).Contains("Exact GUID token");
        await Assert.That(quickPickEntries[0].ValueSummaryText).Contains("Object id");
        await Assert.That(quickPickEntries[0].ValueSummaryText).Contains("Proto token");
        await Assert
            .That(quickPickEntries[0].ValueOptions.Select(static option => option.LabelText))
            .Contains("Exact GUID token");
        await Assert
            .That(quickPickEntries[0].ValueOptions.Select(static option => option.ValueText))
            .Contains("guid:factory-gate");
        await Assert
            .That(quickPickEntries[0].ValueOptions.Select(static option => option.ValueText))
            .Contains("proto:20011");
    }

    [Test]
    public async Task BuildInventoryStaticObjectPrototypeEntries_FiltersToInventoryCompatibleObjects()
    {
        StaticObjectCatalogEntry[] entries =
        [
            new StaticObjectCatalogEntry(
                "Mob asset",
                "Rusty Dagger",
                "Weapon",
                "guid:dagger-a",
                string.Empty,
                14001,
                "Rusty Dagger [14001]",
                "maps/shrouded_hills/mob01.mob",
                "Tile unavailable",
                "Mob asset - maps/shrouded_hills/mob01.mob"
            ),
            new StaticObjectCatalogEntry(
                "Sector object",
                "Steam Engine",
                "Scenery",
                "guid:engine-a",
                string.Empty,
                25001,
                "Steam Engine [25001]",
                "maps/tarant/industrial/sec_02.sector",
                "Tile (301, 455)",
                "Sector object - maps/tarant/industrial/sec_02.sector"
            ),
        ];

        var quickPickEntries = GameDataQuickPickCatalog.BuildInventoryStaticObjectPrototypeEntries(
            entries,
            string.Empty
        );

        await Assert.That(quickPickEntries.Count).IsEqualTo(1);
        await Assert.That(quickPickEntries[0].BadgeText).IsEqualTo("Weapon");
        await Assert.That(quickPickEntries[0].ApplyValueText).IsEqualTo("guid:dagger-a");
    }

    [Test]
    public async Task QuickPickEntries_ToString_IsSafeForComboBoxSelectionText()
    {
        var entry = GameDataQuickPickCatalog.BuildSpellEntries("teleportation").Single();

        await Assert.That(entry.ToString()).IsEqualTo("Teleportation");
        await Assert.That(entry.RecommendedValueOption.ToString()).Contains("Spell token");
        await Assert.That(entry.RecommendedValueChoice.ToString()).Contains("Teleportation");
    }

    [Test]
    public async Task BuildInventoryTokenEntries_CombinesPaletteItemsAndPlacedItemSources()
    {
        PrototypePaletteEntry[] prototypeEntries =
        [
            new PrototypePaletteEntry(
                14001,
                "Weapon",
                "proto/items/dagger.pro",
                "Rusty Dagger",
                "A chipped blade.",
                "Items",
                "art/items/dagger.art"
            ),
            new PrototypePaletteEntry(
                24001,
                "Npc",
                "proto/critters/guard.pro",
                "City Guard",
                "A stern watchman.",
                "Critters",
                "art/critters/guard.art"
            ),
        ];
        StaticObjectCatalogEntry[] staticObjectEntries =
        [
            new StaticObjectCatalogEntry(
                "Mob asset",
                "Rusty Dagger",
                "Weapon",
                "guid:dagger-a",
                string.Empty,
                14001,
                "Rusty Dagger [14001]",
                "maps/shrouded_hills/mob01.mob",
                "Tile unavailable",
                "Mob asset - maps/shrouded_hills/mob01.mob"
            ),
            new StaticObjectCatalogEntry(
                "Sector object",
                "Steam Engine",
                "Scenery",
                "guid:engine-a",
                string.Empty,
                25001,
                "Steam Engine [25001]",
                "maps/tarant/industrial/sec_02.sector",
                "Tile (301, 455)",
                "Sector object - maps/tarant/industrial/sec_02.sector"
            ),
        ];

        var quickPickEntries = GameDataQuickPickCatalog.BuildInventoryTokenEntries(
            prototypeEntries,
            staticObjectEntries,
            string.Empty
        );

        await Assert.That(quickPickEntries.Count).IsEqualTo(2);
        await Assert
            .That(
                quickPickEntries.Any(entry =>
                    entry.BadgeText == "Proto · Weapon" && entry.ApplyValueText == "proto:14001"
                )
            )
            .IsTrue();
        await Assert
            .That(
                quickPickEntries.Any(entry =>
                    entry.BadgeText == "Placed · Weapon" && entry.ApplyValueText == "guid:dagger-a"
                )
            )
            .IsTrue();
    }

    [Test]
    public async Task BuildWorldLocationEntries_ExcludesAreasWithoutCoordinates()
    {
        WorldMapCatalogEntry[] entries =
        [
            new WorldMapCatalogEntry(
                10,
                "Tarant",
                91,
                98,
                true,
                "Industrial city.",
                "World (91, 98)",
                "map01 @ (500, 475)",
                ["map01"]
            ),
            new WorldMapCatalogEntry(
                11,
                "Hidden Test Area",
                0,
                0,
                false,
                "No world coordinates.",
                "World coordinates unavailable",
                "No linked local map anchors.",
                []
            ),
        ];

        var quickPickEntries = GameDataQuickPickCatalog.BuildWorldLocationEntries(entries, "tar");

        await Assert.That(quickPickEntries.Count).IsEqualTo(1);
        await Assert.That(quickPickEntries[0].TitleText).IsEqualTo("Tarant");
        await Assert.That(quickPickEntries[0].WorldX).IsEqualTo(91);
        await Assert.That(quickPickEntries[0].WorldY).IsEqualTo(98);
        await Assert.That(quickPickEntries[0].ValueOptions[0].LabelText).IsEqualTo("World coordinates");
        await Assert.That(quickPickEntries[0].ApplyValueText).IsEqualTo("91, 98");
    }

    [Test]
    public async Task BuildTileArtEntries_MatchesByNameAndReturnsCanonicalArtId()
    {
        TileArtCatalogEntry[] entries =
        [
            new TileArtCatalogEntry(
                0x50000011u,
                "0x50000011",
                "Cobblestone Street",
                "Ground",
                17,
                0,
                0,
                "art/ground/cobble.art",
                "Ground art #17 - frame 0 - palette 0"
            ),
            new TileArtCatalogEntry(
                0x60021007u,
                "0x60021007",
                "Empty Key Ring",
                "Item",
                7,
                1,
                0,
                "art/inventory/keyring.art",
                "Item art #7 - frame 1 - palette 0"
            ),
        ];

        var quickPickEntries = GameDataQuickPickCatalog.BuildTileArtEntries(entries, "cobble");

        await Assert.That(quickPickEntries.Count).IsEqualTo(1);
        await Assert.That(quickPickEntries[0].BadgeText).IsEqualTo("Ground");
        await Assert.That(quickPickEntries[0].ApplyValueText).IsEqualTo("0x50000011");
        await Assert.That(quickPickEntries[0].ValueOptions[0].LabelText).IsEqualTo("Art id");
    }

    [Test]
    public async Task BuildSpawnTokenEntries_CombinesPrototypeAndPlacedSpawnSources()
    {
        PrototypePaletteEntry[] prototypeEntries =
        [
            new PrototypePaletteEntry(
                24001,
                "Npc",
                "proto/critters/guard.pro",
                "City Guard",
                "A stern watchman.",
                "Critters",
                "art/critters/guard.art"
            ),
            new PrototypePaletteEntry(
                2001,
                "Scenery",
                "proto/scenery/lamp.pro",
                "Lamp Post",
                "Town street light.",
                "Scenery",
                "art/scenery/lamp.art"
            ),
        ];
        StaticObjectCatalogEntry[] staticObjectEntries =
        [
            new StaticObjectCatalogEntry(
                "Sector object",
                "Lamp Post",
                "Scenery",
                "guid:lamp-a",
                string.Empty,
                2001,
                "Lamp Post [2001]",
                "maps/tarant/street/sec_03.sector",
                "Tile (85, 144)",
                "Sector object - maps/tarant/street/sec_03.sector"
            ),
        ];

        var quickPickEntries = GameDataQuickPickCatalog.BuildSpawnTokenEntries(
            prototypeEntries,
            staticObjectEntries,
            "lamp"
        );

        await Assert.That(quickPickEntries.Count).IsEqualTo(2);
        await Assert
            .That(
                quickPickEntries.Any(entry =>
                    entry.BadgeText == "Proto · Scenery" && entry.ApplyValueText == "proto:2001"
                )
            )
            .IsTrue();
        await Assert
            .That(
                quickPickEntries.Any(entry =>
                    entry.BadgeText == "Placed · Scenery" && entry.ApplyValueText == "guid:lamp-a"
                )
            )
            .IsTrue();
    }

    [Test]
    public async Task BuildSchematicEntries_UsesRawPrototypeNumbersInsteadOfProtoTokens()
    {
        PrototypePaletteEntry[] entries =
        [
            new PrototypePaletteEntry(
                10079,
                "Generic",
                "proto/items/pyrotechnic-axe.pro",
                "Pyrotechnic Axe",
                "A learned schematic result.",
                "Items",
                "art/items/axe.art"
            ),
        ];

        var quickPickEntries = GameDataQuickPickCatalog.BuildSchematicEntries(entries, "pyro");

        await Assert.That(quickPickEntries.Count).IsEqualTo(1);
        await Assert.That(quickPickEntries[0].ApplyValueText).IsEqualTo("10079");
        await Assert.That(quickPickEntries[0].SubtitleText).IsEqualTo("Schematic id 10079");
    }

    [Test]
    public async Task BuildSpellEntries_MatchesByCollegeAndAppliesCanonicalSpellName()
    {
        var quickPickEntries = GameDataQuickPickCatalog.BuildSpellEntries("conveyance");

        await Assert.That(quickPickEntries.Count).IsGreaterThan(0);
        await Assert.That(quickPickEntries[0].BadgeText).IsEqualTo("Spell");
        await Assert.That(quickPickEntries.Any(entry => entry.ApplyValueText == "Teleportation")).IsTrue();
    }

    [Test]
    public async Task BuildSpellCollegeEntries_ReturnsCanonicalCollegeToken()
    {
        var quickPickEntries = GameDataQuickPickCatalog.BuildSpellCollegeEntries("tempo");

        await Assert.That(quickPickEntries.Count).IsEqualTo(1);
        await Assert.That(quickPickEntries[0].TitleText).IsEqualTo("Temporal");
        await Assert.That(quickPickEntries[0].ApplyValueText).IsEqualTo("Temporal");
    }

    [Test]
    public async Task BuildTechEntries_ReturnCanonicalDisciplineAndSkillTokens()
    {
        var disciplineEntries = GameDataQuickPickCatalog.BuildTechDisciplineEntries("mech");
        var skillEntries = GameDataQuickPickCatalog.BuildTechSkillEntries("repair");

        await Assert.That(disciplineEntries.Count).IsEqualTo(1);
        await Assert.That(disciplineEntries[0].ApplyValueText).IsEqualTo("Mechanical");
        await Assert.That(skillEntries.Count).IsEqualTo(1);
        await Assert.That(skillEntries[0].ApplyValueText).IsEqualTo("Repair");
    }
}
