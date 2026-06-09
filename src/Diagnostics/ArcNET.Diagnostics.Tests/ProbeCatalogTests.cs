namespace ArcNET.Diagnostics.Tests;

public sealed class ProbeCatalogTests
{
    [Test]
    public async Task ExpandSelectors_WhenTokensContainProfileKeys_ExpandsDistinctSelectors()
    {
        var selectors = ProbeCatalog.ExpandSelectors(["world-core", "ui-core", "world-core"]);

        await Assert
            .That(selectors)
            .IsEquivalentTo([
                "teleport-do",
                "map-open-in-game",
                "timeevent-notify-pc-teleported",
                "object-create",
                "ui-show-inven-loot",
                "ui-start-dialog",
                "ui-spell-add",
                "ui-spell-maintain-add",
                "ui-spell-maintain-end",
            ]);
    }
}
