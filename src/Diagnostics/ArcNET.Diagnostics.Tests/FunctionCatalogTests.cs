namespace ArcNET.Diagnostics.Tests;

public sealed class FunctionCatalogTests
{
    [Test]
    public async Task TryGetDefinition_WhenTokenIsNormalizedVariant_ResolvesKnownFunction()
    {
        var resolved = FunctionCatalog.TryGetDefinition("level-recalc", out var function);

        await Assert.That(resolved).IsTrue();
        await Assert.That(function.Key).IsEqualTo("level_recalc");
        await Assert.That(function.Rva).IsEqualTo(RuntimeOffsets.LevelRecalcRva);
    }

    [Test]
    public async Task DispatcherCandidates_ExposeRenderDispatcherTargets()
    {
        await Assert
            .That(FunctionCatalog.DispatcherCandidates.Select(static candidate => candidate.Key))
            .IsEquivalentTo(["tig_window_display", "gamelib_draw", "tig_video_flip"]);
    }
}
