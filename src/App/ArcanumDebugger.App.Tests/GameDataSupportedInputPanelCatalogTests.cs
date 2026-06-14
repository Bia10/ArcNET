using System.Linq;
using ArcanumDebugger.App.ViewModels;
using ArcNET.Diagnostics;
using ArcNET.GameData.Workspace;

namespace ArcanumDebugger.App.Tests;

public sealed class GameDataSupportedInputPanelCatalogTests
{
    [Test]
    public async Task CreateLookupState_WhenFilterEmpty_ShowsWorkspaceBackedPreviewEntries()
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

        var state = GameDataSupportedInputPanelCatalog.CreateLookupState(
            prototypeEntries,
            staticObjectEntries,
            string.Empty
        );

        await Assert.That(state.HasEntries).IsTrue();
        await Assert.That(state.SummaryText).IsEqualTo(string.Empty);
        await Assert.That(state.FilterPlaceholderText).Contains("proto numbers");
        await Assert.That(state.Entries.Select(static entry => entry.ApplyValueText)).Contains("proto:20011");
        await Assert
            .That(state.Entries.Select(static entry => entry.ApplyValueText))
            .Contains("3f61a56b-2818-4f89-a297-8c94504f90a2");
    }

    [Test]
    public async Task CreateInventoryState_WhenCatalogMissing_ShowsLoadPrompt()
    {
        var state = GameDataSupportedInputPanelCatalog.CreateInventoryState([], [], "dagger");

        await Assert.That(state.HasEntries).IsFalse();
        await Assert.That(state.SummaryText).Contains("Load the local workspace catalog");
        await Assert.That(state.FilterPlaceholderText).Contains("item names");
        await Assert.That(state.BrowseButtonText).IsEqualTo("Open Full Catalog");
    }

    [Test]
    public async Task CreateInventoryState_WhenFilterMatches_ReturnsSupportedEntries()
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
        ];

        var state = GameDataSupportedInputPanelCatalog.CreateInventoryState(
            prototypeEntries,
            staticObjectEntries,
            "dagger"
        );

        await Assert.That(state.HasEntries).IsTrue();
        await Assert.That(state.Entries.Count).IsEqualTo(2);
        await Assert.That(state.Entries.Select(static entry => entry.ApplyValueText)).Contains("proto:14001");
        await Assert.That(state.Entries.Select(static entry => entry.ApplyValueText)).Contains("guid:dagger-a");
    }

    [Test]
    public async Task CreateInventoryState_WhenFilterEmpty_ShowsWorkspaceBackedPreviewEntries()
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
        ];

        var state = GameDataSupportedInputPanelCatalog.CreateInventoryState(
            prototypeEntries,
            staticObjectEntries,
            string.Empty
        );

        await Assert.That(state.HasEntries).IsTrue();
        await Assert.That(state.SummaryText).IsEqualTo(string.Empty);
        await Assert.That(state.Entries.Select(static entry => entry.ApplyValueText)).Contains("proto:14001");
        await Assert.That(state.Entries.Select(static entry => entry.ApplyValueText)).Contains("guid:dagger-a");
    }

    [Test]
    public async Task CreateSpawnState_WhenFilterDoesNotMatch_ShowsNoMatchMessage()
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
        ];

        var state = GameDataSupportedInputPanelCatalog.CreateSpawnState(prototypeEntries, [], "ogre");

        await Assert.That(state.HasEntries).IsFalse();
        await Assert.That(state.SummaryText).Contains("No supported spawn inputs matched 'ogre'");
    }

    [Test]
    public async Task CreateSpawnState_WhenFilterEmpty_ShowsWorkspaceBackedPreviewEntries()
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

        var state = GameDataSupportedInputPanelCatalog.CreateSpawnState(
            prototypeEntries,
            staticObjectEntries,
            string.Empty
        );

        await Assert.That(state.HasEntries).IsTrue();
        await Assert.That(state.SummaryText).IsEqualTo(string.Empty);
        await Assert.That(state.Entries.Select(static entry => entry.ApplyValueText)).Contains("proto:24001");
        await Assert.That(state.Entries.Select(static entry => entry.ApplyValueText)).Contains("guid:lamp-a");
    }

    [Test]
    public async Task CreateWorldLocationState_WhenFilterEmpty_ReturnsKnownLocations()
    {
        WorldMapCatalogEntry[] worldEntries =
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
                20,
                "Ashbury",
                120,
                144,
                true,
                "Coastal city.",
                "World (120, 144)",
                "map02 @ (320, 240)",
                ["map02"]
            ),
        ];

        var state = GameDataSupportedInputPanelCatalog.CreateWorldLocationState(worldEntries);

        await Assert.That(state.HasEntries).IsTrue();
        await Assert.That(state.Entries.Count).IsEqualTo(2);
        await Assert.That(state.FilterPlaceholderText).Contains("location names");
        await Assert.That(state.Entries[0].WorldX).IsNotNull();
        await Assert.That(state.Entries[0].WorldY).IsNotNull();
    }
}
