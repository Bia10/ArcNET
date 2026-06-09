namespace ArcNET.Diagnostics.Tests;

public sealed class CodeCatalogTests
{
    [Test]
    public async Task FormatModuleAddress_WhenAnchorExists_UsesAnchorLabel()
    {
        var formatted = CodeCatalog.FormatModuleAddress((uint)RuntimeOffsets.LevelRecalcRva);

        await Assert.That(formatted).IsEqualTo("level_recalc (Arcanum.exe+0x000A69C0)");
    }

    [Test]
    public async Task TryResolveAnchor_WhenRvaIsFarFromKnownSite_ReturnsFalse()
    {
        var resolved = CodeCatalog.TryResolveAnchor(0x7FFF_FFFF, out _);

        await Assert.That(resolved).IsFalse();
    }
}
