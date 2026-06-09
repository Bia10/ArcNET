namespace ArcNET.Diagnostics.Tests;

public sealed class RuntimeWatchValueCatalogTests
{
    [Test]
    public async Task QuestPcStateName_WhenBotchedModifierIsPresent_AppendsBotchedLabel()
    {
        var stateName = RuntimeWatchValueCatalog.QuestPcStateName(RuntimeWatchValueCatalog.QuestBotchedModifier | 2);

        await Assert.That(stateName).IsEqualTo("Accepted [Botched]");
    }

    [Test]
    public async Task FormatPackedLocation_WhenGivenPackedCoordinates_RendersTuple()
    {
        const ulong packedLocation = ((ulong)456 << 32) | 123u;

        await Assert.That(RuntimeWatchValueCatalog.FormatPackedLocation(packedLocation)).IsEqualTo("(123, 456)");
    }
}
