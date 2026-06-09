namespace ArcNET.Diagnostics.Tests;

public sealed class ModuleSymbolCatalogTests
{
    [Test]
    public async Task ResolveUnique_WhenNormalizedTokenMatchesSingleSymbol_ReturnsSymbol()
    {
        var catalog = new ModuleSymbolCatalog(
            @"C:\Games\Arcanum\Arcanum.exe",
            [
                new ModuleFunctionSymbol("tig_window_display", 0x1000, 0x20),
                new ModuleFunctionSymbol("gamelib_draw", 0x2000, 0x30),
            ]
        );

        var symbol = catalog.ResolveUnique("TigWindowDisplay");

        await Assert.That(symbol.Name).IsEqualTo("tig_window_display");
        await Assert.That(symbol.Rva).IsEqualTo((uint)0x1000);
    }

    [Test]
    public async Task Query_WhenDuplicatesOnlyEnabled_FiltersToDuplicateNames()
    {
        var catalog = new ModuleSymbolCatalog(
            @"C:\Games\Arcanum\Arcanum.exe",
            [
                new ModuleFunctionSymbol("foo", 0x1000, 0x20),
                new ModuleFunctionSymbol("foo", 0x2000, 0x20),
                new ModuleFunctionSymbol("bar", 0x3000, 0x20),
            ]
        );

        var matches = catalog.Query(null, 10, duplicatesOnly: true).ToArray();

        await Assert.That(matches.Length).IsEqualTo(2);
        await Assert.That(matches.All(static match => match.Name == "foo")).IsTrue();
    }
}
