using ArcanumDebugger.App.ViewModels;
using ArcNET.Diagnostics;

namespace ArcanumDebugger.App.Tests;

public sealed class InventoryLiveCatalogTests
{
    [Test]
    public async Task CreateOwnerEntries_PutsPlayerFirstAndAppendsRoster()
    {
        MobileRosterEntrySnapshot[] mobiles =
        [
            new(
                "0x0000000200000002",
                "Sogg Mead Mug",
                "Npc",
                "mob:sogg",
                "proto#17088",
                "0x0000000000004321",
                "PoolEntry · pool index 1",
                17088,
                "PoolEntry"
            ),
        ];

        var entries = InventoryLiveCatalog.CreateOwnerEntries(mobiles);

        await Assert.That(entries.Count).IsEqualTo(2);
        await Assert.That(entries[0].DisplayText).IsEqualTo("Player");
        await Assert.That(entries[0].IsPlayer).IsTrue();
        await Assert.That(entries[1].HandleTokenText).IsEqualTo("0x0000000200000002");
    }

    [Test]
    public async Task CreateItemEntries_UsesOwnerInventoryLinksAndResolvedItems()
    {
        var owner = new ObjectProbeObjectSnapshot(
            "0x0000000200000001",
            "PoolEntry",
            "Pc",
            "hero",
            "proto#1000",
            "0x0000000000004000",
            "0x00002004",
            "PoolEntry · pool index 1",
            [
                new ObjectProbeSectionSnapshot(
                    "inventory_links",
                    "Inventory Links",
                    "Getter-backed handle-array read",
                    [
                        new ObjectProbeDetailSnapshot(
                            "Inventory Slot 0",
                            "0x0000000200000002",
                            "obj_array_field_handle_get"
                        ),
                        new ObjectProbeDetailSnapshot(
                            "Inventory Slot 1",
                            "0x0000000200000003",
                            "obj_array_field_handle_get"
                        ),
                    ]
                ),
            ],
            []
        );
        ObjectProbeObjectSnapshot[] items =
        [
            new(
                "0x0000000200000002",
                "PoolEntry",
                "Weapon",
                "Rusty Dagger",
                "proto#14001",
                "0x0000000000005000",
                "0x00003004",
                "PoolEntry · pool index 2",
                [],
                []
            ),
            new(
                "0x0000000200000003",
                "PoolEntry",
                "Armor",
                "Leather Armor",
                "proto#8200",
                "0x0000000000005001",
                "0x00003008",
                "PoolEntry · pool index 3",
                [],
                []
            ),
        ];

        var entries = InventoryLiveCatalog.CreateItemEntries(owner, items, 24, out var totalHandleCount);

        await Assert.That(totalHandleCount).IsEqualTo(2);
        await Assert.That(entries.Count).IsEqualTo(2);
        await Assert.That(entries[0].SlotLabelText).IsEqualTo("Inventory Slot 0");
        await Assert.That(entries[0].DisplayText).IsEqualTo("Rusty Dagger");
        await Assert.That(entries[1].PrototypeText).IsEqualTo("proto#8200");
    }
}
