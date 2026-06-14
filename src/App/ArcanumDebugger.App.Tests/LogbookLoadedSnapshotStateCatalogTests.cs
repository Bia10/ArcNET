using ArcanumDebugger.App.ViewModels;

namespace ArcanumDebugger.App.Tests;

public sealed class LogbookLoadedSnapshotStateCatalogTests
{
    [Test]
    public async Task TryCreateInvalidation_WhenRequestMatchesLoadedSnapshot_ReturnsFalse()
    {
        var result = LogbookLoadedSnapshotStateCatalog.TryCreateInvalidation(
            "player",
            "quests",
            "player",
            "quests",
            out var invalidation
        );

        await Assert.That(result).IsFalse();
        await Assert.That(invalidation).IsNull();
    }

    [Test]
    public async Task TryCreateInvalidation_WhenTargetChanges_ReturnsTargetReloadSummaries()
    {
        var result = LogbookLoadedSnapshotStateCatalog.TryCreateInvalidation(
            "player",
            "quests",
            "0x0000000201234567",
            "quests",
            out var invalidation
        );

        await Assert.That(result).IsTrue();
        await Assert.That(invalidation.DisplaySummaryText).Contains("different player or companion");
        await Assert.That(invalidation.DisplaySummaryText).Contains("quest journal page");
        await Assert.That(invalidation.EditorSummaryText).Contains("rebuild journal shortcuts");
    }

    [Test]
    public async Task TryCreateInvalidation_WhenPageChanges_ReturnsPageReloadSummaries()
    {
        var result = LogbookLoadedSnapshotStateCatalog.TryCreateInvalidation(
            "player",
            "quests",
            "player",
            "background",
            out var invalidation
        );

        await Assert.That(result).IsTrue();
        await Assert.That(invalidation.DisplaySummaryText).Contains("different journal page");
        await Assert.That(invalidation.DisplaySummaryText).Contains("background page");
        await Assert.That(invalidation.EditorSummaryText).Contains("shortcut list");
    }

    [Test]
    public async Task TryCreateInvalidation_WhenTargetAndPageChange_ReturnsCombinedReloadSummary()
    {
        var result = LogbookLoadedSnapshotStateCatalog.TryCreateInvalidation(
            "player",
            "quests",
            "0x0000000201234567",
            "keys",
            out var invalidation
        );

        await Assert.That(result).IsTrue();
        await Assert.That(invalidation.DisplaySummaryText).Contains("different player or companion");
        await Assert.That(invalidation.DisplaySummaryText).Contains("different journal page");
        await Assert.That(invalidation.EditorSummaryText).Contains("different target and page");
        await Assert.That(invalidation.EditorSummaryText).Contains("keyring page");
    }
}
