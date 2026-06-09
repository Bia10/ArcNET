namespace ArcNET.Diagnostics.Tests;

public sealed class RuntimeWatchCatalogTests
{
    [Test]
    public async Task ResolveSelectors_WhenTokensContainProfilesAndHooks_ReturnsDistinctOrderedHooks()
    {
        var hooks = RuntimeWatchCatalog.ResolveSelectors(["render-core", "tig-window-display", "ui-show-inven-loot"]);

        await Assert
            .That(hooks.Select(static hook => hook.Key))
            .IsEquivalentTo(["ui-show-inven-loot", "gamelib-draw", "tig-window-display"]);
    }

    [Test]
    public async Task UsesHighVolumeHooks_WhenPresetContainsRenderHooks_ReturnsTrue()
    {
        var renderHooks = RuntimeWatchCatalog.ResolveSelectors(["render-core"]);
        var worldHooks = RuntimeWatchCatalog.ResolveSelectors(["world"]);

        await Assert.That(RuntimeWatchCatalog.UsesHighVolumeHooks(renderHooks)).IsTrue();
        await Assert.That(RuntimeWatchCatalog.UsesHighVolumeHooks(worldHooks)).IsFalse();
    }
}
